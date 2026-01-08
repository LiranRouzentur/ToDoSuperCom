namespace TaskApp.Application.DTOs;

public class UserResponse
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Telephone { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
}

