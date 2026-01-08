using Microsoft.EntityFrameworkCore;
using TaskApp.Infrastructure.Data;

namespace TaskSystem.Api.Tests.TestHelpers;

public static class MockDbContextFactory
{
    public static TaskDbContext CreateInMemoryContext(string? databaseName = null)
    {
        var options = new DbContextOptionsBuilder<TaskDbContext>()
            .UseInMemoryDatabase(databaseName ?? Guid.NewGuid().ToString())
            .Options;

        return new TaskDbContext(options);
    }
}

