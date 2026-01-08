using TaskApp.Application.DTOs;
using TaskApp.Domain.Enums;
using TaskStatus = TaskApp.Domain.Enums.TaskStatus;

namespace TaskApp.Application.Services;

public interface ITaskService
{
    Task<TaskResponse> CreateTaskAsync(TaskCreateRequest request, CancellationToken cancellationToken = default);
    Task<TaskResponse?> GetTaskByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<PagedResponse<TaskResponse>> ListTasksAsync(
        TaskScope? scope,
        Guid? ownerUserId,
        Guid? assignedUserId,
        List<TaskStatus>? statuses,
        List<TaskPriority>? priorities,
        bool? overdueOnly,
        bool? reminderSent,
        string? search,
        string? sortBy,
        string? sortDir,
        int? page,
        int? pageSize,
        CancellationToken cancellationToken = default);
    Task<TaskResponse> UpdateTaskAsync(Guid id, TaskUpdateRequest request, byte[] rowVersion, CancellationToken cancellationToken = default);
    Task<TaskResponse> UpdateTaskStatusAsync(Guid id, TaskStatus status, byte[] rowVersion, CancellationToken cancellationToken = default);
    Task<TaskResponse> UpdateTaskAssigneeAsync(Guid id, Guid? assignedUserId, byte[] rowVersion, CancellationToken cancellationToken = default);
    Task DeleteTaskAsync(Guid id, CancellationToken cancellationToken = default);
}

public enum TaskScope
{
    My,
    Assigned,
    All
}

