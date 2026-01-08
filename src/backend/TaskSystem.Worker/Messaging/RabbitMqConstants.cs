namespace TaskSystem.Worker.Messaging;

/// <summary>
/// Centralized constants for RabbitMQ topology and configuration.
/// </summary>
public static class RabbitMqConstants
{
    // Exchange
    public const string ExchangeName = "tasks.events";
    public const string ExchangeType = "topic"; // RabbitMQ exchange type

    // Queues
    public const string DueRemindersQueue = "tasks.reminders.due";
    public const string DueRemindersDlq = "tasks.reminders.dlq";

    // Routing Keys
    public const string TaskDueRoutingKey = "task.due";

    // Message Properties
    public const string ContentType = "application/json";
    public const int DefaultPrefetchCount = 1;
}

