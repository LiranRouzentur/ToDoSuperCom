using Microsoft.EntityFrameworkCore;
using TaskApp.Application.DTOs;
using TaskApp.Application.Helpers;
using TaskApp.Domain.Entities;
using TaskApp.Domain.Enums;
using TaskApp.Infrastructure.Data;
using Task = TaskApp.Domain.Entities.Task;
using TaskStatus = TaskApp.Domain.Enums.TaskStatus;

namespace TaskApp.Application.Services;

public class TaskService : ITaskService
{
    private readonly TaskDbContext _context;
    private readonly IUserService _userService;
    private readonly ILogger<TaskService> _logger;

    public TaskService(TaskDbContext context, IUserService userService, ILogger<TaskService> logger)
    {
        _context = context;
        _userService = userService;
        _logger = logger;
    }

    public async Task<TaskResponse> CreateTaskAsync(TaskCreateRequest request, CancellationToken cancellationToken = default)
    {
        // Validate due date is in future
        if (request.DueDateUtc <= DateTime.UtcNow)
        {
            throw new InvalidOperationException("Due date must be in the future (UTC).");
        }

        using var scope = _logger.BeginScope(new Dictionary<string, object>
        {
            ["Action"] = "CreateTask",
            ["OwnerEmail"] = request.Owner.Email
        });

        _logger.LogInformation("Creating new task '{Title}'", request.Title);

        // Upsert owner
        var owner = await _userService.UpsertUserByEmailAsync(new UserCreateRequest
        {
            FullName = request.Owner.FullName,
            Email = request.Owner.Email,
            Telephone = request.Owner.Telephone
        }, cancellationToken);

        // Upsert assignee if provided, otherwise use owner
        UserResponse assignee;
        if (request.Assignee != null)
        {
            assignee = await _userService.UpsertUserByEmailAsync(new UserCreateRequest
            {
                FullName = request.Assignee.FullName,
                Email = request.Assignee.Email,
                Telephone = request.Assignee.Telephone
            }, cancellationToken);
        }
        else
        {
            assignee = owner;
        }

        // Create task
        var task = new Task
        {
            Id = Guid.NewGuid(),
            Title = request.Title.Trim(),
            Description = request.Description.Trim(),
            DueDateUtc = request.DueDateUtc,
            Priority = request.Priority,
            Status = request.Status ?? TaskStatus.Open,
            OwnerUserId = owner.Id,
            AssignedUserId = assignee.Id,
            ReminderSent = false,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };

        _context.Tasks.Add(task);
        await _context.SaveChangesAsync(cancellationToken);
        
        _logger.LogInformation("Task created successfully. Id: {TaskId}", task.Id);

        // Load with navigation properties
        await _context.Entry(task)
            .Reference(t => t.Owner)
            .LoadAsync(cancellationToken);
        await _context.Entry(task)
            .Reference(t => t.Assignee)
            .LoadAsync(cancellationToken);

        return MapToResponse(task);
    }

    public async Task<TaskResponse?> GetTaskByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var task = await _context.Tasks
            .Include(t => t.Owner)
            .Include(t => t.Assignee)
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);

        return task == null ? null : MapToResponse(task);
    }

    public async Task<PagedResponse<TaskResponse>> ListTasksAsync(
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
        CancellationToken cancellationToken = default)
    {
        var (normalizedPage, normalizedPageSize) = PaginationHelper.NormalizePagination(page, pageSize);

        var query = _context.Tasks
            .Include(t => t.Owner)
            .Include(t => t.Assignee)
            .AsQueryable();

        // Apply filters (extracted to helper methods for clarity)
        query = ApplyScopeFilter(query, scope, ownerUserId, assignedUserId);
        query = ApplyStatusFilter(query, statuses);
        query = ApplyPriorityFilter(query, priorities);
        query = ApplyOverdueFilter(query, overdueOnly);
        query = ApplyReminderSentFilter(query, reminderSent);
        query = ApplySearchFilter(query, search);

        // Apply sorting
        query = ApplySorting(query, sortBy, sortDir);

        // Get total count before pagination
        var totalItems = await query.CountAsync(cancellationToken);
        var totalPages = PaginationHelper.CalculateTotalPages(totalItems, normalizedPageSize);

        // Apply pagination
        var tasks = await query
            .Skip((normalizedPage - 1) * normalizedPageSize)
            .Take(normalizedPageSize)
            .ToListAsync(cancellationToken);

        return new PagedResponse<TaskResponse>
        {
            Items = tasks.Select(MapToResponse).ToList(),
            Page = normalizedPage,
            PageSize = normalizedPageSize,
            TotalItems = totalItems,
            TotalPages = totalPages
        };
    }

    private static IQueryable<Domain.Entities.Task> ApplyScopeFilter(
        IQueryable<Domain.Entities.Task> query,
        TaskScope? scope,
        Guid? ownerUserId,
        Guid? assignedUserId)
    {
        if (scope.HasValue)
        {
            return scope.Value switch
            {
                TaskScope.My when ownerUserId.HasValue => query.Where(t => t.OwnerUserId == ownerUserId.Value),
                TaskScope.Assigned when assignedUserId.HasValue => query.Where(t => t.AssignedUserId == assignedUserId.Value),
                TaskScope.All => query,
                _ => query
            };
        }

        // Apply explicit filters if scope not provided
        if (ownerUserId.HasValue)
            query = query.Where(t => t.OwnerUserId == ownerUserId.Value);
        if (assignedUserId.HasValue)
            query = query.Where(t => t.AssignedUserId == assignedUserId.Value);

        return query;
    }

    private static IQueryable<Domain.Entities.Task> ApplyStatusFilter(
        IQueryable<Domain.Entities.Task> query,
        List<TaskStatus>? statuses)
    {
        if (statuses != null && statuses.Count > 0)
        {
            query = query.Where(t => statuses.Contains(t.Status));
        }
        return query;
    }

    private static IQueryable<Domain.Entities.Task> ApplyPriorityFilter(
        IQueryable<Domain.Entities.Task> query,
        List<TaskPriority>? priorities)
    {
        if (priorities != null && priorities.Count > 0)
        {
            query = query.Where(t => priorities.Contains(t.Priority));
        }
        return query;
    }

    private static IQueryable<Domain.Entities.Task> ApplyOverdueFilter(
        IQueryable<Domain.Entities.Task> query,
        bool? overdueOnly)
    {
        if (overdueOnly == true)
        {
            var now = DateTime.UtcNow;
            query = query.Where(t => t.DueDateUtc < now && t.Status != TaskStatus.Completed && t.Status != TaskStatus.Cancelled);
        }
        return query;
    }

    private static IQueryable<Domain.Entities.Task> ApplyReminderSentFilter(
        IQueryable<Domain.Entities.Task> query,
        bool? reminderSent)
    {
        if (reminderSent.HasValue)
        {
            query = query.Where(t => t.ReminderSent == reminderSent.Value);
        }
        return query;
    }

    private static IQueryable<Domain.Entities.Task> ApplySearchFilter(
        IQueryable<Domain.Entities.Task> query,
        string? search)
    {
        if (!string.IsNullOrWhiteSpace(search))
        {
            var searchTerm = search.Trim().ToLowerInvariant();
            query = query.Where(t =>
                t.Title.ToLower().Contains(searchTerm) ||
                t.Description.ToLower().Contains(searchTerm));
        }
        return query;
    }

    public async Task<TaskResponse> UpdateTaskAsync(Guid id, TaskUpdateRequest request, byte[] rowVersion, CancellationToken cancellationToken = default)
    {
        var task = await _context.Tasks
            .Include(t => t.Owner)
            .Include(t => t.Assignee)
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);

        if (task == null)
        {
            _logger.LogWarning("UpdateTask: Task {TaskId} not found", id);
            throw new KeyNotFoundException($"Task with id {id} not found.");
        }

        using var scope = _logger.BeginScope(new Dictionary<string, object>
        {
            ["Action"] = "UpdateTask",
            ["TaskId"] = id
        });
        
        _logger.LogInformation("Updating task {TaskId}", id);

        // Set original RowVersion for EF Core concurrency check
        // EF Core will throw DbUpdateConcurrencyException if RowVersion doesn't match at SaveChanges
        _context.Entry(task).Property(t => t.RowVersion).OriginalValue = rowVersion;

        // Validate due date
        if (request.DueDateUtc < DateTime.UtcNow)
        {
            throw new InvalidOperationException("Due date must not be in the past (UTC).");
        }

        // Check if task is overdue and validate update rules
        if (task.IsOverdue())
        {
            // If still overdue after update, reject unless due date is changed to future
            if (request.DueDateUtc <= DateTime.UtcNow)
            {
                throw new InvalidOperationException("Cannot update overdue task unless due date is changed to a future date.");
            }
        }

        // Validate status transition
        if (!task.CanTransitionTo(request.Status))
        {
            throw new InvalidOperationException($"Invalid status transition from {task.Status} to {request.Status}.");
        }

        // Update assignee if provided
        if (request.AssignedUserId.HasValue)
        {
            var assignee = await _userService.GetUserByIdAsync(request.AssignedUserId.Value, cancellationToken);
            if (assignee == null)
            {
                throw new KeyNotFoundException($"User with id {request.AssignedUserId.Value} not found.");
            }
            task.AssignedUserId = request.AssignedUserId.Value;
        }

        // Update task properties
        task.Title = request.Title.Trim();
        task.Description = request.Description.Trim();
        task.DueDateUtc = request.DueDateUtc;
        task.Priority = request.Priority;
        task.Status = request.Status;
        // UpdatedAtUtc and RowVersion are set by UpdateTimestampInterceptor

        // Update overdue status if needed
        task.UpdateOverdueStatus();

        // EF Core will check RowVersion concurrency token here
        // If OriginalValue doesn't match current DB value, throws DbUpdateConcurrencyException
        await _context.SaveChangesAsync(cancellationToken);

        // Reload to get updated row version
        await _context.Entry(task).ReloadAsync(cancellationToken);
        await _context.Entry(task).Reference(t => t.Owner).LoadAsync(cancellationToken);
        await _context.Entry(task).Reference(t => t.Assignee).LoadAsync(cancellationToken);

        return MapToResponse(task);
    }

    public async Task<TaskResponse> UpdateTaskStatusAsync(Guid id, TaskStatus status, byte[] rowVersion, CancellationToken cancellationToken = default)
    {
        var task = await _context.Tasks
            .Include(t => t.Owner)
            .Include(t => t.Assignee)
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);

        if (task == null)
        {
            _logger.LogWarning("UpdateTaskStatus: Task {TaskId} not found", id);
            throw new KeyNotFoundException($"Task with id {id} not found.");
        }

        _logger.LogInformation("Updating task status. Id: {TaskId}, NewStatus: {Status}, OldStatus: {OldStatus}", 
            id, status, task.Status);

        // Set original RowVersion for EF Core concurrency check
        _context.Entry(task).Property(t => t.RowVersion).OriginalValue = rowVersion;

        // Validate status transition
        if (!task.CanTransitionTo(status))
        {
            throw new InvalidOperationException($"Invalid status transition from {task.Status} to {status}.");
        }

        task.Status = status;
        // UpdatedAtUtc and RowVersion are set by UpdateTimestampInterceptor
        task.UpdateOverdueStatus();

        // EF Core will check RowVersion concurrency token here
        await _context.SaveChangesAsync(cancellationToken);

        // Reload to get updated row version
        await _context.Entry(task).ReloadAsync(cancellationToken);
        await _context.Entry(task).Reference(t => t.Owner).LoadAsync(cancellationToken);
        await _context.Entry(task).Reference(t => t.Assignee).LoadAsync(cancellationToken);

        return MapToResponse(task);
    }

    public async Task<TaskResponse> UpdateTaskAssigneeAsync(Guid id, Guid? assignedUserId, byte[] rowVersion, CancellationToken cancellationToken = default)
    {
        var task = await _context.Tasks
            .Include(t => t.Owner)
            .Include(t => t.Assignee)
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);

        if (task == null)
        {
            _logger.LogWarning("UpdateTaskAssignee: Task {TaskId} not found", id);
            throw new KeyNotFoundException($"Task with id {id} not found.");
        }

        _logger.LogInformation("Updating task assignee. Id: {TaskId}, NewAssignee: {AssigneeId}", id, assignedUserId);

        // Set original RowVersion for EF Core concurrency check
        _context.Entry(task).Property(t => t.RowVersion).OriginalValue = rowVersion;

        if (assignedUserId.HasValue)
        {
            var assignee = await _userService.GetUserByIdAsync(assignedUserId.Value, cancellationToken);
            if (assignee == null)
            {
                throw new KeyNotFoundException($"User with id {assignedUserId.Value} not found.");
            }
            task.AssignedUserId = assignedUserId.Value;
        }
        else
        {
            task.AssignedUserId = null;
        }

        // UpdatedAtUtc and RowVersion are set by UpdateTimestampInterceptor

        // EF Core will check RowVersion concurrency token here
        await _context.SaveChangesAsync(cancellationToken);

        // Reload to get updated row version
        await _context.Entry(task).ReloadAsync(cancellationToken);
        await _context.Entry(task).Reference(t => t.Owner).LoadAsync(cancellationToken);
        await _context.Entry(task).Reference(t => t.Assignee).LoadAsync(cancellationToken);

        return MapToResponse(task);
    }

    public async System.Threading.Tasks.Task DeleteTaskAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var task = await _context.Tasks.FindAsync(new object[] { id }, cancellationToken);
        if (task == null)
        {
            _logger.LogWarning("DeleteTask: Task {TaskId} not found", id);
            throw new KeyNotFoundException($"Task with id {id} not found.");
        }

        _logger.LogInformation("Deleting task {TaskId}", id);

        _context.Tasks.Remove(task);
        await _context.SaveChangesAsync(cancellationToken);
    }

    private static IQueryable<Domain.Entities.Task> ApplySorting(IQueryable<Domain.Entities.Task> query, string? sortBy, string? sortDir)
    {
        var isDescending = sortDir?.ToLowerInvariant() == "desc";
        var sortByLower = sortBy?.ToLowerInvariant();

        return sortByLower switch
        {
            "duedateutc" or "duedate" => isDescending
                ? query.OrderByDescending(t => t.DueDateUtc)
                : query.OrderBy(t => t.DueDateUtc),
            "createdatutc" or "createdat" => isDescending
                ? query.OrderByDescending(t => t.CreatedAtUtc)
                : query.OrderBy(t => t.CreatedAtUtc),
            "priority" => isDescending
                ? query.OrderByDescending(t => t.Priority)
                : query.OrderBy(t => t.Priority),
            "status" => isDescending
                ? query.OrderByDescending(t => t.Status)
                : query.OrderBy(t => t.Status),
            "title" => isDescending
                ? query.OrderByDescending(t => t.Title)
                : query.OrderBy(t => t.Title),
            _ => query.OrderBy(t => t.DueDateUtc) // Default sort
        };
    }

    private static TaskResponse MapToResponse(Domain.Entities.Task task)
    {
        return new TaskResponse
        {
            Id = task.Id,
            Title = task.Title,
            Description = task.Description,
            DueDateUtc = task.DueDateUtc,
            Priority = task.Priority,
            Status = task.Status,
            ReminderSent = task.ReminderSent,
            Owner = new UserRefDto
            {
                Id = task.Owner.Id,
                FullName = task.Owner.FullName,
                Email = task.Owner.Email,
                Telephone = task.Owner.Telephone
            },
            Assignee = task.Assignee != null
                ? new UserRefDto
                {
                    Id = task.Assignee.Id,
                    FullName = task.Assignee.FullName,
                    Email = task.Assignee.Email,
                    Telephone = task.Assignee.Telephone
                }
                : new UserRefDto(), // Should not happen, but handle gracefully
            CreatedAtUtc = task.CreatedAtUtc,
            UpdatedAtUtc = task.UpdatedAtUtc,
            RowVersion = RowVersionHelper.ToBase64String(task.RowVersion)
        };
    }
}

