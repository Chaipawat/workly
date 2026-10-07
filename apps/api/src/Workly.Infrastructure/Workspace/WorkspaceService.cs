using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Workly.Application.Workspace;
using Workly.Domain.Entities;
using Workly.Domain.Enums;
using Workly.Infrastructure.Persistence;
using TaskStatus = Workly.Domain.Enums.TaskStatus;

namespace Workly.Infrastructure.Workspace;

public sealed class WorkspaceService(WorklyDbContext db) : IWorkspaceService
{
    public async Task<IReadOnlyList<OrganizationResponse>> ListOrganizationsAsync(
        Guid userId, CancellationToken cancellationToken)
    {
        var organizations = await db.OrganizationMembers.IgnoreQueryFilters()
            .Where(x => x.UserId == userId && x.Status == MembershipStatus.Active)
            .OrderBy(x => x.Organization.Name)
            .Select(x => new { x.OrganizationId, x.Organization.Name, x.Organization.Slug, x.Role })
            .ToListAsync(cancellationToken);
        return organizations.Select(x => new OrganizationResponse(x.OrganizationId, x.Name, x.Slug,
            RoleName(x.Role))).ToList();
    }

    public async Task<OrganizationResponse> CreateOrganizationAsync(Guid userId,
        CreateOrganizationRequest request, CancellationToken cancellationToken)
    {
        var name = request.Name.Trim();
        var slug = request.Slug.Trim().ToLowerInvariant();
        var organization = new Organization { Name = name, Slug = slug };
        var owner = new OrganizationMember
        {
            Organization = organization,
            OrganizationId = organization.Id,
            UserId = userId,
            Role = OrganizationRole.Owner,
            JobTitle = "Owner"
        };
        db.Organizations.Add(organization);
        db.OrganizationMembers.Add(owner);
        db.ActivityLogs.Add(new ActivityLog
        {
            OrganizationId = organization.Id,
            Actor = owner,
            ActorId = owner.Id,
            Action = "organization.created",
            EntityType = "organization",
            EntityId = organization.Id,
            Metadata = JsonSerializer.Serialize(new { name })
        });
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (
            exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            throw new WorkspaceException(409, "Organization slug is already in use.");
        }
        return new OrganizationResponse(organization.Id, organization.Name, organization.Slug, "owner");
    }

    public async Task<OrganizationResponse> UpdateOrganizationAsync(Guid userId, Guid organizationId,
        UpdateOrganizationRequest request, CancellationToken cancellationToken)
    {
        var membership = await db.OrganizationMembers.IgnoreQueryFilters()
            .SingleOrDefaultAsync(x => x.OrganizationId == organizationId && x.UserId == userId
                && x.Status == MembershipStatus.Active, cancellationToken)
            ?? throw new WorkspaceException(404, "Organization was not found or you are not an active member.");
        if (membership.Role is not (OrganizationRole.Owner or OrganizationRole.Admin))
            throw new WorkspaceException(403, "Only organization owners and admins can update workspace settings.");

        db.CurrentOrganizationId = organizationId;
        var organization = await db.Organizations.SingleAsync(x => x.Id == organizationId, cancellationToken);
        var oldName = organization.Name;
        var newName = request.Name.Trim();
        var newSlug = request.Slug.Trim().ToLowerInvariant();
        organization.Name = newName;
        organization.Slug = newSlug;
        db.ActivityLogs.Add(new ActivityLog
        {
            OrganizationId = organizationId,
            ActorId = membership.Id,
            Actor = membership,
            Action = "organization.updated",
            EntityType = "organization",
            EntityId = organizationId,
            Metadata = JsonSerializer.Serialize(new { oldName, name = newName })
        });
        try { await db.SaveChangesAsync(cancellationToken); }
        catch (DbUpdateException exception) when (
            exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        { throw new WorkspaceException(409, "Organization slug is already in use."); }
        return new OrganizationResponse(organization.Id, organization.Name, organization.Slug,
            RoleName(membership.Role));
    }

    public async Task<DashboardResponse> GetDashboardAsync(Guid userId, Guid organizationId,
        DateOnly today, CancellationToken cancellationToken)
    {
        var membership = await db.OrganizationMembers.IgnoreQueryFilters()
            .Include(x => x.Organization)
            .Include(x => x.User)
            .SingleOrDefaultAsync(x => x.OrganizationId == organizationId && x.UserId == userId
                && x.Status == MembershipStatus.Active, cancellationToken)
            ?? throw new WorkspaceException(404, "Organization was not found or you are not an active member.");

        db.CurrentOrganizationId = organizationId;
        var tomorrow = today.AddDays(1);
        var inSevenDays = today.AddDays(7);
        var monthEnd = new DateOnly(today.Year, today.Month,
            DateTime.DaysInMonth(today.Year, today.Month));

        var memberCount = await db.OrganizationMembers.CountAsync(
            x => x.Status == MembershipStatus.Active, cancellationToken);
        var departmentCount = await db.Departments.CountAsync(cancellationToken);
        var activeProjectCount = await db.Projects.CountAsync(
            x => x.Status == ProjectStatus.Active, cancellationToken);
        var pendingInvites = await db.Invitations.CountAsync(
            x => x.AcceptedAt == null && x.RevokedAt == null && x.ExpiresAt > DateTimeOffset.UtcNow,
            cancellationToken);
        var projectsDueThisMonth = await db.Projects.CountAsync(x => x.Status == ProjectStatus.Active
            && x.DueDate >= today && x.DueDate <= monthEnd, cancellationToken);
        var onLeaveCount = await db.LeaveRequests.CountAsync(x => x.Status == LeaveRequestStatus.Approved
            && x.StartDate <= today && x.EndDate >= today, cancellationToken);

        var metrics = new List<DashboardMetricResponse>
        {
            new("Members", memberCount, $"{pendingInvites} pending invites", "users", "blue"),
            new("Departments", departmentCount, "Across the organization", "departments", "indigo"),
            new("Active projects", activeProjectCount, $"{projectsDueThisMonth} due this month", "projects", "emerald"),
            new("On leave today", onLeaveCount, onLeaveCount == 1 ? "1 teammate away" : $"{onLeaveCount} teammates away", "leave", "amber")
        };

        var projectRows = await VisibleProjects(membership)
            .Where(x => x.Status == ProjectStatus.Active)
            .OrderBy(x => x.DueDate == null).ThenBy(x => x.DueDate)
            .Take(3)
            .Select(x => new
            {
                x.Id, x.Name, x.Description, x.DueDate,
                Total = x.Tasks.Count,
                Completed = x.Tasks.Count(task => task.Status == TaskStatus.Done),
                MemberNames = x.Members.OrderBy(member => member.AddedAt).Take(3)
                    .Select(member => member.Member.User.DisplayName).ToList()
            }).ToListAsync(cancellationToken);
        var accents = new[] { "blue", "indigo", "emerald" };
        var projects = projectRows.Select((x, index) => new DashboardProjectResponse(x.Id, x.Name,
            x.Description ?? "", x.Completed, x.Total,
            x.DueDate is null ? "No due date" : $"Due {x.DueDate.Value:MMM d}",
            x.MemberNames.Select(Initials).ToList(), accents[index % accents.Length])).ToList();

        var leaveRows = await db.LeaveRequests
            .Where(x => x.Status == LeaveRequestStatus.Approved && x.StartDate <= today && x.EndDate >= today)
            .OrderBy(x => x.EndDate).Take(5)
            .Select(x => new { x.Id, x.Member.User.DisplayName, x.Member.JobTitle, x.Type, x.EndDate })
            .ToListAsync(cancellationToken);
        var onLeave = leaveRows.Select(x => new LeaveTodayResponse(x.Id, x.DisplayName,
            Initials(x.DisplayName), x.JobTitle, x.Type.ToString().ToLowerInvariant(),
            x.EndDate == today ? "Returns tomorrow" : $"Returns {x.EndDate.AddDays(1):MMM d}")).ToList();

        var taskUpcoming = await VisibleTasks(membership)
            .Where(x => x.Status != TaskStatus.Done && x.DueDate >= today && x.DueDate <= inSevenDays)
            .OrderBy(x => x.DueDate).Take(8)
            .Select(x => new { x.Id, x.DueDate, x.Title, ProjectName = x.Project.Name })
            .ToListAsync(cancellationToken);
        var leaveUpcoming = await db.LeaveRequests
            .Where(x => x.Status == LeaveRequestStatus.Approved && x.StartDate >= tomorrow
                && x.StartDate <= inSevenDays)
            .OrderBy(x => x.StartDate).Take(8)
            .Select(x => new { x.Id, x.StartDate, x.Member.User.DisplayName })
            .ToListAsync(cancellationToken);
        var upcoming = taskUpcoming.Select(x => (Date: x.DueDate!.Value,
                Item: new UpcomingResponse($"task-{x.Id}", DayLabel(x.DueDate.Value, today), x.Title,
                    $"Task due · {x.ProjectName}", "task")))
            .Concat(leaveUpcoming.Select(x => (Date: x.StartDate,
                Item: new UpcomingResponse($"leave-{x.Id}", DayLabel(x.StartDate, today),
                    $"{x.DisplayName} away", "Approved leave", "leave"))))
            .OrderBy(x => x.Date).Take(8).Select(x => x.Item).ToList();

        var statusCounts = await VisibleTasks(membership).GroupBy(x => x.Status)
            .Select(group => new { Status = group.Key, Count = group.Count() })
            .ToDictionaryAsync(x => x.Status, x => x.Count, cancellationToken);
        var taskStatus = new TaskStatusResponse(statusCounts.GetValueOrDefault(TaskStatus.Todo),
            statusCounts.GetValueOrDefault(TaskStatus.InProgress), statusCounts.GetValueOrDefault(TaskStatus.Done));

        var teamRows = await db.OrganizationMembers.Where(x => x.Status == MembershipStatus.Active)
            .OrderByDescending(x => x.JoinedAt).Take(5)
            .Select(x => new { x.Id, x.User.DisplayName, x.JobTitle }).ToListAsync(cancellationToken);
        var awayIds = await db.LeaveRequests.Where(x => x.Status == LeaveRequestStatus.Approved
                && x.StartDate <= today && x.EndDate >= today)
            .Select(x => x.MemberId).ToListAsync(cancellationToken);
        var away = awayIds.ToHashSet();
        var team = teamRows.Select(x => new TeamMemberResponse(x.Id, x.DisplayName,
            Initials(x.DisplayName), x.JobTitle, away.Contains(x.Id) ? "away" : "offline")).ToList();

        var activityRows = await db.ActivityLogs.OrderByDescending(x => x.CreatedAt).Take(8)
            .Select(x => new { x.Id, x.Action, x.EntityType, x.Metadata, x.CreatedAt,
                Actor = x.Actor == null ? null : x.Actor.User.DisplayName }).ToListAsync(cancellationToken);
        var activities = activityRows.Select(x => MapActivity(x.Id, x.Actor, x.Action, x.EntityType,
            x.Metadata, x.CreatedAt)).ToList();

        return new DashboardResponse(membership.Organization.Name, membership.User.DisplayName,
            metrics, projects, onLeave, upcoming, taskStatus, team, activities);
    }

    private IQueryable<Project> VisibleProjects(OrganizationMember member) => member.Role switch
    {
        OrganizationRole.Owner or OrganizationRole.Admin => db.Projects,
        OrganizationRole.Hr => db.Projects.Where(_ => false),
        _ => db.Projects.Where(x => x.Members.Any(memberLink => memberLink.MemberId == member.Id))
    };

    private IQueryable<WorkTask> VisibleTasks(OrganizationMember member) => member.Role switch
    {
        OrganizationRole.Owner or OrganizationRole.Admin => db.Tasks,
        OrganizationRole.Hr => db.Tasks.Where(_ => false),
        _ => db.Tasks.Where(x => x.Project.Members.Any(memberLink => memberLink.MemberId == member.Id))
    };

    private static string RoleName(OrganizationRole role) => role.ToString().ToLowerInvariant();

    private static string Initials(string name)
    {
        var initials = string.Concat(name.Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Take(2).Select(part => char.ToUpperInvariant(part[0])));
        return string.IsNullOrEmpty(initials) ? "?" : initials;
    }

    private static string DayLabel(DateOnly date, DateOnly today) => date == today ? "Today"
        : date == today.AddDays(1) ? "Tomorrow" : date.ToString("MMM d");

    private static ActivityResponse MapActivity(Guid id, string? actor, string action, string entityType,
        string metadata, DateTimeOffset createdAt)
    {
        var actorName = actor ?? "Workly";
        var actionText = action.Split('.').LastOrDefault() switch
        {
            "created" => "created", "updated" => "updated", "completed" => "completed",
            "requested" => "requested", "approved" => "approved", "rejected" => "rejected",
            "posted" => "posted", _ => "changed"
        };
        var target = entityType.Replace('_', ' ');
        try
        {
            using var document = JsonDocument.Parse(metadata);
            if (document.RootElement.TryGetProperty("name", out var name)) target = name.GetString() ?? target;
            else if (document.RootElement.TryGetProperty("title", out var title)) target = title.GetString() ?? target;
        }
        catch (JsonException) { }
        var elapsed = DateTimeOffset.UtcNow - createdAt;
        var time = elapsed.TotalMinutes < 1 ? "Just now"
            : elapsed.TotalHours < 1 ? $"{(int)elapsed.TotalMinutes} minutes ago"
            : elapsed.TotalDays < 1 ? $"{(int)elapsed.TotalHours} hours ago"
            : elapsed.TotalDays < 2 ? "Yesterday" : createdAt.ToString("MMM d");
        var tone = actionText switch { "approved" or "completed" or "posted" => "emerald",
            "requested" => "amber", "created" => "indigo", _ => "blue" };
        return new ActivityResponse(id, actorName, Initials(actorName), actionText, target, time, tone);
    }
}
