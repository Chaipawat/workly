using Microsoft.EntityFrameworkCore;
using Workly.Domain.Entities;

namespace Workly.Infrastructure.Persistence;

public sealed class WorklyDbContext(DbContextOptions<WorklyDbContext> options)
    : DbContext(options)
{
    // Set only after the backend has verified the user's organization membership.
    public Guid? CurrentOrganizationId { get; set; }

    public DbSet<User> Users => Set<User>();
    public DbSet<Organization> Organizations => Set<Organization>();
    public DbSet<OrganizationMember> OrganizationMembers => Set<OrganizationMember>();
    public DbSet<Department> Departments => Set<Department>();
    public DbSet<Project> Projects => Set<Project>();
    public DbSet<ProjectMember> ProjectMembers => Set<ProjectMember>();
    public DbSet<WorkTask> Tasks => Set<WorkTask>();
    public DbSet<TaskComment> TaskComments => Set<TaskComment>();
    public DbSet<LeaveRequest> LeaveRequests => Set<LeaveRequest>();
    public DbSet<Announcement> Announcements => Set<Announcement>();
    public DbSet<Invitation> Invitations => Set<Invitation>();
    public DbSet<ActivityLog> ActivityLogs => Set<ActivityLog>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasPostgresExtension("citext");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(WorklyDbContext).Assembly);

        modelBuilder.Entity<Organization>()
            .HasQueryFilter(x => CurrentOrganizationId != null && x.Id == CurrentOrganizationId);
        AddOrganizationFilter<OrganizationMember>(modelBuilder);
        AddOrganizationFilter<Department>(modelBuilder);
        AddOrganizationFilter<Project>(modelBuilder);
        AddOrganizationFilter<ProjectMember>(modelBuilder);
        AddOrganizationFilter<WorkTask>(modelBuilder);
        AddOrganizationFilter<TaskComment>(modelBuilder);
        AddOrganizationFilter<LeaveRequest>(modelBuilder);
        AddOrganizationFilter<Announcement>(modelBuilder);
        AddOrganizationFilter<Invitation>(modelBuilder);
        AddOrganizationFilter<ActivityLog>(modelBuilder);
    }

    private void AddOrganizationFilter<TEntity>(ModelBuilder modelBuilder)
        where TEntity : class
    {
        modelBuilder.Entity<TEntity>().HasQueryFilter(entity =>
            CurrentOrganizationId != null
            && EF.Property<Guid>(entity, "OrganizationId") == CurrentOrganizationId);
    }
}
