using Microsoft.EntityFrameworkCore;
using TaskApp.Domain.Entities;
using TaskApp.Domain.Enums;

namespace TaskApp.Infrastructure.Data;

public static class DbSeeder
{
    public static async System.Threading.Tasks.Task SeedAsync(TaskDbContext context)
    {
        // Check if we should seed (e.g. if we already have users we might still want to re-seed if that's the requirement)
        // User said: "I dont mind us having hard coded data that would reapper each time we would do the builed"
        // To ensure consistency, we'll clear existing data.
        if (context.Database.ProviderName == "Microsoft.EntityFrameworkCore.InMemory")
        {
            context.Tasks.RemoveRange(context.Tasks);
            context.Users.RemoveRange(context.Users);
        }
        else
        {
            await context.Tasks.ExecuteDeleteAsync();
            await context.Users.ExecuteDeleteAsync();
        }

        var currentUser = new User
        {
            Id = Guid.NewGuid(),
            FullName = "Current User",
            Email = "liran@example.com",
            Telephone = "050-1234567",
            CreatedAtUtc = DateTime.UtcNow
        };

        var users = new List<User>
        {
            currentUser,
            new User { Id = Guid.NewGuid(), FullName = "Alice Smith", Email = "alice@example.com", Telephone = "050-1111111", CreatedAtUtc = DateTime.UtcNow },
            new User { Id = Guid.NewGuid(), FullName = "Bob Jones", Email = "bob@example.com", Telephone = "050-2222222", CreatedAtUtc = DateTime.UtcNow },
            new User { Id = Guid.NewGuid(), FullName = "Charlie Brown", Email = "charlie@example.com", Telephone = "050-3333333", CreatedAtUtc = DateTime.UtcNow },
            new User { Id = Guid.NewGuid(), FullName = "Diana Prince", Email = "diana@example.com", Telephone = "050-4444444", CreatedAtUtc = DateTime.UtcNow }
        };

        await context.Users.AddRangeAsync(users);
        await context.SaveChangesAsync();

        var tasks = new List<Domain.Entities.Task>();
        var random = new Random();
        var userIds = users.Select(u => u.Id).ToList();

        // DEMO DATA: STRICTLY 25 Tasks total (5 Overdue, 20 Future)
        // DEMO DATA: STRICTLY 25 Tasks total (5 Overdue, 20 Others)
        
        // 1. Create list of 20 statuses for the non-overdue tasks
        // Requirement: Min 3 tasks in each status (Draft, Open, InProgress, Completed, Cancelled)
        // 5 statuses * 3 = 15 tasks fixed. 5 remaining tasks random.
        var targetStatuses = new List<TaskApp.Domain.Enums.TaskStatus>();
        var statusOptions = new[] 
        { 
            TaskApp.Domain.Enums.TaskStatus.Draft,
            TaskApp.Domain.Enums.TaskStatus.Open,
            TaskApp.Domain.Enums.TaskStatus.InProgress,
            TaskApp.Domain.Enums.TaskStatus.Completed,
            TaskApp.Domain.Enums.TaskStatus.Cancelled
        };

        // Add 3 of each
        foreach (var status in statusOptions)
        {
            for (int k = 0; k < 3; k++) targetStatuses.Add(status);
        }

        // Fill remaining 5 randomly
        for (int k = 0; k < 5; k++)
        {
            targetStatuses.Add(statusOptions[random.Next(statusOptions.Length)]);
        }

        // Shuffle the statuses so they aren't sequential
        targetStatuses = targetStatuses.OrderBy(_ => random.Next()).ToList();

        for (int i = 1; i <= 25; i++)
        {
            // First 5 are Overdue (fixed)
            // Next 20 are from our shuffled list (i-6 index)
            bool isOverdue = i <= 5;
            
            TaskApp.Domain.Enums.TaskStatus status;
            DateTime dueDate;
            
            if (isOverdue)
            {
                status = TaskApp.Domain.Enums.TaskStatus.Overdue;
                dueDate = DateTime.UtcNow.AddDays(-1);
            }
            else
            {
                // Take from our prepared distribution list
                status = targetStatuses[i - 6];
                
                // Set due date based on status logic to look realistic
                if (status == TaskApp.Domain.Enums.TaskStatus.Completed || status == TaskApp.Domain.Enums.TaskStatus.Cancelled)
                {
                    dueDate = DateTime.UtcNow.AddDays(random.Next(-5, -1)); // Finished in past
                }
                else
                {
                    dueDate = DateTime.UtcNow.AddDays(random.Next(1, 10)); // Due in future
                }
            }

            var title = isOverdue ? $"Overdue Task {i}" : $"{status} Task {i}";
            var priority = isOverdue ? TaskPriority.High : (TaskPriority)random.Next(0, 3);
            
            // Ensure Current User has tasks for proper tab filtering:
            // Tasks 1-10: Owned by Current User, assigned to OTHER users
            // Tasks 11-20: Assigned to Current User, owned by OTHER users
            // Tasks 21-25: Other users only (for variety)
            Guid ownerId;
            Guid assigneeId;
            
            if (i <= 10)
            {
                // First 10 tasks: Current User is the owner, assignee is someone else
                ownerId = currentUser.Id;
                // Pick a random user that is NOT the current user (indices 1-4)
                assigneeId = userIds[random.Next(1, userIds.Count)];
            }
            else if (i <= 20)
            {
                // Next 10 tasks: Current User is the assignee, owner is someone else
                // Pick a random user that is NOT the current user (indices 1-4)
                ownerId = userIds[random.Next(1, userIds.Count)];
                assigneeId = currentUser.Id;
            }
            else
            {
                // Last 5 tasks: Other users only (for variety)
                ownerId = userIds[random.Next(1, userIds.Count)]; // Skip currentUser (index 0)
                assigneeId = userIds[random.Next(1, userIds.Count)];
            }

            tasks.Add(new Domain.Entities.Task
            {
                Id = Guid.NewGuid(),
                Title = title,
                Description = $"Auto-generated task #{i} for testing. Status: {status}",
                DueDateUtc = dueDate,
                Priority = priority,
                Status = status,
                OwnerUserId = ownerId,
                AssignedUserId = assigneeId,
                CreatedAtUtc = DateTime.UtcNow.AddDays(-5),
                UpdatedAtUtc = DateTime.UtcNow.AddDays(-1),
                RowVersion = Guid.NewGuid().ToByteArray()
            });
        }

        await context.Tasks.AddRangeAsync(tasks);
        try
        {
            await context.SaveChangesAsync();
        }
        catch (DbUpdateException ex)
        {
            var message = $"Error seeding database: {ex.Message}. Inner: {ex.InnerException?.Message}";
            throw new Exception(message, ex);
        }
    }
}
