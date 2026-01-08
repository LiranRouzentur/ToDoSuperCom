using TaskApp.Application.DTOs;

namespace TaskApp.Application.Services;

public interface IUserService
{
    Task<UserResponse> CreateUserAsync(UserCreateRequest request, CancellationToken cancellationToken = default);
    Task<UserResponse?> GetUserByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<UserResponse?> GetUserByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<UserResponse> UpsertUserByEmailAsync(UserCreateRequest request, CancellationToken cancellationToken = default);
    Task<PagedResponse<UserResponse>> ListUsersAsync(string? search, int? page, int? pageSize, CancellationToken cancellationToken = default);
}

