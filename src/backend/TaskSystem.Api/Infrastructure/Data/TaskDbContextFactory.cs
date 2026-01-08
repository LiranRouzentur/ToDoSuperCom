using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace TaskApp.Infrastructure.Data;

public class TaskDbContextFactory : IDesignTimeDbContextFactory<TaskDbContext>
{
    public TaskDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<TaskDbContext>();
        
        // Default connection string for design-time migrations
        // This will be overridden by actual connection string from configuration at runtime
        var connectionString = "Data Source=app.db";

        optionsBuilder.UseSqlite(connectionString);

        return new TaskDbContext(optionsBuilder.Options);
    }
}

