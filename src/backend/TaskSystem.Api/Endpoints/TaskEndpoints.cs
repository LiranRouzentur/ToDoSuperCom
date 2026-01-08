using Microsoft.AspNetCore.Mvc;
using TaskApp.Application.DTOs;
using TaskApp.Application.Services;
using TaskApp.Domain.Enums;
using TaskStatus = TaskApp.Domain.Enums.TaskStatus;

namespace TaskApp.Api.Endpoints;

public static class TaskEndpoints
{
    public static void MapTasksEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/tasks")
            .WithTags("Tasks");

        group.MapPost("/", CreateTask)
            .WithName("CreateTask")
            .Produces<TaskResponse>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status400BadRequest);

        group.MapGet("/{id:guid}", GetTaskById)
            .WithName("GetTaskById")
            .Produces<TaskResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapGet("/", ListTasks)
            .WithName("ListTasks")
            .Produces<PagedResponse<TaskResponse>>(StatusCodes.Status200OK);

        group.MapPut("/{id:guid}", UpdateTask)
            .WithName("UpdateTask")
            .Produces<TaskResponse>(StatusCodes.Status200OK)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapPatch("/{id:guid}/status", UpdateTaskStatus)
            .WithName("UpdateTaskStatus")
            .Produces<TaskResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapPatch("/{id:guid}/assignee", UpdateTaskAssignee)
            .WithName("UpdateTaskAssignee")
            .Produces<TaskResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapDelete("/{id:guid}", DeleteTask)
            .WithName("DeleteTask")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound);
    }

    private static async Task<IResult> CreateTask(
        [FromBody] TaskCreateRequest request,
        [FromServices] ITaskService service,
        CancellationToken cancellationToken)
    {
        var task = await service.CreateTaskAsync(request, cancellationToken);
        return Results.Created($"/api/tasks/{task.Id}", task);
    }

    private static async Task<IResult> GetTaskById(
        [FromRoute] Guid id,
        [FromServices] ITaskService service,
        CancellationToken cancellationToken)
    {
        var task = await service.GetTaskByIdAsync(id, cancellationToken);
        return task != null ? Results.Ok(task) : Results.NotFound();
    }

    private static async Task<IResult> ListTasks(
        [FromServices] ITaskService service,
        [FromQuery] string? scope,
        [FromQuery] string? ownerUserId,
        [FromQuery] string? assignedUserId,
        [FromQuery] string? status, // Comma separated
        [FromQuery] string? priority, // Comma separated
        [FromQuery] bool? overdueOnly,
        [FromQuery] bool? reminderSent,
        [FromQuery] string? search,
        [FromQuery] string? sortBy,
        [FromQuery] string? sortDir,
        [FromQuery] int? page,
        [FromQuery] int? pageSize,
        CancellationToken cancellationToken)
    {
        TaskScope? scopeEnum = null;
        if (!string.IsNullOrEmpty(scope) && Enum.TryParse<TaskScope>(scope, true, out var parsedScope))
        {
            scopeEnum = parsedScope;
        }

        Guid? ownerUserIdGuid = null;
        if (Guid.TryParse(ownerUserId, out var oId)) ownerUserIdGuid = oId;

        Guid? assignedUserIdGuid = null;
        if (Guid.TryParse(assignedUserId, out var aId)) assignedUserIdGuid = aId;

        List<TaskStatus>? statusList = null;
        if (!string.IsNullOrEmpty(status))
        {
            statusList = status.Split(',')
                .Select(s => Enum.Parse<TaskStatus>(s, true))
                .ToList();
        }

        List<TaskPriority>? priorityList = null;
        if (!string.IsNullOrEmpty(priority))
        {
            priorityList = priority.Split(',')
                .Select(p => Enum.Parse<TaskPriority>(p, true))
                .ToList();
        }

        var result = await service.ListTasksAsync(
            scopeEnum,
            ownerUserIdGuid,
            assignedUserIdGuid,
            statusList,
            priorityList,
            overdueOnly,
            reminderSent,
            search,
            sortBy,
            sortDir,
            page,
            pageSize,
            cancellationToken);

        return Results.Ok(result);
    }

    private static async Task<IResult> UpdateTask(
        [FromRoute] Guid id,
        [FromBody] TaskUpdateRequest request,
        [FromHeader(Name = "If-Match")] string? ifMatch,
        [FromServices] ITaskService service,
        CancellationToken cancellationToken)
    {
        if (ifMatch is null)
        {
            return Results.Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Missing If-Match header",
                detail: "If-Match header with current RowVersion is required for concurrency control.");
        }

        try
        {
            var rowVersion = Convert.FromBase64String(ifMatch.Trim('"'));
            var task = await service.UpdateTaskAsync(id, request, rowVersion, cancellationToken);
            return Results.Ok(task);
        }
        catch (FormatException)
        {
             return Results.Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Invalid RowVersion",
                detail: "If-Match header must be a valid Base64 string.");
        }
        catch (KeyNotFoundException)
        {
            return Results.NotFound();
        }
        catch (Microsoft.EntityFrameworkCore.DbUpdateConcurrencyException)
        {
            return Results.Conflict(new ProblemDetails
            {
                Title = "Concurrency Conflict",
                Detail = "The task has been modified by another user. Please reload and try again."
            });
        }
    }

    private static async Task<IResult> UpdateTaskStatus(
        [FromRoute] Guid id,
        [FromBody] TaskStatusUpdateRequest request,
        [FromHeader(Name = "If-Match")] string? ifMatch,
        [FromServices] ITaskService service,
        CancellationToken cancellationToken)
    {
        // If DTO doesn't exist, we might need to create it or bind just the enum if strictly adhering to Minimal API simplicity.
        // But let's assume body contains { "status": "InProgress" }
        
        if (ifMatch is null)
        {
            return Results.Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Missing If-Match header",
                detail: "If-Match header with current RowVersion is required for concurrency control.");
        }

        try
        {
            var rowVersion = Convert.FromBase64String(ifMatch.Trim('"'));
            var task = await service.UpdateTaskStatusAsync(id, request.Status, rowVersion, cancellationToken);
            return Results.Ok(task);
        }
        catch (FormatException)
        {
             return Results.Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Invalid RowVersion",
                detail: "If-Match header must be a valid Base64 string.");
        }
        catch (KeyNotFoundException)
        {
            return Results.NotFound();
        }
        catch (Microsoft.EntityFrameworkCore.DbUpdateConcurrencyException)
        {
            return Results.Conflict(new ProblemDetails
            {
                Title = "Concurrency Conflict",
                Detail = "The task has been modified by another user. Please reload and try again."
            });
        }
    }

    private static async Task<IResult> UpdateTaskAssignee(
        [FromRoute] Guid id,
        [FromBody] TaskAssigneeUpdateRequest request,
        [FromHeader(Name = "If-Match")] string? ifMatch,
        [FromServices] ITaskService service,
        CancellationToken cancellationToken)
    {
        if (ifMatch is null)
        {
             return Results.Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Missing If-Match header",
                detail: "If-Match header with current RowVersion is required for concurrency control.");
        }

        try
        {
            var rowVersion = Convert.FromBase64String(ifMatch.Trim('"'));
            var task = await service.UpdateTaskAssigneeAsync(id, request.AssignedUserId, rowVersion, cancellationToken);
            return Results.Ok(task);
        }
        catch (FormatException)
        {
             return Results.Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Invalid RowVersion",
                detail: "If-Match header must be a valid Base64 string.");
        }
        catch (KeyNotFoundException)
        {
            return Results.NotFound();
        }
        catch (Microsoft.EntityFrameworkCore.DbUpdateConcurrencyException)
        {
             return Results.Conflict(new ProblemDetails
            {
                Title = "Concurrency Conflict",
                Detail = "The task has been modified by another user. Please reload and try again."
            });
        }
    }

    private static async Task<IResult> DeleteTask(
        [FromRoute] Guid id,
        [FromServices] ITaskService service,
        CancellationToken cancellationToken)
    {
        try
        {
            await service.DeleteTaskAsync(id, cancellationToken);
            return Results.NoContent();
        }
        catch (KeyNotFoundException)
        {
            return Results.NotFound();
        }
    }
}

// Helper DTOs for PATCH if they don't exist in TaskApp.Application.DTOs
// Using existing DTOs from TaskApp.Application.DTOs namespace

