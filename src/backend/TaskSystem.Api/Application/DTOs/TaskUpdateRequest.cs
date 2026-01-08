using TaskApp.Domain.Enums;
using TaskStatus = TaskApp.Domain.Enums.TaskStatus;

namespace TaskApp.Application.DTOs;

public class TaskUpdateRequest
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime DueDateUtc { get; set; }
    public TaskPriority Priority { get; set; }
    public TaskStatus Status { get; set; }
    public Guid? AssignedUserId { get; set; }
}

