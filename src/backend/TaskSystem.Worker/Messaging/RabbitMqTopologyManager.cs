using RabbitMQ.Client;

namespace TaskSystem.Worker.Messaging;

public class RabbitMqTopologyManager
{
    private readonly ILogger<RabbitMqTopologyManager> _logger;

    public RabbitMqTopologyManager(ILogger<RabbitMqTopologyManager> logger)
    {
        _logger = logger;
    }

    public void DeclareTopology(IModel channel)
    {
        _logger.LogInformation("Declaring RabbitMQ topology...");

        // Declare durable topic exchange
        channel.ExchangeDeclare(
            exchange: RabbitMqConstants.ExchangeName,
            type: RabbitMqConstants.ExchangeType,
            durable: true,
            autoDelete: false,
            arguments: null);

        // Declare durable queue for due reminders
        channel.QueueDeclare(
            queue: RabbitMqConstants.DueRemindersQueue,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null);

        // Bind queue to exchange with routing key
        channel.QueueBind(
            queue: RabbitMqConstants.DueRemindersQueue,
            exchange: RabbitMqConstants.ExchangeName,
            routingKey: RabbitMqConstants.TaskDueRoutingKey,
            arguments: null);

        // Declare DLQ for failed messages
        channel.QueueDeclare(
            queue: RabbitMqConstants.DueRemindersDlq,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null);

        _logger.LogInformation("RabbitMQ topology declared successfully. Exchange: {Exchange}, Queue: {Queue}", 
            RabbitMqConstants.ExchangeName, RabbitMqConstants.DueRemindersQueue);
    }
}

