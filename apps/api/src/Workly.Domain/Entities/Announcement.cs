namespace Workly.Domain.Entities;

public class Announcement
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public Guid OrganizationId { get; set; }
    public Guid AuthorId { get; set; }
    public required string Title { get; set; }
    public required string Body { get; set; }
    public bool Pinned { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public OrganizationMember Author { get; set; } = null!;
}
