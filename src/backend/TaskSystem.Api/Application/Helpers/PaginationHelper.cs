namespace TaskApp.Application.Helpers;

public static class PaginationHelper
{
    public const int DefaultPageSize = 20;
    public const int MaxPageSize = 100;

    public static (int page, int pageSize) NormalizePagination(int? page, int? pageSize)
    {
        var normalizedPage = Math.Max(1, page ?? 1);
        var normalizedPageSize = Math.Min(MaxPageSize, Math.Max(1, pageSize ?? DefaultPageSize));

        return (normalizedPage, normalizedPageSize);
    }

    public static int CalculateTotalPages(int totalItems, int pageSize)
    {
        if (pageSize <= 0)
            return 0;

        return (int)Math.Ceiling(totalItems / (double)pageSize);
    }
}

