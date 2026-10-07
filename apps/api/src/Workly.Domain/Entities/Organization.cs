namespace Workly.Domain.Entities;

public class Organization
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public required string Name { get; set; }
    public required string Slug { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public ICollection<OrganizationMember> Members { get; set; } = [];
    public ICollection<Department> Departments { get; set; } = [];
    public ICollection<Project> Projects { get; set; } = [];
}
