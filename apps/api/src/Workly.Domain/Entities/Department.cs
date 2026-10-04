namespace Workly.Domain.Entities;

public class Department
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public Guid OrganizationId { get; set; }
    public required string Name { get; set; }
    public Guid? ManagerId { get; set; }

    public Organization Organization { get; set; } = null!;
    public OrganizationMember? Manager { get; set; }
    public ICollection<OrganizationMember> Members { get; set; } = [];
}
