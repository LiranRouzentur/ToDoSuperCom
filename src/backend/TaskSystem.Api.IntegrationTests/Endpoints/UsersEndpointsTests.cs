using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using TaskApp.Application.DTOs;
using TaskSystem.Api.IntegrationTests.Infrastructure;

namespace TaskSystem.Api.IntegrationTests.Endpoints;

public class UsersEndpointsTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public UsersEndpointsTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task POST_users_WithValidRequest_Returns201Created()
    {
        // Arrange
        var request = new UserCreateRequest
        {
            FullName = "Integration Test User",
            Email = "integration@example.com",
            Telephone = "+972501234567"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/users", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var user = await response.Content.ReadFromJsonAsync<UserResponse>();
        user.Should().NotBeNull();
        user!.FullName.Should().Be("Integration Test User");
        user.Email.Should().Be("integration@example.com");
    }

    [Fact]
    public async Task GET_users_WithSearch_ReturnsFilteredResults()
    {
        // Arrange - Create a user first
        var createRequest = new UserCreateRequest
        {
            FullName = "Search Test User",
            Email = "search@example.com",
            Telephone = "+972501234567"
        };
        await _client.PostAsJsonAsync("/api/v1/users", createRequest);

        // Act
        var response = await _client.GetAsync("/api/v1/users?search=Search");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<PagedResponse<UserResponse>>();
        result.Should().NotBeNull();
        result!.Items.Should().Contain(u => u.FullName.Contains("Search"));
    }

    [Fact]
    public async Task GET_users_id_WithExistingId_ReturnsUser()
    {
        // Arrange - Create a user first
        var createRequest = new UserCreateRequest
        {
            FullName = "Get Test User",
            Email = "getuser@example.com",
            Telephone = "+972501234567"
        };
        var createResponse = await _client.PostAsJsonAsync("/api/v1/users", createRequest);
        var createdUser = await createResponse.Content.ReadFromJsonAsync<UserResponse>();

        // Act
        var response = await _client.GetAsync($"/api/v1/users/{createdUser!.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var user = await response.Content.ReadFromJsonAsync<UserResponse>();
        user.Should().NotBeNull();
        user!.Id.Should().Be(createdUser.Id);
    }
}

