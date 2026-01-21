using FluentAssertions;
using TaskApp.Application.Helpers;

namespace TaskSystem.Api.Tests.Unit.Helpers;

public class PhoneNumberHelperTests
{
    [Theory]
    [InlineData("+972501234567", true)]
    [InlineData("0501234567", true)]
    [InlineData("+972212345678", false)]
    [InlineData("0212345678", false)]
    [InlineData("+972551234567", true)]
    [InlineData("0551234567", true)]
    [InlineData("+972991234567", false)]
    [InlineData("0991234567", false)]
    [InlineData("1234567890", false)]
    [InlineData("+1234567890", false)]
    [InlineData("", false)]
    [InlineData("   ", false)]
    [InlineData("+97250123456", false)] // Too short
    [InlineData("+9725012345678", false)] // Too long
    public void IsValidIsraeliPhone_WithVariousInputs_ReturnsExpectedResult(string phone, bool expected)
    {
        // Arrange & Act
        var result = PhoneNumberHelper.IsValidIsraeliPhone(phone);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("0501234567", "+972501234567")]
    [InlineData("+972501234567", "+972501234567")]
    [InlineData("0212345678", "+972212345678")]
    [InlineData("+972212345678", "+972212345678")]
    public void NormalizePhone_WithValidInput_ReturnsNormalized(string input, string expected)
    {
        // Arrange & Act
        var result = PhoneNumberHelper.NormalizePhone(input);

        // Assert
        result.Should().Be(expected);
    }

    [Fact]
    public void NormalizePhone_WithNull_ReturnsNull()
    {
        // Arrange & Act
        var result = PhoneNumberHelper.NormalizePhone(null);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void NormalizePhone_WithWhitespace_ReturnsWhitespace()
    {
        // Arrange & Act
        var result = PhoneNumberHelper.NormalizePhone("   ");

        // Assert
        result.Should().Be("   ");
    }
}

