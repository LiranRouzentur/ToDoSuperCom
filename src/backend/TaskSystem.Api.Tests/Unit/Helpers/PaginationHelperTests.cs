using FluentAssertions;
using TaskApp.Application.Helpers;

namespace TaskSystem.Api.Tests.Unit.Helpers;

public class PaginationHelperTests
{
    [Fact]
    public void NormalizePagination_WithValidValues_ReturnsNormalizedValues()
    {
        // Arrange & Act
        var (page, pageSize) = PaginationHelper.NormalizePagination(2, 50);

        // Assert
        page.Should().Be(2);
        pageSize.Should().Be(50);
    }

    [Fact]
    public void NormalizePagination_WithNullValues_ReturnsDefaults()
    {
        // Arrange & Act
        var (page, pageSize) = PaginationHelper.NormalizePagination(null, null);

        // Assert
        page.Should().Be(1);
        pageSize.Should().Be(PaginationHelper.DefaultPageSize);
    }

    [Fact]
    public void NormalizePagination_WithPageLessThanOne_ReturnsOne()
    {
        // Arrange & Act
        var (page, pageSize) = PaginationHelper.NormalizePagination(0, 20);

        // Assert
        page.Should().Be(1);
    }

    [Fact]
    public void NormalizePagination_WithPageSizeGreaterThanMax_ReturnsMax()
    {
        // Arrange & Act
        var (page, pageSize) = PaginationHelper.NormalizePagination(1, 200);

        // Assert
        pageSize.Should().Be(PaginationHelper.MaxPageSize);
    }

    [Fact]
    public void NormalizePagination_WithPageSizeLessThanOne_ReturnsOne()
    {
        // Arrange & Act
        var (page, pageSize) = PaginationHelper.NormalizePagination(1, 0);

        // Assert
        pageSize.Should().Be(1);
    }

    [Fact]
    public void CalculateTotalPages_WithValidValues_ReturnsCorrectPages()
    {
        // Arrange & Act
        var totalPages = PaginationHelper.CalculateTotalPages(100, 20);

        // Assert
        totalPages.Should().Be(5);
    }

    [Fact]
    public void CalculateTotalPages_WithRemainder_RoundsUp()
    {
        // Arrange & Act
        var totalPages = PaginationHelper.CalculateTotalPages(101, 20);

        // Assert
        totalPages.Should().Be(6);
    }

    [Fact]
    public void CalculateTotalPages_WithZeroPageSize_ReturnsZero()
    {
        // Arrange & Act
        var totalPages = PaginationHelper.CalculateTotalPages(100, 0);

        // Assert
        totalPages.Should().Be(0);
    }

    [Fact]
    public void CalculateTotalPages_WithZeroItems_ReturnsZero()
    {
        // Arrange & Act
        var totalPages = PaginationHelper.CalculateTotalPages(0, 20);

        // Assert
        totalPages.Should().Be(0);
    }
}

