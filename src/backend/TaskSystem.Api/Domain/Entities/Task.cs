using TaskApp.Domain.Enums;

namespace TaskApp.Domain.Entities;

public class Task
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime DueDateUtc { get; set; }
    public TaskPriority Priority { get; set; }
    public Enums.TaskStatus Status { get; set; }
    public Guid OwnerUserId { get; set; }
    public Guid? AssignedUserId { get; set; }
    public bool ReminderSent { get; set; }
    public DateTime? DueNotifiedAtUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }

    public byte[] RowVersion { get; set; } = Array.Empty<byte>();

    // Navigation properties
    public User Owner { get; set; } = null!;
    public User? Assignee { get; set; }

    // Domain methods
    public bool IsOverdue()
    {
        return DueDateUtc < DateTime.UtcNow && !IsTerminalStatus();
    }

    public bool IsTerminalStatus()
    {
        return Status == Enums.TaskStatus.Completed || Status == Enums.TaskStatus.Cancelled;
    }

    public bool CanTransitionTo(Enums.TaskStatus newStatus)
    {
        // Allow flexible Kanban transitions (User can move cards anywhere)
        // System handles Overdue logic separately.
        return true;
    }

    public void UpdateOverdueStatus()
    {
        if (IsOverdue() && Status != Enums.TaskStatus.Overdue)
        {
            Status = Enums.TaskStatus.Overdue;
        }
    }
}
