using System.Text;
using System.Text.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using TaskSystem.Shared.Contracts.Events;

namespace TaskSystem.Worker.Messaging;

public class RabbitMqConsumer : BackgroundService
{
    private readonly IConnection _connection;
    private readonly ILogger<RabbitMqConsumer> _logger;
    private IModel? _channel;

    public RabbitMqConsumer(IConnection connection, ILogger<RabbitMqConsumer> logger)
    {
        _connection = connection;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _channel = _connection.CreateModel();
        _channel.BasicQos(prefetchSize: 0, prefetchCount: RabbitMqConstants.DefaultPrefetchCount, global: false);

        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.Received += async (model, ea) =>
        {
            var deliveryTag = ea.DeliveryTag;
            var messageId = ea.BasicProperties.MessageId ?? "unknown";
            
            try
            {
                var body = ea.Body.ToArray();
                var message = JsonSerializer.Deserialize<TaskDueV1>(Encoding.UTF8.GetString(body));

                if (message == null)
                {
                    _logger.LogWarning("Received null or invalid TaskDue message. MessageId: {MessageId}, DeliveryTag: {DeliveryTag}", 
                        messageId, deliveryTag);
                    _channel.BasicNack(deliveryTag: deliveryTag, multiple: false, requeue: false);
                    return;
                }

                // Requirement: log "Hi your Task is due {Task.Title}"
                // Note: Consumer is idempotent - duplicate messages will result in duplicate logs
                // This is acceptable as the requirement is to log the message
                _logger.LogInformation("Hi your Task is due {Title}. TaskId: {TaskId}, MessageId: {MessageId}", 
                    message.Title, message.TaskId, messageId);

                // Ack on success
                _channel.BasicAck(deliveryTag: deliveryTag, multiple: false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing TaskDue message. MessageId: {MessageId}, DeliveryTag: {DeliveryTag}", 
                    messageId, deliveryTag);

                // Nack without requeue - message goes to DLQ if configured
                // Requeue=false prevents infinite retry loops
                _channel.BasicNack(deliveryTag: deliveryTag, multiple: false, requeue: false);
            }

            await Task.CompletedTask;
        };

        _channel.BasicConsume(
            queue: RabbitMqConstants.DueRemindersQueue,
            autoAck: false,
            consumer: consumer);

        _logger.LogInformation("RabbitMQ consumer started and listening. Queue: {Queue}, PrefetchCount: {PrefetchCount}", 
            RabbitMqConstants.DueRemindersQueue, RabbitMqConstants.DefaultPrefetchCount);

        // Keep the worker running until cancellation is requested
        var tcs = new TaskCompletionSource<bool>();
        stoppingToken.Register(() => tcs.SetResult(true));
        await tcs.Task;

        _logger.LogInformation("RabbitMQ consumer stopping...");
    }

    public override void Dispose()
    {
        _channel?.Close();
        _channel?.Dispose();
        base.Dispose();
    }
}

