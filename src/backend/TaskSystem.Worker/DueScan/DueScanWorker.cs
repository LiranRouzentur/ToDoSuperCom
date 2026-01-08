using Microsoft.Data.Sqlite;
using TaskSystem.Shared.Contracts.Events;
using TaskSystem.Worker.Configuration;
using TaskSystem.Worker.Messaging;

namespace TaskSystem.Worker.DueScan;

public class DueScanWorker : BackgroundService
{
    private readonly IConfiguration _configuration;
    private readonly IRabbitMqPublisher _publisher;
    private readonly ILogger<DueScanWorker> _logger;

    public DueScanWorker(
        IConfiguration configuration,
        IRabbitMqPublisher publisher,
        ILogger<DueScanWorker> logger)
    {
        _configuration = configuration;
        _publisher = publisher;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var intervalSeconds = _configuration.GetValue<int>(
            DueScanConfiguration.IntervalSecondsKey, 
            DueScanConfiguration.DefaultIntervalSeconds);
        var batchSize = _configuration.GetValue<int>(
            DueScanConfiguration.BatchSizeKey, 
            DueScanConfiguration.DefaultBatchSize);
        var connectionString = _configuration.GetConnectionString("DefaultConnection") 
            ?? throw new InvalidOperationException("ConnectionString:DefaultConnection is required");

        // Validate configuration
        if (intervalSeconds < DueScanConfiguration.MinIntervalSeconds)
        {
            _logger.LogWarning("IntervalSeconds {Interval} is below minimum {Min}, using minimum", 
                intervalSeconds, DueScanConfiguration.MinIntervalSeconds);
            intervalSeconds = DueScanConfiguration.MinIntervalSeconds;
        }
        if (batchSize > DueScanConfiguration.MaxBatchSize)
        {
            _logger.LogWarning("BatchSize {Batch} exceeds maximum {Max}, using maximum", 
                batchSize, DueScanConfiguration.MaxBatchSize);
            batchSize = DueScanConfiguration.MaxBatchSize;
        }

        _logger.LogInformation("DueScan worker started. Polling every {Interval}s, batch size {BatchSize}", 
            intervalSeconds, batchSize);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ScanAndPublishDueTasks(connectionString, batchSize, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("DueScan worker is stopping");
                break;
            }
            catch (Exception ex)
            {
                // Log error but continue - prevents worker crash on transient failures
                _logger.LogError(ex, "Error in DueScan loop. Will retry on next interval");
            }

            // Wait for next interval (respects cancellation token)
            try
            {
                await Task.Delay(TimeSpan.FromSeconds(intervalSeconds), stoppingToken);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("DueScan worker cancellation requested");
                break;
            }
        }
    }

    private async Task ScanAndPublishDueTasks(string connectionString, int batchSize, CancellationToken cancellationToken)
    {
        using var connection = new SqliteConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        // Check if Tasks table exists (wait for API migrations)
        using (var checkCmd = connection.CreateCommand())
        {
            checkCmd.CommandText = "SELECT name FROM sqlite_master WHERE type='table' AND name='Tasks'";
            var tableExists = await checkCmd.ExecuteScalarAsync(cancellationToken);
            if (tableExists == null)
            {
                _logger.LogDebug("Tasks table does not exist yet, waiting for API migrations...");
                return; // Skip this scan, will retry in next interval
            }
        }

        var now = DateTime.UtcNow;

        // Atomic claim: UPDATE Tasks SET DueNotifiedAtUtc = @now 
        // WHERE Id IN (SELECT Id FROM Tasks WHERE DueDateUtc < @now AND DueNotifiedAtUtc IS NULL AND Status NOT IN (3, 5) ORDER BY DueDateUtc LIMIT @batch)
        // Status 3 = Completed, Status 5 = Cancelled
        var claimSql = @"
            UPDATE Tasks 
            SET DueNotifiedAtUtc = @now 
            WHERE Id IN (
                SELECT Id FROM Tasks 
                WHERE DueDateUtc < @now 
                AND DueNotifiedAtUtc IS NULL 
                AND Status != 3 
                AND Status != 5 
                ORDER BY DueDateUtc 
                LIMIT @batch
            )";

        using (var claimCmd = connection.CreateCommand())
        {
            claimCmd.CommandText = claimSql;
            claimCmd.Parameters.AddWithValue("@now", now);
            claimCmd.Parameters.AddWithValue("@batch", batchSize);

            var updated = await claimCmd.ExecuteNonQueryAsync(cancellationToken);
            if (updated == 0)
            {
                return; // No tasks to claim
            }

            _logger.LogInformation("Claimed {Count} overdue tasks", updated);
        }

        // Now SELECT the claimed tasks to publish messages
        var selectSql = @"
            SELECT Id, Title, DueDateUtc 
            FROM Tasks 
            WHERE DueNotifiedAtUtc = @now";

        using var selectCmd = connection.CreateCommand();
        selectCmd.CommandText = selectSql;
        selectCmd.Parameters.AddWithValue("@now", now);

        using var reader = await selectCmd.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var taskId = reader.GetGuid(0);
            var title = reader.GetString(1);
            var dueDate = reader.GetDateTime(2);

            var message = new TaskDueV1
            {
                TaskId = taskId,
                Title = title,
                DueDateUtc = dueDate,
                TimestampUtc = now
            };

            // Publish message (with error handling in publisher)
            // Note: If publish fails, task is already marked as notified
            // This is acceptable because:
            // 1. The claim is atomic and prevents duplicate processing
            // 2. Publisher logs errors but doesn't throw
            // 3. In production, consider outbox pattern for guaranteed delivery
            _publisher.PublishTaskDue(message);
            _logger.LogInformation("Published TaskDue message. TaskId: {TaskId}, Title: {Title}", taskId, title);
        }
    }
}

