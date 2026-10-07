using Workly.Domain.Enums;

namespace Workly.Domain.Entities;

public class OrganizationMember
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public Guid OrganizationId { get; set; }
    public Guid UserId { get; set; }
    public OrganizationRole Role { get; set; } = OrganizationRole.Employee;
    public MembershipStatus Status { get; set; } = MembershipStatus.Active;
    public required string JobTitle { get; set; }
    public Guid? DepartmentId { get; set; }
    public DateTimeOffset JoinedAt { get; set; } = DateTimeOffset.UtcNow;

    public Organization Organization { get; set; } = null!;
    public User User { get; set; } = null!;
    public Department? Department { get; set; }
}
