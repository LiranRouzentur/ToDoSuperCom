using FluentAssertions;
using FluentValidation.TestHelper;
using TaskApp.Application.DTOs;
using TaskApp.Application.Validators;
using TaskApp.Domain.Enums;

namespace TaskSystem.Api.Tests.Unit.Validators;

public class TaskCreateRequestValidatorTests
{
    private readonly TaskCreateRequestValidator _validator;

    public TaskCreateRequestValidatorTests()
    {
        _validator = new TaskCreateRequestValidator();
    }

    [Fact]
    public void Validate_WithValidRequest_ShouldPass()
    {
        // Arrange
        var request = new TaskCreateRequest
        {
            Title = "Test Task",
            Description = "Test Description",
            DueDateUtc = DateTime.UtcNow.AddDays(1),
            Priority = TaskPriority.Medium,
            Owner = new UserRefDto
            {
                FullName = "John Doe",
                Email = "john@example.com",
                Telephone = "+972501234567"
            }
        };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WithEmptyTitle_ShouldFail()
    {
        // Arrange
        var request = new TaskCreateRequest
        {
            Title = "",
            Description = "Test Description",
            DueDateUtc = DateTime.UtcNow.AddDays(1),
            Priority = TaskPriority.Medium,
            Owner = new UserRefDto
            {
                FullName = "John Doe",
                Email = "john@example.com",
                Telephone = "+972501234567"
            }
        };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Title);
    }

    [Fact]
    public void Validate_WithTitleExceedingMaxLength_ShouldFail()
    {
        // Arrange
        var request = new TaskCreateRequest
        {
            Title = new string('A', 51),
            Description = "Test Description",
            DueDateUtc = DateTime.UtcNow.AddDays(1),
            Priority = TaskPriority.Medium,
            Owner = new UserRefDto
            {
                FullName = "John Doe",
                Email = "john@example.com",
                Telephone = "+972501234567"
            }
        };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Title)
            .WithErrorMessage("Title must not exceed 50 characters.");
    }

    [Fact]
    public void Validate_WithEmptyDescription_ShouldFail()
    {
        // Arrange
        var request = new TaskCreateRequest
        {
            Title = "Test Task",
            Description = "",
            DueDateUtc = DateTime.UtcNow.AddDays(1),
            Priority = TaskPriority.Medium,
            Owner = new UserRefDto
            {
                FullName = "John Doe",
                Email = "john@example.com",
                Telephone = "+972501234567"
            }
        };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Description);
    }

    [Fact]
    public void Validate_WithDescriptionExceedingMaxLength_ShouldFail()
    {
        // Arrange
        var request = new TaskCreateRequest
        {
            Title = "Test Task",
            Description = new string('A', 251),
            DueDateUtc = DateTime.UtcNow.AddDays(1),
            Priority = TaskPriority.Medium,
            Owner = new UserRefDto
            {
                FullName = "John Doe",
                Email = "john@example.com",
                Telephone = "+972501234567"
            }
        };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Description)
            .WithErrorMessage("Description must not exceed 250 characters.");
    }

    [Fact]
    public void Validate_WithDueDateInPast_ShouldFail()
    {
        // Arrange
        var request = new TaskCreateRequest
        {
            Title = "Test Task",
            Description = "Test Description",
            DueDateUtc = DateTime.UtcNow.AddDays(-1),
            Priority = TaskPriority.Medium,
            Owner = new UserRefDto
            {
                FullName = "John Doe",
                Email = "john@example.com",
                Telephone = "+972501234567"
            }
        };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.DueDateUtc)
            .WithErrorMessage("Due date must be in the future (UTC).");
    }

    [Fact]
    public void Validate_WithNullOwner_ShouldFail()
    {
        // Arrange
        var request = new TaskCreateRequest
        {
            Title = "Test Task",
            Description = "Test Description",
            DueDateUtc = DateTime.UtcNow.AddDays(1),
            Priority = TaskPriority.Medium,
            Owner = null!
        };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Owner);
    }
}

