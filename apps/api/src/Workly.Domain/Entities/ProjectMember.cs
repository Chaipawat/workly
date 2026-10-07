namespace Workly.Domain.Entities;

public class ProjectMember
{
    public Guid OrganizationId { get; set; }
    public Guid ProjectId { get; set; }
    public Guid MemberId { get; set; }
    public DateTimeOffset AddedAt { get; set; } = DateTimeOffset.UtcNow;

    public Project Project { get; set; } = null!;
    public OrganizationMember Member { get; set; } = null!;
}
