namespace Workly.Domain.Entities;

public class TaskComment
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public Guid OrganizationId { get; set; }
    public Guid TaskId { get; set; }
    public Guid AuthorId { get; set; }
    public required string Body { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public WorkTask Task { get; set; } = null!;
    public OrganizationMember Author { get; set; } = null!;
}
