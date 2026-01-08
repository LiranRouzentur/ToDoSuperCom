using Microsoft.EntityFrameworkCore;
using TaskApp.Application.DTOs;
using TaskApp.Application.Helpers;
using TaskApp.Domain.Entities;
using TaskApp.Infrastructure.Data;

namespace TaskApp.Application.Services;

public class UserService : IUserService
{
    private readonly TaskDbContext _context;

    public UserService(TaskDbContext context)
    {
        _context = context;
    }

    public async Task<UserResponse> CreateUserAsync(UserCreateRequest request, CancellationToken cancellationToken = default)
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            FullName = request.FullName.Trim(),
            Email = request.Email.Trim().ToLowerInvariant(),
            Telephone = PhoneNumberHelper.NormalizePhone(request.Telephone),
            CreatedAtUtc = DateTime.UtcNow
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync(cancellationToken);

        return MapToResponse(user);
    }

    public async Task<UserResponse?> GetUserByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var user = await _context.Users.FindAsync(new object[] { id }, cancellationToken);
        return user == null ? null : MapToResponse(user);
    }

    public async Task<UserResponse?> GetUserByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        var normalizedEmail = email.Trim().ToLowerInvariant();
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Email == normalizedEmail, cancellationToken);

        return user == null ? null : MapToResponse(user);
    }

    public async Task<UserResponse> UpsertUserByEmailAsync(UserCreateRequest request, CancellationToken cancellationToken = default)
    {
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        var existingUser = await _context.Users
            .FirstOrDefaultAsync(u => u.Email == normalizedEmail, cancellationToken);

        if (existingUser != null)
        {
            // Update existing user
            existingUser.FullName = request.FullName.Trim();
            existingUser.Telephone = PhoneNumberHelper.NormalizePhone(request.Telephone);
            await _context.SaveChangesAsync(cancellationToken);
            return MapToResponse(existingUser);
        }

        // Create new user
        return await CreateUserAsync(request, cancellationToken);
    }

    public async Task<PagedResponse<UserResponse>> ListUsersAsync(string? search, int? page, int? pageSize, CancellationToken cancellationToken = default)
    {
        var (normalizedPage, normalizedPageSize) = PaginationHelper.NormalizePagination(page, pageSize);

        var query = _context.Users.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var searchTerm = search.Trim().ToLowerInvariant();
            query = query.Where(u =>
                u.FullName.ToLower().Contains(searchTerm) ||
                u.Email.ToLower().Contains(searchTerm) ||
                u.Telephone.Contains(searchTerm));
        }

        var totalItems = await query.CountAsync(cancellationToken);
        var totalPages = PaginationHelper.CalculateTotalPages(totalItems, normalizedPageSize);

        var users = await query
            .OrderBy(u => u.FullName)
            .Skip((normalizedPage - 1) * normalizedPageSize)
            .Take(normalizedPageSize)
            .ToListAsync(cancellationToken);

        return new PagedResponse<UserResponse>
        {
            Items = users.Select(MapToResponse).ToList(),
            Page = normalizedPage,
            PageSize = normalizedPageSize,
            TotalItems = totalItems,
            TotalPages = totalPages
        };
    }

    private static UserResponse MapToResponse(User user)
    {
        return new UserResponse
        {
            Id = user.Id,
            FullName = user.FullName,
            Email = user.Email,
            Telephone = user.Telephone,
            CreatedAtUtc = user.CreatedAtUtc
        };
    }
}

