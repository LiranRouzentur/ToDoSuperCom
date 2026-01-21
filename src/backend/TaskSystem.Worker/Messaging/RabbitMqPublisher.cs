using System.Text;
using System.Text.Json;
using RabbitMQ.Client;
using TaskSystem.Shared.Contracts.Events;

namespace TaskSystem.Worker.Messaging;

public class RabbitMqPublisher : IRabbitMqPublisher, IDisposable
{
    private readonly IConnection _connection;
    private readonly IModel _channel;
    private readonly ILogger<RabbitMqPublisher> _logger;
    private readonly object _publishLock = new();

    public RabbitMqPublisher(IConnection connection, ILogger<RabbitMqPublisher> logger)
    {
        _connection = connection ?? throw new ArgumentNullException(nameof(connection));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _channel = _connection.CreateModel();
    }

    public void PublishTaskDue(TaskDueV1 message)
    {
        try
        {
            var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(message));

            var properties = _channel.CreateBasicProperties();
            properties.Persistent = true; // Durable message
            properties.ContentType = RabbitMqConstants.ContentType;
            properties.MessageId = message.TaskId.ToString(); // Use TaskId as message ID for deduplication

            // RabbitMQ IModel is NOT thread-safe. Synchronization is required.
            lock (_publishLock)
            {
                _channel.BasicPublish(
                    exchange: RabbitMqConstants.ExchangeName,
                    routingKey: RabbitMqConstants.TaskDueRoutingKey,
                    basicProperties: properties,
                    body: body);
            }

            _logger.LogInformation("Published TaskDue message. TaskId: {TaskId}, Title: {Title}", 
                message.TaskId, message.Title);
        }
        catch (Exception ex)
        {
            // Log error but don't throw - allows worker to continue
            // The task is already marked as notified, so retry would be idempotent
            _logger.LogError(ex, "Failed to publish TaskDue message. TaskId: {TaskId}, Title: {Title}. " +
                "Task is already marked as notified and will be skipped on next scan.", 
                message.TaskId, message.Title);
            // Note: In production, consider implementing an outbox pattern for guaranteed delivery
        }
    }

    public void Dispose()
    {
        _channel?.Close();
        _channel?.Dispose();
    }
}

