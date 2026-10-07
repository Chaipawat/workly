using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Workly.Domain.Entities;
using Workly.Domain.Enums;

namespace Workly.Infrastructure.Persistence.Configurations;

internal sealed class AnnouncementConfiguration : IEntityTypeConfiguration<Announcement>
{
    public void Configure(EntityTypeBuilder<Announcement> entity)
    {
        entity.ToTable("announcements");
        entity.HasKey(x => x.Id);
        entity.Property(x => x.Title).HasMaxLength(240);
        entity.Property(x => x.Body).HasMaxLength(12000);
        entity.HasOne(x => x.Author)
            .WithMany()
            .HasForeignKey(x => new { x.OrganizationId, x.AuthorId })
            .HasPrincipalKey(x => new { x.OrganizationId, x.Id })
            .OnDelete(DeleteBehavior.Restrict);
        entity.HasIndex(x => new { x.OrganizationId, x.Pinned, x.CreatedAt })
            .IsDescending(false, true, true);
    }
}

internal sealed class InvitationConfiguration : IEntityTypeConfiguration<Invitation>
{
    public void Configure(EntityTypeBuilder<Invitation> entity)
    {
        entity.ToTable("invitations", table =>
            table.HasCheckConstraint(
                "ck_invitations_role",
                "role IN ('employee', 'manager', 'hr', 'admin')"));
        entity.HasKey(x => x.Id);
        entity.Property(x => x.Email).HasColumnType("citext").HasMaxLength(320);
        entity.Property(x => x.TokenHash).HasMaxLength(512);
        entity.Property(x => x.Role).HasConversion(
            value => value.ToString().ToLowerInvariant(),
            value => Enum.Parse<OrganizationRole>(value, true));
        entity.HasOne(x => x.Department)
            .WithMany()
            .HasForeignKey(x => new { x.OrganizationId, x.DepartmentId })
            .HasPrincipalKey(x => new { x.OrganizationId, x.Id })
            .OnDelete(DeleteBehavior.Restrict);
        entity.HasOne(x => x.InvitedBy)
            .WithMany()
            .HasForeignKey(x => new { x.OrganizationId, x.InvitedById })
            .HasPrincipalKey(x => new { x.OrganizationId, x.Id })
            .OnDelete(DeleteBehavior.Restrict);
        entity.HasIndex(x => x.TokenHash).IsUnique();
        entity.HasIndex(x => new { x.OrganizationId, x.Email })
            .IsUnique()
            .HasFilter("accepted_at IS NULL AND revoked_at IS NULL");
    }
}

internal sealed class ActivityLogConfiguration : IEntityTypeConfiguration<ActivityLog>
{
    public void Configure(EntityTypeBuilder<ActivityLog> entity)
    {
        entity.ToTable("activity_logs");
        entity.HasKey(x => x.Id);
        entity.Property(x => x.Action).HasMaxLength(160);
        entity.Property(x => x.EntityType).HasMaxLength(120);
        entity.Property(x => x.Metadata).HasColumnType("jsonb");
        entity.HasOne<Organization>()
            .WithMany()
            .HasForeignKey(x => x.OrganizationId)
            .OnDelete(DeleteBehavior.Cascade);
        entity.HasOne(x => x.Actor)
            .WithMany()
            .HasForeignKey(x => new { x.OrganizationId, x.ActorId })
            .HasPrincipalKey(x => new { x.OrganizationId, x.Id })
            .OnDelete(DeleteBehavior.Restrict);
        entity.HasIndex(x => new { x.OrganizationId, x.CreatedAt })
            .IsDescending(false, true);
    }
}

internal sealed class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> entity)
    {
        entity.ToTable("refresh_tokens");
        entity.HasKey(x => x.Id);
        entity.Property(x => x.TokenHash).HasMaxLength(512);
        entity.HasIndex(x => x.TokenHash).IsUnique();
        entity.HasIndex(x => x.UserId);
        entity.HasOne(x => x.User)
            .WithMany(x => x.RefreshTokens)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);
        entity.HasOne(x => x.ReplacedBy)
            .WithMany()
            .HasForeignKey(x => x.ReplacedById)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
