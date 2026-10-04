using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Workly.Domain.Entities;
using Workly.Domain.Enums;

namespace Workly.Infrastructure.Persistence.Configurations;

internal sealed class WorkTaskConfiguration : IEntityTypeConfiguration<WorkTask>
{
    public void Configure(EntityTypeBuilder<WorkTask> entity)
    {
        entity.ToTable("tasks", table =>
        {
            table.HasCheckConstraint(
                "ck_tasks_status",
                "status IN ('todo', 'in_progress', 'done')");
            table.HasCheckConstraint(
                "ck_tasks_priority",
                "priority IN ('low', 'medium', 'high')");
            table.HasCheckConstraint("ck_tasks_position", "position >= 0");
        });
        entity.HasKey(x => x.Id);
        entity.HasAlternateKey(x => new { x.OrganizationId, x.Id });
        entity.Property(x => x.Title).HasMaxLength(240);
        entity.Property(x => x.Description).HasMaxLength(8000);
        entity.Property(x => x.Status).HasConversion(
            value => value == Domain.Enums.TaskStatus.InProgress
                ? "in_progress"
                : value.ToString().ToLowerInvariant(),
            value => value == "in_progress"
                ? Domain.Enums.TaskStatus.InProgress
                : Enum.Parse<Domain.Enums.TaskStatus>(value, true));
        entity.Property(x => x.Priority).HasConversion(
            value => value.ToString().ToLowerInvariant(),
            value => Enum.Parse<TaskPriority>(value, true));

        entity.HasOne(x => x.Project)
            .WithMany(x => x.Tasks)
            .HasForeignKey(x => new { x.OrganizationId, x.ProjectId })
            .HasPrincipalKey(x => new { x.OrganizationId, x.Id })
            .OnDelete(DeleteBehavior.Cascade);
        entity.HasOne(x => x.Assignee)
            .WithMany()
            .HasForeignKey(x => new { x.OrganizationId, x.AssigneeId })
            .HasPrincipalKey(x => new { x.OrganizationId, x.Id })
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasIndex(x => new { x.ProjectId, x.Status, x.Position });
        entity.HasIndex(x => new { x.OrganizationId, x.AssigneeId, x.Status });
        entity.HasIndex(x => new { x.OrganizationId, x.DueDate })
            .HasFilter("status <> 'done'");
    }
}

internal sealed class TaskCommentConfiguration : IEntityTypeConfiguration<TaskComment>
{
    public void Configure(EntityTypeBuilder<TaskComment> entity)
    {
        entity.ToTable("task_comments");
        entity.HasKey(x => x.Id);
        entity.Property(x => x.Body).HasMaxLength(8000);
        entity.HasOne(x => x.Task)
            .WithMany(x => x.Comments)
            .HasForeignKey(x => new { x.OrganizationId, x.TaskId })
            .HasPrincipalKey(x => new { x.OrganizationId, x.Id })
            .OnDelete(DeleteBehavior.Cascade);
        entity.HasOne(x => x.Author)
            .WithMany()
            .HasForeignKey(x => new { x.OrganizationId, x.AuthorId })
            .HasPrincipalKey(x => new { x.OrganizationId, x.Id })
            .OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class LeaveRequestConfiguration : IEntityTypeConfiguration<LeaveRequest>
{
    public void Configure(EntityTypeBuilder<LeaveRequest> entity)
    {
        entity.ToTable("leave_requests", table =>
        {
            table.HasCheckConstraint(
                "ck_leave_requests_type",
                "type IN ('annual', 'sick', 'personal', 'other')");
            table.HasCheckConstraint(
                "ck_leave_requests_status",
                "status IN ('pending', 'approved', 'rejected', 'cancelled')");
            table.HasCheckConstraint("ck_leave_requests_dates", "end_date >= start_date");
        });
        entity.HasKey(x => x.Id);
        entity.Property(x => x.Type).HasConversion(
            value => value.ToString().ToLowerInvariant(),
            value => Enum.Parse<LeaveType>(value, true));
        entity.Property(x => x.Status).HasConversion(
            value => value.ToString().ToLowerInvariant(),
            value => Enum.Parse<LeaveRequestStatus>(value, true));
        entity.Property(x => x.Reason).HasMaxLength(2000);
        entity.Property(x => x.DecisionNote).HasMaxLength(2000);
        entity.HasOne(x => x.Member)
            .WithMany()
            .HasForeignKey(x => new { x.OrganizationId, x.MemberId })
            .HasPrincipalKey(x => new { x.OrganizationId, x.Id })
            .OnDelete(DeleteBehavior.Restrict);
        entity.HasOne(x => x.DecidedBy)
            .WithMany()
            .HasForeignKey(x => new { x.OrganizationId, x.DecidedById })
            .HasPrincipalKey(x => new { x.OrganizationId, x.Id })
            .OnDelete(DeleteBehavior.Restrict);
        entity.HasIndex(x => new { x.OrganizationId, x.StartDate, x.EndDate })
            .HasFilter("status = 'approved'");
        entity.HasIndex(x => new { x.OrganizationId, x.Status });
    }
}
