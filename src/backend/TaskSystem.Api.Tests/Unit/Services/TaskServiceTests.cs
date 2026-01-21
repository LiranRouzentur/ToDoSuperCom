using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using TaskApp.Application.DTOs;
using TaskApp.Application.Services;
using TaskApp.Domain.Entities;
using TaskApp.Domain.Enums;
using TaskApp.Infrastructure.Data;
using TaskSystem.Api.Tests.TestHelpers;
using Task = TaskApp.Domain.Entities.Task;
using TaskStatus = TaskApp.Domain.Enums.TaskStatus;
using SystemTask = System.Threading.Tasks.Task;

namespace TaskSystem.Api.Tests.Unit.Services;

public class TaskServiceTests : IDisposable
{
    private readonly TaskDbContext _context;
    private readonly Mock<IUserService> _userServiceMock;
    private readonly Mock<ILogger<TaskService>> _loggerMock;
    private readonly TaskService _service;

    public TaskServiceTests()
    {
        _context = MockDbContextFactory.CreateInMemoryContext();
        _userServiceMock = new Mock<IUserService>();
        _loggerMock = new Mock<ILogger<TaskService>>();
        _service = new TaskService(_context, _userServiceMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async SystemTask CreateTaskAsync_WithValidRequest_ReturnsTaskResponse()
    {
        // Arrange
        var owner = new UserResponse
        {
            Id = Guid.NewGuid(),
            FullName = "John Doe",
            Email = "john@example.com",
            Telephone = "+972501234567",
            CreatedAtUtc = DateTime.UtcNow
        };

        _userServiceMock.Setup(x => x.UpsertUserByEmailAsync(It.IsAny<UserCreateRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(owner);

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

        // Seed the user in the context so the service can load it via navigation property
        var userEntity = new User
        {
            Id = owner.Id,
            FullName = owner.FullName,
            Email = owner.Email,
            Telephone = owner.Telephone,
            CreatedAtUtc = owner.CreatedAtUtc
        };
        _context.Users.Add(userEntity);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.CreateTaskAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Title.Should().Be("Test Task");
        result.Description.Should().Be("Test Description");
        result.Owner.Id.Should().Be(owner.Id);
    }

    [Fact]
    public async SystemTask CreateTaskAsync_WithDueDateInPast_ThrowsInvalidOperationException()
    {
        // Arrange
        var owner = new UserResponse
        {
            Id = Guid.NewGuid(),
            FullName = "John Doe",
            Email = "john@example.com",
            Telephone = "+972501234567",
            CreatedAtUtc = DateTime.UtcNow
        };

        _userServiceMock.Setup(x => x.UpsertUserByEmailAsync(It.IsAny<UserCreateRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(owner);

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

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => _service.CreateTaskAsync(request));
    }

    [Fact]
    public async SystemTask GetTaskByIdAsync_WithExistingId_ReturnsTaskResponse()
    {
        // Arrange
        var owner = new User
        {
            Id = Guid.NewGuid(),
            FullName = "John Doe",
            Email = "john@example.com",
            Telephone = "+972501234567",
            CreatedAtUtc = DateTime.UtcNow
        };
        _context.Users.Add(owner);

        var task = new Task
        {
            Id = Guid.NewGuid(),
            Title = "Test Task",
            Description = "Test Description",
            DueDateUtc = DateTime.UtcNow.AddDays(1),
            Priority = TaskPriority.Medium,
            Status = TaskStatus.Open,
            OwnerUserId = owner.Id,
            AssignedUserId = owner.Id,
            ReminderSent = false,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow,
            Owner = owner
        };
        _context.Tasks.Add(task);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetTaskByIdAsync(task.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Title.Should().Be("Test Task");
    }

    [Fact]
    public async SystemTask GetTaskByIdAsync_WithNonExistentId_ReturnsNull()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act
        var result = await _service.GetTaskByIdAsync(nonExistentId);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async SystemTask UpdateTaskAsync_WithOverdueTaskAndNoDueDateChange_ThrowsInvalidOperationException()
    {
        // Arrange
        var owner = new User
        {
            Id = Guid.NewGuid(),
            FullName = "John Doe",
            Email = "john@example.com",
            Telephone = "+972501234567",
            CreatedAtUtc = DateTime.UtcNow
        };
        _context.Users.Add(owner);

        var task = new Task
        {
            Id = Guid.NewGuid(),
            Title = "Overdue Task",
            Description = "Description",
            DueDateUtc = DateTime.UtcNow.AddDays(-1), // Overdue
            Priority = TaskPriority.Medium,
            Status = TaskStatus.Open,
            OwnerUserId = owner.Id,
            AssignedUserId = owner.Id,
            ReminderSent = false,
            CreatedAtUtc = DateTime.UtcNow.AddDays(-2),
            UpdatedAtUtc = DateTime.UtcNow.AddDays(-2),
            Owner = owner
        };
        _context.Tasks.Add(task);
        await _context.SaveChangesAsync();

        var updateRequest = new TaskUpdateRequest
        {
            Title = "Updated Task",
            Description = "Updated Description",
            DueDateUtc = DateTime.UtcNow.AddDays(-1), // Still overdue
            Priority = TaskPriority.High,
            Status = TaskStatus.InProgress
        };

        var rowVersion = task.RowVersion;

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => 
            _service.UpdateTaskAsync(task.Id, updateRequest, rowVersion));
    }

    [Fact]
    public async SystemTask DeleteTaskAsync_WithExistingTask_DeletesTask()
    {
        // Arrange
        var owner = new User
        {
            Id = Guid.NewGuid(),
            FullName = "John Doe",
            Email = "john@example.com",
            Telephone = "+972501234567",
            CreatedAtUtc = DateTime.UtcNow
        };
        _context.Users.Add(owner);

        var task = new Task
        {
            Id = Guid.NewGuid(),
            Title = "Test Task",
            Description = "Test Description",
            DueDateUtc = DateTime.UtcNow.AddDays(1),
            Priority = TaskPriority.Medium,
            Status = TaskStatus.Open,
            OwnerUserId = owner.Id,
            AssignedUserId = owner.Id,
            ReminderSent = false,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow,
            Owner = owner
        };
        _context.Tasks.Add(task);
        await _context.SaveChangesAsync();

        // Act
        await _service.DeleteTaskAsync(task.Id);

        // Assert
        var deletedTask = await _context.Tasks.FindAsync(task.Id);
        deletedTask.Should().BeNull();
    }

    [Fact]
    public async SystemTask DeleteTaskAsync_WithNonExistentTask_ThrowsKeyNotFoundException()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() => _service.DeleteTaskAsync(nonExistentId));
    }

    [Fact]
    public async SystemTask ListTasksAsync_WithFilters_ReturnsFilteredResults()
    {
        // Arrange
        var owner = new User
        {
            Id = Guid.NewGuid(),
            FullName = "John Doe",
            Email = "john@example.com",
            Telephone = "+972501234567",
            CreatedAtUtc = DateTime.UtcNow
        };
        _context.Users.Add(owner);

        var tasks = new[]
        {
            new Task
            {
                Id = Guid.NewGuid(),
                Title = "Task 1",
                Description = "Description 1",
                DueDateUtc = DateTime.UtcNow.AddDays(1),
                Priority = TaskPriority.High,
                Status = TaskStatus.Open,
                OwnerUserId = owner.Id,
                AssignedUserId = owner.Id,
                ReminderSent = false,
                CreatedAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = DateTime.UtcNow,
                Owner = owner
            },
            new Task
            {
                Id = Guid.NewGuid(),
                Title = "Task 2",
                Description = "Description 2",
                DueDateUtc = DateTime.UtcNow.AddDays(2),
                Priority = TaskPriority.Low,
                Status = TaskStatus.Completed,
                OwnerUserId = owner.Id,
                AssignedUserId = owner.Id,
                ReminderSent = false,
                CreatedAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = DateTime.UtcNow,
                Owner = owner
            }
        };
        _context.Tasks.AddRange(tasks);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.ListTasksAsync(
            scope: null,
            ownerUserId: owner.Id,
            assignedUserId: null,
            statuses: new List<TaskStatus> { TaskStatus.Open },
            priorities: null,
            overdueOnly: null,
            reminderSent: null,
            search: null,
            sortBy: null,
            sortDir: null,
            page: 1,
            pageSize: 10);

        // Assert
        result.Items.Should().HaveCount(1);
        result.Items[0].Status.Should().Be(TaskStatus.Open);
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}

