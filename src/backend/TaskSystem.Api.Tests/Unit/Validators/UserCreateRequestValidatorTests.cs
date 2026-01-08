using FluentAssertions;
using FluentValidation.TestHelper;
using TaskApp.Application.DTOs;
using TaskApp.Application.Validators;

namespace TaskSystem.Api.Tests.Unit.Validators;

public class UserCreateRequestValidatorTests
{
    private readonly UserCreateRequestValidator _validator;

    public UserCreateRequestValidatorTests()
    {
        _validator = new UserCreateRequestValidator();
    }

    [Fact]
    public void Validate_WithValidRequest_ShouldPass()
    {
        // Arrange
        var request = new UserCreateRequest
        {
            FullName = "John Doe",
            Email = "john@example.com",
            Telephone = "+972501234567"
        };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WithEmptyFullName_ShouldFail()
    {
        // Arrange
        var request = new UserCreateRequest
        {
            FullName = "",
            Email = "john@example.com",
            Telephone = "+972501234567"
        };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.FullName);
    }

    [Fact]
    public void Validate_WithFullNameExceedingMaxLength_ShouldFail()
    {
        // Arrange
        var request = new UserCreateRequest
        {
            FullName = new string('A', 101),
            Email = "john@example.com",
            Telephone = "+972501234567"
        };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.FullName)
            .WithErrorMessage("Full name must not exceed 100 characters.");
    }

    [Fact]
    public void Validate_WithInvalidEmail_ShouldFail()
    {
        // Arrange
        var request = new UserCreateRequest
        {
            FullName = "John Doe",
            Email = "invalid-email",
            Telephone = "+972501234567"
        };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Email)
            .WithErrorMessage("Email must be a valid email address.");
    }

    [Fact]
    public void Validate_WithEmptyEmail_ShouldFail()
    {
        // Arrange
        var request = new UserCreateRequest
        {
            FullName = "John Doe",
            Email = "",
            Telephone = "+972501234567"
        };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Email);
    }

    [Fact]
    public void Validate_WithInvalidPhoneNumber_ShouldFail()
    {
        // Arrange
        var request = new UserCreateRequest
        {
            FullName = "John Doe",
            Email = "john@example.com",
            Telephone = "1234567890"
        };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Telephone)
            .WithErrorMessage("Telephone must be a valid Israeli phone number.");
    }

    [Fact]
    public void Validate_WithEmptyTelephone_ShouldFail()
    {
        // Arrange
        var request = new UserCreateRequest
        {
            FullName = "John Doe",
            Email = "john@example.com",
            Telephone = ""
        };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Telephone);
    }
}

