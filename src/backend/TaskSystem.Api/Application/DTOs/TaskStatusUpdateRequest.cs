using TaskApp.Domain.Enums;
using TaskStatus = TaskApp.Domain.Enums.TaskStatus;

namespace TaskApp.Application.DTOs;

public class TaskStatusUpdateRequest
{
    public TaskStatus Status { get; set; }
}

