using System.Net;
using System.Text.Json;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Hosting;

namespace TaskApp.Api.Middleware;

public class ErrorHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ErrorHandlingMiddleware> _logger;

    public ErrorHandlingMiddleware(RequestDelegate next, ILogger<ErrorHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Generate correlation ID for request tracing
        var correlationId = context.Request.Headers["X-Correlation-Id"].FirstOrDefault() 
            ?? Guid.NewGuid().ToString();
        context.Items["CorrelationId"] = correlationId;
        context.Response.Headers["X-Correlation-Id"] = correlationId;

        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex, correlationId);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception, string correlationId)
    {
        // Extract task/user IDs from context if available for better traceability
        var taskId = context.Request.RouteValues["id"]?.ToString();
        var path = context.Request.Path.Value ?? string.Empty;
        
        // Log full exception details server-side (for debugging)
        _logger.LogError(exception, 
            "Error processing request. CorrelationId: {CorrelationId}, Path: {Path}, TaskId: {TaskId}", 
            correlationId, path, taskId);

        var response = context.Response;
        response.ContentType = "application/json";

        var errorResponse = new ErrorResponse
        {
            Error = new ErrorDetail
            {
                CorrelationId = correlationId
            }
        };

        switch (exception)
        {
            case ValidationException validationException:
                response.StatusCode = (int)HttpStatusCode.BadRequest;
                errorResponse.Error.Code = "VALIDATION_ERROR";
                errorResponse.Error.Message = "One or more validation errors occurred.";
                errorResponse.Error.Details = validationException.Errors.Select(e => new ErrorFieldDetail
                {
                    Field = e.PropertyName,
                    Message = e.ErrorMessage
                }).ToList();
                break;

            case KeyNotFoundException:
                response.StatusCode = (int)HttpStatusCode.NotFound;
                errorResponse.Error.Code = "NOT_FOUND";
                errorResponse.Error.Message = exception.Message;
                break;

            case DbUpdateConcurrencyException concurrencyEx:
                response.StatusCode = (int)HttpStatusCode.Conflict;
                errorResponse.Error.Code = "CONCURRENCY_CONFLICT";
                errorResponse.Error.Message = "The resource was modified by another process.";
                _logger.LogWarning(concurrencyEx, 
                    "Concurrency conflict detected. CorrelationId: {CorrelationId}, Path: {Path}", 
                    correlationId, context.Request.Path);
                break;

            case InvalidOperationException:
                response.StatusCode = (int)HttpStatusCode.BadRequest;
                errorResponse.Error.Code = "INVALID_OPERATION";
                errorResponse.Error.Message = exception.Message;
                break;

            default:
                response.StatusCode = (int)HttpStatusCode.InternalServerError;
                errorResponse.Error.Code = "INTERNAL_ERROR";
                // Only show detailed error messages in Development
                errorResponse.Error.Message = context.RequestServices.GetRequiredService<IWebHostEnvironment>().IsDevelopment()
                    ? exception.Message
                    : "An error occurred while processing your request.";
                break;
        }

        var jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        await response.WriteAsync(JsonSerializer.Serialize(errorResponse, jsonOptions));
    }
}

public class ErrorResponse
{
    public ErrorDetail Error { get; set; } = null!;
}

public class ErrorDetail
{
    public string Code { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public List<ErrorFieldDetail>? Details { get; set; }
    public string CorrelationId { get; set; } = string.Empty;
    public object? Current { get; set; }
}

public class ErrorFieldDetail
{
    public string Field { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}

