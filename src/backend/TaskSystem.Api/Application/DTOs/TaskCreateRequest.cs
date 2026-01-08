using TaskApp.Domain.Enums;
using TaskStatus = TaskApp.Domain.Enums.TaskStatus;

namespace TaskApp.Application.DTOs;

public class TaskCreateRequest
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime DueDateUtc { get; set; }
    public TaskPriority Priority { get; set; }
    public TaskStatus? Status { get; set; }
    public UserRefDto Owner { get; set; } = null!;
    public UserRefDto? Assignee { get; set; }
}

