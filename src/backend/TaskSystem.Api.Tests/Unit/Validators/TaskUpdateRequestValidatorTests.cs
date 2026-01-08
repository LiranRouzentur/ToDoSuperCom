using FluentAssertions;
using FluentValidation.TestHelper;
using TaskApp.Application.DTOs;
using TaskApp.Application.Validators;
using TaskApp.Domain.Enums;
using TaskStatusEnum = TaskApp.Domain.Enums.TaskStatus;

namespace TaskSystem.Api.Tests.Unit.Validators;

public class TaskUpdateRequestValidatorTests
{
    private readonly TaskUpdateRequestValidator _validator;

    public TaskUpdateRequestValidatorTests()
    {
        _validator = new TaskUpdateRequestValidator();
    }

    [Fact]
    public void Validate_WithValidRequest_ShouldPass()
    {
        // Arrange
        var request = new TaskUpdateRequest
        {
            Title = "Updated Task",
            Description = "Updated Description",
            DueDateUtc = DateTime.UtcNow.AddDays(1),
            Priority = TaskPriority.High,
            Status = TaskStatusEnum.InProgress
        };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WithDueDateInPast_ShouldFail()
    {
        // Arrange
        var request = new TaskUpdateRequest
        {
            Title = "Updated Task",
            Description = "Updated Description",
            DueDateUtc = DateTime.UtcNow.AddDays(-1),
            Priority = TaskPriority.Medium,
            Status = TaskStatusEnum.Open
        };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.DueDateUtc)
            .WithErrorMessage("Due date must not be in the past (UTC).");
    }

    [Fact]
    public void Validate_WithEmptyTitle_ShouldFail()
    {
        // Arrange
        var request = new TaskUpdateRequest
        {
            Title = "",
            Description = "Updated Description",
            DueDateUtc = DateTime.UtcNow.AddDays(1),
            Priority = TaskPriority.Medium,
            Status = TaskStatusEnum.Open
        };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Title);
    }
}

