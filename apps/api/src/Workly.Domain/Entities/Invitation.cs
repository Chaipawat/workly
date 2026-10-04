using Workly.Domain.Enums;

namespace Workly.Domain.Entities;

public class Invitation
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public Guid OrganizationId { get; set; }
    public required string Email { get; set; }
    public OrganizationRole Role { get; set; } = OrganizationRole.Employee;
    public Guid? DepartmentId { get; set; }
    public required string TokenHash { get; set; }
    public Guid InvitedById { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset? AcceptedAt { get; set; }
    public DateTimeOffset? RevokedAt { get; set; }

    public Department? Department { get; set; }
    public OrganizationMember InvitedBy { get; set; } = null!;
}
