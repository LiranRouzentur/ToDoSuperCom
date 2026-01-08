using TaskApp.Domain.Enums;
using TaskStatus = TaskApp.Domain.Enums.TaskStatus;

namespace TaskApp.Application.DTOs;

public class TaskResponse
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime DueDateUtc { get; set; }
    public TaskPriority Priority { get; set; }
    public TaskStatus Status { get; set; }
    public bool ReminderSent { get; set; }
    public UserRefDto Owner { get; set; } = null!;
    public UserRefDto Assignee { get; set; } = null!;
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
    public string RowVersion { get; set; } = string.Empty;
}

