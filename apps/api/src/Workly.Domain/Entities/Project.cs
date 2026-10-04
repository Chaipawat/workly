using Workly.Domain.Enums;

namespace Workly.Domain.Entities;

public class Project
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public Guid OrganizationId { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
    public ProjectStatus Status { get; set; } = ProjectStatus.Active;
    public DateOnly? StartDate { get; set; }
    public DateOnly? DueDate { get; set; }
    public Guid CreatedById { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public Organization Organization { get; set; } = null!;
    public OrganizationMember CreatedBy { get; set; } = null!;
    public ICollection<ProjectMember> Members { get; set; } = [];
    public ICollection<WorkTask> Tasks { get; set; } = [];
}
