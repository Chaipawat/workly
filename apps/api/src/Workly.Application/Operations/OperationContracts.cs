using System.ComponentModel.DataAnnotations;

namespace Workly.Application.Operations;

public sealed record MemberResponse(Guid Id, string Name, string Email, string JobTitle, string Role,
    string Status, Guid? DepartmentId, string? DepartmentName);
public sealed class AddMemberRequest
{
    [Required, EmailAddress, StringLength(320)] public required string Email { get; init; }
    [Required, StringLength(120)] public required string JobTitle { get; init; }
    public Guid? DepartmentId { get; init; }
    [Required] public string Role { get; init; } = "employee";
}
public sealed class UpdateMemberRequest
{
    [Required, StringLength(120)] public required string JobTitle { get; init; }
    public Guid? DepartmentId { get; init; }
    [Required] public required string Role { get; init; }
    [Required] public required string Status { get; init; }
}

public sealed record DepartmentResponse(Guid Id, string Name, Guid? ManagerId, string? ManagerName, int MemberCount);
public sealed class SaveDepartmentRequest
{
    [Required, StringLength(120, MinimumLength = 2)] public required string Name { get; init; }
    public Guid? ManagerId { get; init; }
}

public sealed record ProjectResponse(Guid Id, string Name, string? Description, string Status,
    DateOnly? StartDate, DateOnly? DueDate, int CompletedTasks, int TotalTasks);
public sealed class SaveProjectRequest
{
    [Required, StringLength(160, MinimumLength = 2)] public required string Name { get; init; }
    [StringLength(4000)] public string? Description { get; init; }
    [Required] public string Status { get; init; } = "active";
    public DateOnly? StartDate { get; init; }
    public DateOnly? DueDate { get; init; }
}

public sealed record TaskResponse(Guid Id, Guid ProjectId, string ProjectName, string Title,
    string? Description, string Status, string Priority, Guid? AssigneeId, string? AssigneeName,
    DateOnly? DueDate, int Position);
public sealed class SaveTaskRequest
{
    public Guid ProjectId { get; init; }
    [Required, StringLength(240, MinimumLength = 2)] public required string Title { get; init; }
    [StringLength(8000)] public string? Description { get; init; }
    [Required] public string Status { get; init; } = "todo";
    [Required] public string Priority { get; init; } = "medium";
    public Guid? AssigneeId { get; init; }
    public DateOnly? DueDate { get; init; }
    [Range(0, int.MaxValue)] public int Position { get; init; }
}

public sealed record LeaveResponse(Guid Id, Guid MemberId, string MemberName, string Type,
    DateOnly StartDate, DateOnly EndDate, string Reason, string Status, string? DecisionNote,
    DateTimeOffset CreatedAt);
public sealed class CreateLeaveRequest
{
    [Required] public required string Type { get; init; }
    public DateOnly StartDate { get; init; }
    public DateOnly EndDate { get; init; }
    [Required, StringLength(2000, MinimumLength = 2)] public required string Reason { get; init; }
}
public sealed class DecideLeaveRequest
{
    [Required] public required string Decision { get; init; }
    [StringLength(2000)] public string? Note { get; init; }
}

public sealed record AnnouncementResponse(Guid Id, string Title, string Body, bool Pinned,
    string AuthorName, DateTimeOffset CreatedAt);
public sealed class SaveAnnouncementRequest
{
    [Required, StringLength(240, MinimumLength = 2)] public required string Title { get; init; }
    [Required, StringLength(12000, MinimumLength = 2)] public required string Body { get; init; }
    public bool Pinned { get; init; }
}
