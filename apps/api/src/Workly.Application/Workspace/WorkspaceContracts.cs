using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Workly.Application.Workspace;

public sealed class CreateOrganizationRequest
{
    [Required, StringLength(160, MinimumLength = 2)]
    public required string Name { get; init; }

    [Required, RegularExpression("^[a-z0-9]+(?:-[a-z0-9]+)*$"), StringLength(80, MinimumLength = 2)]
    public required string Slug { get; init; }
}

public sealed class UpdateOrganizationRequest
{
    [Required, StringLength(160, MinimumLength = 2)]
    public required string Name { get; init; }

    [Required, RegularExpression("^[a-z0-9]+(?:-[a-z0-9]+)*$"), StringLength(80, MinimumLength = 2)]
    public required string Slug { get; init; }
}

public sealed record OrganizationResponse(Guid Id, string Name, string Slug, string Role);

public sealed record DashboardMetricResponse(string Label, int Value, string Trend, string Icon, string Tone);
public sealed record DashboardProjectResponse(Guid Id, string Name, string Description, int CompletedTasks,
    int TotalTasks, string DueLabel, IReadOnlyList<string> MemberInitials, string Accent);
public sealed record LeaveTodayResponse(Guid Id, string Name, string Initials, string Role, string Type,
    string ReturnLabel);
public sealed record UpcomingResponse(string Id, string Day, string Title, string Detail, string Kind);
public sealed record ActivityResponse(Guid Id, string Actor, string Initials, string Action, string Target,
    string Time, string Tone);
public sealed record TeamMemberResponse(Guid Id, string Name, string Initials, string Role, string Status);
public sealed record TaskStatusResponse(int Todo,
    [property: JsonPropertyName("in-progress")] int InProgress, int Done);
public sealed record DashboardResponse(string OrganizationName, string CurrentUserName,
    IReadOnlyList<DashboardMetricResponse> Metrics, IReadOnlyList<DashboardProjectResponse> Projects,
    IReadOnlyList<LeaveTodayResponse> OnLeaveToday, IReadOnlyList<UpcomingResponse> Upcoming,
    TaskStatusResponse TaskStatus, IReadOnlyList<TeamMemberResponse> Team,
    IReadOnlyList<ActivityResponse> Activities);

public interface IWorkspaceService
{
    Task<IReadOnlyList<OrganizationResponse>> ListOrganizationsAsync(Guid userId, CancellationToken cancellationToken);
    Task<OrganizationResponse> CreateOrganizationAsync(Guid userId, CreateOrganizationRequest request,
        CancellationToken cancellationToken);
    Task<OrganizationResponse> UpdateOrganizationAsync(Guid userId, Guid organizationId,
        UpdateOrganizationRequest request, CancellationToken cancellationToken);
    Task<DashboardResponse> GetDashboardAsync(Guid userId, Guid organizationId, DateOnly today,
        CancellationToken cancellationToken);
}

public sealed class WorkspaceException(int statusCode, string message) : Exception(message)
{
    public int StatusCode { get; } = statusCode;
}
