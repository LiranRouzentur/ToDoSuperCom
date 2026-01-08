namespace TaskSystem.Shared.Contracts.Events;

public sealed record TaskDueV1
{
    public Guid TaskId { get; init; }
    public string Title { get; init; } = string.Empty;
    public DateTime DueDateUtc { get; init; }
    public DateTime TimestampUtc { get; init; } = DateTime.UtcNow;
}


