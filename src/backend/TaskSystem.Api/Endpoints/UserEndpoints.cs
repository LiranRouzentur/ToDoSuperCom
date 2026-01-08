using Microsoft.AspNetCore.Mvc;
using TaskApp.Application.DTOs;
using TaskApp.Application.Services;

namespace TaskApp.Api.Endpoints;

public static class UserEndpoints
{
    public static void MapUsersEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/users")
            .WithTags("Users");

        group.MapPost("/", CreateUser)
            .WithName("CreateUser")
            .Produces<UserResponse>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status400BadRequest);

        group.MapGet("/{id:guid}", GetUserById)
            .WithName("GetUserById")
            .Produces<UserResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapGet("/email/{email}", GetUserByEmail)
            .WithName("GetUserByEmail")
            .Produces<UserResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapGet("/", ListUsers)
            .WithName("ListUsers")
            .Produces<PagedResponse<UserResponse>>(StatusCodes.Status200OK);
    }

    private static async Task<IResult> CreateUser(
        [FromBody] UserCreateRequest request,
        [FromServices] IUserService service,
        CancellationToken cancellationToken)
    {
        var user = await service.CreateUserAsync(request, cancellationToken);
        return Results.Created($"/api/users/{user.Id}", user);
    }

    private static async Task<IResult> GetUserById(
        [FromRoute] Guid id,
        [FromServices] IUserService service,
        CancellationToken cancellationToken)
    {
        var user = await service.GetUserByIdAsync(id, cancellationToken);
        return user != null ? Results.Ok(user) : Results.NotFound();
    }

    private static async Task<IResult> GetUserByEmail(
        [FromRoute] string email,
        [FromServices] IUserService service,
        CancellationToken cancellationToken)
    {
        var user = await service.GetUserByEmailAsync(email, cancellationToken);
        return user != null ? Results.Ok(user) : Results.NotFound();
    }

    private static async Task<IResult> ListUsers(
        [FromServices] IUserService service,
        [FromQuery] string? search,
        [FromQuery] int? page,
        [FromQuery] int? pageSize,
        CancellationToken cancellationToken)
    {
        var result = await service.ListUsersAsync(search, page, pageSize, cancellationToken);
        return Results.Ok(result);
    }
}
