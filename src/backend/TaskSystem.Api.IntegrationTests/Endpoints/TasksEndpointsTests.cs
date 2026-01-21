using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using TaskApp.Application.DTOs;
using TaskApp.Domain.Enums;
using TaskSystem.Api.IntegrationTests.Infrastructure;
using TaskStatus = TaskApp.Domain.Enums.TaskStatus;

namespace TaskSystem.Api.IntegrationTests.Endpoints;

public class TasksEndpointsTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly System.Text.Json.JsonSerializerOptions _jsonOptions;

    public TasksEndpointsTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
        _jsonOptions = new System.Text.Json.JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
        };
    }

    [Fact]
    public async Task POST_tasks_WithValidRequest_Returns201Created()
    {
        // Arrange
        var request = new TaskCreateRequest
        {
            Title = "Integration Test Task",
            Description = "This is a test task for integration testing",
            DueDateUtc = DateTime.UtcNow.AddDays(1),
            Priority = TaskPriority.Medium,
            Owner = new UserRefDto
            {
                FullName = "Test User",
                Email = "test@example.com",
                Telephone = "+972501234567"
            }
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/tasks", request, _jsonOptions);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var task = await response.Content.ReadFromJsonAsync<TaskResponse>(_jsonOptions);
        task.Should().NotBeNull();
        task!.Title.Should().Be("Integration Test Task");
    }

    [Fact]
    public async Task GET_tasks_WithFilters_ReturnsFilteredResults()
    {
        // Arrange - Create a task first
        var createRequest = new TaskCreateRequest
        {
            Title = "Filter Test Task",
            Description = "Description",
            DueDateUtc = DateTime.UtcNow.AddDays(1),
            Priority = TaskPriority.High,
            Owner = new UserRefDto
            {
                FullName = "Test User",
                Email = "filter@example.com",
                Telephone = "+972501234567"
            }
        };
        var createResponse = await _client.PostAsJsonAsync("/api/v1/tasks", createRequest, _jsonOptions);
        var createdTask = await createResponse.Content.ReadFromJsonAsync<TaskResponse>(_jsonOptions);

        // Act
        var response = await _client.GetAsync($"/api/v1/tasks?status=Open&priority=High");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<PagedResponse<TaskResponse>>(_jsonOptions);
        result.Should().NotBeNull();
        result!.Items.Should().Contain(t => t.Id == createdTask!.Id);
    }

    [Fact]
    public async Task GET_tasks_id_WithExistingId_ReturnsTask()
    {
        // Arrange - Create a task first
        var createRequest = new TaskCreateRequest
        {
            Title = "Get Test Task",
            Description = "Description",
            DueDateUtc = DateTime.UtcNow.AddDays(1),
            Priority = TaskPriority.Medium,
            Owner = new UserRefDto
            {
                FullName = "Test User",
                Email = "get@example.com",
                Telephone = "+972501234567"
            }
        };
        var createResponse = await _client.PostAsJsonAsync("/api/v1/tasks", createRequest, _jsonOptions);
        var createdTask = await createResponse.Content.ReadFromJsonAsync<TaskResponse>(_jsonOptions);

        // Act
        var response = await _client.GetAsync($"/api/v1/tasks/{createdTask!.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var task = await response.Content.ReadFromJsonAsync<TaskResponse>(_jsonOptions);
        task.Should().NotBeNull();
        task!.Id.Should().Be(createdTask.Id);
    }

    [Fact]
    public async Task GET_tasks_id_WithNonExistentId_Returns404()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act
        var response = await _client.GetAsync($"/api/v1/tasks/{nonExistentId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task PUT_tasks_id_WithoutIfMatchHeader_Returns400()
    {
        // Arrange - Create a task first
        var createRequest = new TaskCreateRequest
        {
            Title = "Update Test Task",
            Description = "Description",
            DueDateUtc = DateTime.UtcNow.AddDays(1),
            Priority = TaskPriority.Medium,
            Owner = new UserRefDto
            {
                FullName = "Test User",
                Email = "update@example.com",
                Telephone = "+972501234567"
            }
        };
        var createResponse = await _client.PostAsJsonAsync("/api/v1/tasks", createRequest, _jsonOptions);
        var createdTask = await createResponse.Content.ReadFromJsonAsync<TaskResponse>(_jsonOptions);

        var updateRequest = new TaskUpdateRequest
        {
            Title = "Updated Title",
            Description = "Updated Description",
            DueDateUtc = DateTime.UtcNow.AddDays(2),
            Priority = TaskPriority.High,
            Status = TaskStatus.InProgress
        };

        // Act - Don't include If-Match header
        var response = await _client.PutAsJsonAsync($"/api/v1/tasks/{createdTask!.Id}", updateRequest, _jsonOptions);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task DELETE_tasks_id_WithExistingTask_Returns204()
    {
        // Arrange - Create a task first
        var createRequest = new TaskCreateRequest
        {
            Title = "Delete Test Task",
            Description = "Description",
            DueDateUtc = DateTime.UtcNow.AddDays(1),
            Priority = TaskPriority.Medium,
            Owner = new UserRefDto
            {
                FullName = "Test User",
                Email = "delete@example.com",
                Telephone = "+972501234567"
            }
        };
        var createResponse = await _client.PostAsJsonAsync("/api/v1/tasks", createRequest, _jsonOptions);
        var createdTask = await createResponse.Content.ReadFromJsonAsync<TaskResponse>(_jsonOptions);

        // Act
        var response = await _client.DeleteAsync($"/api/v1/tasks/{createdTask!.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }
}

