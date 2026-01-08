namespace TaskApp.Application.Constants;

/// <summary>
/// Centralized constants for task-related configuration and limits.
/// </summary>
public static class TaskConstants
{
    // Validation limits
    public const int TitleMaxLength = 50;
    public const int DescriptionMaxLength = 250;
    public const int FullNameMaxLength = 100;
    public const int EmailMaxLength = 150;
    public const int TelephoneMaxLength = 20;

    // Default pagination
    public const int DefaultPageSize = 20;
    public const int MaxPageSize = 100;
    public const int MinPage = 1;
}

