using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using TaskApp.Application.DTOs;
using TaskApp.Application.Services;
using TaskApp.Domain.Entities;
using TaskApp.Infrastructure.Data;
using TaskSystem.Api.Tests.TestHelpers;
using SystemTask = System.Threading.Tasks.Task;

namespace TaskSystem.Api.Tests.Unit.Services;

public class UserServiceTests : IDisposable
{
    private readonly TaskDbContext _context;
    private readonly UserService _service;

    public UserServiceTests()
    {
        _context = MockDbContextFactory.CreateInMemoryContext();
        _service = new UserService(_context);
    }

    [Fact]
    public async SystemTask CreateUserAsync_WithValidRequest_ReturnsUserResponse()
    {
        // Arrange
        var request = new UserCreateRequest
        {
            FullName = "John Doe",
            Email = "john@example.com",
            Telephone = "+972501234567"
        };

        // Act
        var result = await _service.CreateUserAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().NotBeEmpty();
        result.FullName.Should().Be("John Doe");
        result.Email.Should().Be("john@example.com");
        result.Telephone.Should().Be("+972501234567");
    }

    [Fact]
    public async SystemTask GetUserByIdAsync_WithExistingId_ReturnsUserResponse()
    {
        // Arrange
        var user = new User
        {
            Id = Guid.NewGuid(),
            FullName = "John Doe",
            Email = "john@example.com",
            Telephone = "+972501234567",
            CreatedAtUtc = DateTime.UtcNow
        };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetUserByIdAsync(user.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(user.Id);
        result.FullName.Should().Be("John Doe");
    }

    [Fact]
    public async SystemTask GetUserByIdAsync_WithNonExistentId_ReturnsNull()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act
        var result = await _service.GetUserByIdAsync(nonExistentId);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async SystemTask GetUserByEmailAsync_WithExistingEmail_ReturnsUserResponse()
    {
        // Arrange
        var user = new User
        {
            Id = Guid.NewGuid(),
            FullName = "John Doe",
            Email = "john@example.com",
            Telephone = "+972501234567",
            CreatedAtUtc = DateTime.UtcNow
        };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetUserByEmailAsync("john@example.com");

        // Assert
        result.Should().NotBeNull();
        result!.Email.Should().Be("john@example.com");
    }

    [Fact]
    public async SystemTask UpsertUserByEmailAsync_WithNewEmail_CreatesUser()
    {
        // Arrange
        var request = new UserCreateRequest
        {
            FullName = "John Doe",
            Email = "john@example.com",
            Telephone = "+972501234567"
        };

        // Act
        var result = await _service.UpsertUserByEmailAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Email.Should().Be("john@example.com");
        var userInDb = await _context.Users.FirstOrDefaultAsync(u => u.Email == "john@example.com");
        userInDb.Should().NotBeNull();
    }

    [Fact]
    public async SystemTask UpsertUserByEmailAsync_WithExistingEmail_UpdatesUser()
    {
        // Arrange
        var existingUser = new User
        {
            Id = Guid.NewGuid(),
            FullName = "John Doe",
            Email = "john@example.com",
            Telephone = "+972501234567",
            CreatedAtUtc = DateTime.UtcNow
        };
        _context.Users.Add(existingUser);
        await _context.SaveChangesAsync();

        var request = new UserCreateRequest
        {
            FullName = "John Updated",
            Email = "john@example.com",
            Telephone = "+972999999999"
        };

        // Act
        var result = await _service.UpsertUserByEmailAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.FullName.Should().Be("John Updated");
        result.Telephone.Should().Be("+972999999999");
        var userInDb = await _context.Users.FirstOrDefaultAsync(u => u.Email == "john@example.com");
        userInDb!.FullName.Should().Be("John Updated");
    }

    [Fact]
    public async SystemTask ListUsersAsync_WithSearchTerm_ReturnsFilteredResults()
    {
        // Arrange
        var users = new[]
        {
            new User { Id = Guid.NewGuid(), FullName = "John Doe", Email = "john@example.com", Telephone = "+972501234567", CreatedAtUtc = DateTime.UtcNow },
            new User { Id = Guid.NewGuid(), FullName = "Jane Smith", Email = "jane@example.com", Telephone = "+972502345678", CreatedAtUtc = DateTime.UtcNow }
        };
        _context.Users.AddRange(users);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.ListUsersAsync("John", 1, 10);

        // Assert
        result.Items.Should().HaveCount(1);
        result.Items[0].FullName.Should().Be("John Doe");
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}

