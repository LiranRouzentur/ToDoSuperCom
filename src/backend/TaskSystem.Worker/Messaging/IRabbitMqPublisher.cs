using TaskSystem.Shared.Contracts.Events;

namespace TaskSystem.Worker.Messaging;

public interface IRabbitMqPublisher
{
    void PublishTaskDue(TaskDueV1 message);
}
