using Microsoft.EntityFrameworkCore;
using TaskApp.Domain.Entities;

namespace TaskApp.Infrastructure.Data;

public class TaskDbContext : DbContext
{
    public TaskDbContext(DbContextOptions<TaskDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users { get; set; }
    public DbSet<Domain.Entities.Task> Tasks { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // User configuration
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.FullName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Email).IsRequired().HasMaxLength(150);
            entity.Property(e => e.Telephone).IsRequired().HasMaxLength(20);
            entity.Property(e => e.CreatedAtUtc).IsRequired().HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasIndex(e => e.Email).IsUnique().HasDatabaseName("UX_Users_Email");
        });

        // Task configuration
        modelBuilder.Entity<Domain.Entities.Task>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Title).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Description).IsRequired().HasMaxLength(250);
            entity.Property(e => e.DueDateUtc).IsRequired();
            entity.Property(e => e.Priority).IsRequired();
            entity.Property(e => e.Status).IsRequired();
            entity.Property(e => e.OwnerUserId).IsRequired();
            entity.Property(e => e.AssignedUserId).IsRequired(false);
            entity.Property(e => e.ReminderSent).IsRequired().HasDefaultValue(false);
            entity.Property(e => e.DueNotifiedAtUtc).IsRequired(false);
            entity.Property(e => e.CreatedAtUtc).IsRequired().HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.UpdatedAtUtc).IsRequired().HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.RowVersion).IsConcurrencyToken();

            // Foreign keys
            entity.HasOne(e => e.Owner)
                .WithMany()
                .HasForeignKey(e => e.OwnerUserId)
                .OnDelete(DeleteBehavior.NoAction);

            entity.HasOne(e => e.Assignee)
                .WithMany()
                .HasForeignKey(e => e.AssignedUserId)
                .OnDelete(DeleteBehavior.NoAction);

            // Indexes
            entity.HasIndex(e => e.DueDateUtc).HasDatabaseName("IX_Tasks_DueDateUtc");
            entity.HasIndex(e => e.AssignedUserId).HasDatabaseName("IX_Tasks_AssignedUserId");
            entity.HasIndex(e => e.OwnerUserId).HasDatabaseName("IX_Tasks_OwnerUserId");
            entity.HasIndex(e => e.Status).HasDatabaseName("IX_Tasks_Status");
            entity.HasIndex(e => new { e.ReminderSent, e.DueDateUtc }).HasDatabaseName("IX_Tasks_ReminderSent_DueDateUtc");
            entity.HasIndex(e => new { e.DueNotifiedAtUtc, e.DueDateUtc }).HasDatabaseName("IX_Tasks_DueNotifiedAtUtc_DueDateUtc");
        });
    }
}

