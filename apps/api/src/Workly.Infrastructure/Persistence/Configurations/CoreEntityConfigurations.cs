using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Workly.Domain.Entities;
using Workly.Domain.Enums;

namespace Workly.Infrastructure.Persistence.Configurations;

internal sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> entity)
    {
        entity.ToTable("users");
        entity.HasKey(x => x.Id);
        entity.Property(x => x.Email).HasColumnType("citext").HasMaxLength(320);
        entity.Property(x => x.PasswordHash).HasMaxLength(512);
        entity.Property(x => x.DisplayName).HasMaxLength(120);
        entity.HasIndex(x => x.Email).IsUnique();
    }
}

internal sealed class OrganizationConfiguration : IEntityTypeConfiguration<Organization>
{
    public void Configure(EntityTypeBuilder<Organization> entity)
    {
        entity.ToTable("organizations");
        entity.HasKey(x => x.Id);
        entity.Property(x => x.Name).HasMaxLength(160);
        entity.Property(x => x.Slug).HasMaxLength(80);
        entity.HasIndex(x => x.Slug).IsUnique();
    }
}

internal sealed class OrganizationMemberConfiguration
    : IEntityTypeConfiguration<OrganizationMember>
{
    public void Configure(EntityTypeBuilder<OrganizationMember> entity)
    {
        entity.ToTable("organization_members", table =>
        {
            table.HasCheckConstraint(
                "ck_organization_members_role",
                "role IN ('employee', 'manager', 'hr', 'admin', 'owner')");
            table.HasCheckConstraint(
                "ck_organization_members_status",
                "status IN ('active', 'removed')");
        });
        entity.HasKey(x => x.Id);
        entity.HasAlternateKey(x => new { x.OrganizationId, x.Id });
        entity.Property(x => x.Role).HasConversion(
            value => value.ToString().ToLowerInvariant(),
            value => Enum.Parse<OrganizationRole>(value, true));
        entity.Property(x => x.Status).HasConversion(
            value => value.ToString().ToLowerInvariant(),
            value => Enum.Parse<MembershipStatus>(value, true));
        entity.Property(x => x.JobTitle).HasMaxLength(120);

        entity.HasOne(x => x.Organization)
            .WithMany(x => x.Members)
            .HasForeignKey(x => x.OrganizationId)
            .OnDelete(DeleteBehavior.Cascade);
        entity.HasOne(x => x.User)
            .WithMany(x => x.Memberships)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Restrict);
        entity.HasOne(x => x.Department)
            .WithMany(x => x.Members)
            .HasForeignKey(x => new { x.OrganizationId, x.DepartmentId })
            .HasPrincipalKey(x => new { x.OrganizationId, x.Id })
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasIndex(x => new { x.OrganizationId, x.UserId }).IsUnique();
        entity.HasIndex(x => x.OrganizationId)
            .IsUnique()
            .HasFilter("role = 'owner' AND status = 'active'");
        entity.HasIndex(x => x.UserId);
        entity.HasIndex(x => new { x.OrganizationId, x.DepartmentId });
    }
}

internal sealed class DepartmentConfiguration : IEntityTypeConfiguration<Department>
{
    public void Configure(EntityTypeBuilder<Department> entity)
    {
        entity.ToTable("departments");
        entity.HasKey(x => x.Id);
        entity.HasAlternateKey(x => new { x.OrganizationId, x.Id });
        entity.Property(x => x.Name).HasColumnType("citext").HasMaxLength(120);
        entity.HasIndex(x => new { x.OrganizationId, x.Name }).IsUnique();

        entity.HasOne(x => x.Organization)
            .WithMany(x => x.Departments)
            .HasForeignKey(x => x.OrganizationId)
            .OnDelete(DeleteBehavior.Cascade);
        entity.HasOne(x => x.Manager)
            .WithMany()
            .HasForeignKey(x => new { x.OrganizationId, x.ManagerId })
            .HasPrincipalKey(x => new { x.OrganizationId, x.Id })
            .OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class ProjectConfiguration : IEntityTypeConfiguration<Project>
{
    public void Configure(EntityTypeBuilder<Project> entity)
    {
        entity.ToTable("projects", table =>
        {
            table.HasCheckConstraint(
                "ck_projects_status",
                "status IN ('active', 'on_hold', 'completed', 'archived')");
            table.HasCheckConstraint(
                "ck_projects_dates",
                "due_date IS NULL OR start_date IS NULL OR due_date >= start_date");
        });
        entity.HasKey(x => x.Id);
        entity.HasAlternateKey(x => new { x.OrganizationId, x.Id });
        entity.Property(x => x.Name).HasMaxLength(160);
        entity.Property(x => x.Description).HasMaxLength(4000);
        entity.Property(x => x.Status).HasConversion(
            value => value == ProjectStatus.OnHold
                ? "on_hold"
                : value.ToString().ToLowerInvariant(),
            value => value == "on_hold"
                ? ProjectStatus.OnHold
                : Enum.Parse<ProjectStatus>(value, true));

        entity.HasOne(x => x.Organization)
            .WithMany(x => x.Projects)
            .HasForeignKey(x => x.OrganizationId)
            .OnDelete(DeleteBehavior.Cascade);
        entity.HasOne(x => x.CreatedBy)
            .WithMany()
            .HasForeignKey(x => new { x.OrganizationId, x.CreatedById })
            .HasPrincipalKey(x => new { x.OrganizationId, x.Id })
            .OnDelete(DeleteBehavior.Restrict);
        entity.HasIndex(x => new { x.OrganizationId, x.Status });
    }
}

internal sealed class ProjectMemberConfiguration : IEntityTypeConfiguration<ProjectMember>
{
    public void Configure(EntityTypeBuilder<ProjectMember> entity)
    {
        entity.ToTable("project_members");
        entity.HasKey(x => new { x.ProjectId, x.MemberId });
        entity.HasOne(x => x.Project)
            .WithMany(x => x.Members)
            .HasForeignKey(x => new { x.OrganizationId, x.ProjectId })
            .HasPrincipalKey(x => new { x.OrganizationId, x.Id })
            .OnDelete(DeleteBehavior.Cascade);
        entity.HasOne(x => x.Member)
            .WithMany()
            .HasForeignKey(x => new { x.OrganizationId, x.MemberId })
            .HasPrincipalKey(x => new { x.OrganizationId, x.Id })
            .OnDelete(DeleteBehavior.Restrict);
    }
}
