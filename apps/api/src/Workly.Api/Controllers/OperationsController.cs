using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Workly.Application.Operations;
using Workly.Application.Workspace;
using Workly.Domain.Entities;
using Workly.Domain.Enums;
using Workly.Infrastructure.Persistence;
using TaskStatus = Workly.Domain.Enums.TaskStatus;

namespace Workly.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/organizations/{organizationId:guid}")]
public sealed class OperationsController(WorklyDbContext db) : ControllerBase
{
    [HttpGet("people")]
    public async Task<ActionResult<IReadOnlyList<MemberResponse>>> People(Guid organizationId,
        CancellationToken cancellationToken)
    {
        await MemberAsync(organizationId, cancellationToken);
        return await db.OrganizationMembers.OrderBy(x => x.User.DisplayName)
            .Select(x => new MemberResponse(x.Id, x.User.DisplayName, x.User.Email, x.JobTitle,
                Name(x.Role), Name(x.Status), x.DepartmentId, x.Department == null ? null : x.Department.Name))
            .ToListAsync(cancellationToken);
    }

    [HttpPatch("people/{memberId:guid}")]
    public async Task<ActionResult<MemberResponse>> UpdatePerson(Guid organizationId, Guid memberId,
        UpdateMemberRequest request, CancellationToken cancellationToken)
    {
        var actor = await MemberAsync(organizationId, cancellationToken);
        Require(actor, OrganizationRole.Owner, OrganizationRole.Admin, OrganizationRole.Hr);
        var member = await db.OrganizationMembers.Include(x => x.User).Include(x => x.Department)
            .SingleOrDefaultAsync(x => x.Id == memberId, cancellationToken)
            ?? throw NotFound("Member was not found.");
        if (member.Role == OrganizationRole.Owner && (request.Role != "owner" || request.Status != "active"))
            throw Conflict("The workspace owner cannot be removed or demoted.");
        if (actor.Role == OrganizationRole.Hr && (request.Role != Name(member.Role) || request.Status != Name(member.Status)))
            throw Forbidden("HR can update people details but cannot change roles or membership status.");
        member.Role = Parse<OrganizationRole>(request.Role, "role");
        member.Status = Parse<MembershipStatus>(request.Status, "status");
        member.JobTitle = request.JobTitle.Trim();
        member.DepartmentId = request.DepartmentId;
        if (request.DepartmentId is not null && !await db.Departments.AnyAsync(x => x.Id == request.DepartmentId, cancellationToken))
            throw BadRequest("Department does not belong to this workspace.");
        Audit(organizationId, actor, "member.updated", "member", member.Id, new { name = member.User.DisplayName });
        await db.SaveChangesAsync(cancellationToken);
        return new MemberResponse(member.Id, member.User.DisplayName, member.User.Email, member.JobTitle,
            Name(member.Role), Name(member.Status), member.DepartmentId, member.Department?.Name);
    }

    [HttpPost("people")]
    public async Task<ActionResult<MemberResponse>> AddPerson(Guid organizationId, AddMemberRequest request,
        CancellationToken cancellationToken)
    {
        var actor = await MemberAsync(organizationId, cancellationToken);
        Require(actor, OrganizationRole.Owner, OrganizationRole.Admin, OrganizationRole.Hr);
        var role = Parse<OrganizationRole>(request.Role, "role");
        if (role == OrganizationRole.Owner) throw BadRequest("Owner membership can only be created through ownership transfer.");
        if (request.DepartmentId is not null && !await db.Departments.AnyAsync(x => x.Id == request.DepartmentId, cancellationToken))
            throw BadRequest("Department does not belong to this workspace.");
        var email = request.Email.Trim().ToLowerInvariant();
        var user = await db.Users.SingleOrDefaultAsync(x => x.Email == email, cancellationToken)
            ?? throw NotFound("That user must register a Workly account before being added.");
        var existing = await db.OrganizationMembers.SingleOrDefaultAsync(x => x.UserId == user.Id, cancellationToken);
        if (existing is not null && existing.Status == MembershipStatus.Active) throw Conflict("This user is already an active member.");
        var member = existing ?? new OrganizationMember { OrganizationId = organizationId, UserId = user.Id,
            User = user, JobTitle = request.JobTitle.Trim() };
        member.Status = MembershipStatus.Active; member.Role = role; member.JobTitle = request.JobTitle.Trim(); member.DepartmentId = request.DepartmentId;
        if (existing is null) db.OrganizationMembers.Add(member);
        Audit(organizationId, actor, "member.added", "member", member.Id, new { name = user.DisplayName });
        await db.SaveChangesAsync(cancellationToken);
        return StatusCode(201, new MemberResponse(member.Id, user.DisplayName, user.Email, member.JobTitle,
            Name(member.Role), Name(member.Status), member.DepartmentId, null));
    }

    [HttpGet("departments")]
    public async Task<ActionResult<IReadOnlyList<DepartmentResponse>>> Departments(Guid organizationId,
        CancellationToken cancellationToken)
    {
        await MemberAsync(organizationId, cancellationToken);
        return await db.Departments.OrderBy(x => x.Name).Select(x => new DepartmentResponse(x.Id, x.Name,
            x.ManagerId, x.Manager == null ? null : x.Manager.User.DisplayName, x.Members.Count)).ToListAsync(cancellationToken);
    }

    [HttpPost("departments")]
    public async Task<ActionResult<DepartmentResponse>> CreateDepartment(Guid organizationId,
        SaveDepartmentRequest request, CancellationToken cancellationToken)
    {
        var actor = await MemberAsync(organizationId, cancellationToken);
        Require(actor, OrganizationRole.Owner, OrganizationRole.Admin, OrganizationRole.Hr);
        await ValidateManager(request.ManagerId, cancellationToken);
        var department = new Department { OrganizationId = organizationId, Name = request.Name.Trim(), ManagerId = request.ManagerId };
        db.Departments.Add(department);
        Audit(organizationId, actor, "department.created", "department", department.Id, new { name = department.Name });
        await db.SaveChangesAsync(cancellationToken);
        return StatusCode(201, new DepartmentResponse(department.Id, department.Name, department.ManagerId, null, 0));
    }

    [HttpPatch("departments/{departmentId:guid}")]
    public async Task<ActionResult<DepartmentResponse>> UpdateDepartment(Guid organizationId, Guid departmentId,
        SaveDepartmentRequest request, CancellationToken cancellationToken)
    {
        var actor = await MemberAsync(organizationId, cancellationToken);
        Require(actor, OrganizationRole.Owner, OrganizationRole.Admin, OrganizationRole.Hr);
        var department = await db.Departments.SingleOrDefaultAsync(x => x.Id == departmentId, cancellationToken)
            ?? throw NotFound("Department was not found.");
        await ValidateManager(request.ManagerId, cancellationToken);
        department.Name = request.Name.Trim(); department.ManagerId = request.ManagerId;
        Audit(organizationId, actor, "department.updated", "department", department.Id, new { name = department.Name });
        await db.SaveChangesAsync(cancellationToken);
        return new DepartmentResponse(department.Id, department.Name, department.ManagerId, null,
            await db.OrganizationMembers.CountAsync(x => x.DepartmentId == department.Id, cancellationToken));
    }

    [HttpDelete("departments/{departmentId:guid}")]
    public async Task<IActionResult> DeleteDepartment(Guid organizationId, Guid departmentId, CancellationToken cancellationToken)
    {
        var actor = await MemberAsync(organizationId, cancellationToken); Require(actor, OrganizationRole.Owner, OrganizationRole.Admin);
        var department = await db.Departments.SingleOrDefaultAsync(x => x.Id == departmentId, cancellationToken)
            ?? throw NotFound("Department was not found.");
        if (await db.OrganizationMembers.AnyAsync(x => x.DepartmentId == departmentId, cancellationToken))
            throw Conflict("Move all members out of this department before deleting it.");
        db.Departments.Remove(department); Audit(organizationId, actor, "department.deleted", "department", department.Id, new { name = department.Name });
        await db.SaveChangesAsync(cancellationToken); return NoContent();
    }

    [HttpGet("projects")]
    public async Task<ActionResult<IReadOnlyList<ProjectResponse>>> Projects(Guid organizationId, CancellationToken cancellationToken)
    {
        var member = await MemberAsync(organizationId, cancellationToken);
        var query = VisibleProjects(member);
        return await query.OrderByDescending(x => x.CreatedAt).Select(x => new ProjectResponse(x.Id, x.Name,
            x.Description, Name(x.Status), x.StartDate, x.DueDate, x.Tasks.Count(t => t.Status == TaskStatus.Done), x.Tasks.Count))
            .ToListAsync(cancellationToken);
    }

    [HttpPost("projects")]
    public async Task<ActionResult<ProjectResponse>> CreateProject(Guid organizationId, SaveProjectRequest request,
        CancellationToken cancellationToken)
    {
        var actor = await MemberAsync(organizationId, cancellationToken); Require(actor, OrganizationRole.Owner, OrganizationRole.Admin, OrganizationRole.Manager);
        ValidateDates(request.StartDate, request.DueDate);
        var project = new Project { OrganizationId = organizationId, Name = request.Name.Trim(), Description = request.Description?.Trim(),
            Status = Parse<ProjectStatus>(request.Status, "status"), StartDate = request.StartDate, DueDate = request.DueDate,
            CreatedById = actor.Id };
        project.Members.Add(new ProjectMember { OrganizationId = organizationId, Project = project, ProjectId = project.Id, MemberId = actor.Id });
        db.Projects.Add(project); Audit(organizationId, actor, "project.created", "project", project.Id, new { name = project.Name });
        await db.SaveChangesAsync(cancellationToken);
        return StatusCode(201, new ProjectResponse(project.Id, project.Name, project.Description, Name(project.Status), project.StartDate, project.DueDate, 0, 0));
    }

    [HttpPatch("projects/{projectId:guid}")]
    public async Task<ActionResult<ProjectResponse>> UpdateProject(Guid organizationId, Guid projectId, SaveProjectRequest request,
        CancellationToken cancellationToken)
    {
        var actor = await MemberAsync(organizationId, cancellationToken); Require(actor, OrganizationRole.Owner, OrganizationRole.Admin, OrganizationRole.Manager);
        var project = await VisibleProjects(actor).SingleOrDefaultAsync(x => x.Id == projectId, cancellationToken)
            ?? throw NotFound("Project was not found.");
        ValidateDates(request.StartDate, request.DueDate);
        project.Name = request.Name.Trim(); project.Description = request.Description?.Trim(); project.Status = Parse<ProjectStatus>(request.Status, "status");
        project.StartDate = request.StartDate; project.DueDate = request.DueDate;
        Audit(organizationId, actor, "project.updated", "project", project.Id, new { name = project.Name }); await db.SaveChangesAsync(cancellationToken);
        var counts = await db.Tasks.Where(x => x.ProjectId == project.Id).GroupBy(_ => 1)
            .Select(x => new { Total = x.Count(), Done = x.Count(t => t.Status == TaskStatus.Done) }).SingleOrDefaultAsync(cancellationToken);
        return new ProjectResponse(project.Id, project.Name, project.Description, Name(project.Status), project.StartDate, project.DueDate, counts?.Done ?? 0, counts?.Total ?? 0);
    }

    [HttpGet("tasks")]
    public async Task<ActionResult<IReadOnlyList<TaskResponse>>> Tasks(Guid organizationId, [FromQuery] Guid? projectId,
        CancellationToken cancellationToken)
    {
        var member = await MemberAsync(organizationId, cancellationToken); var query = VisibleTasks(member);
        if (projectId is not null) query = query.Where(x => x.ProjectId == projectId);
        return await query.OrderBy(x => x.Status).ThenBy(x => x.Position).ThenBy(x => x.DueDate)
            .Select(x => new TaskResponse(x.Id, x.ProjectId, x.Project.Name, x.Title, x.Description, Name(x.Status), Name(x.Priority),
                x.AssigneeId, x.Assignee == null ? null : x.Assignee.User.DisplayName, x.DueDate, x.Position)).ToListAsync(cancellationToken);
    }

    [HttpPost("tasks")]
    public async Task<ActionResult<TaskResponse>> CreateTask(Guid organizationId, SaveTaskRequest request, CancellationToken cancellationToken)
    {
        var actor = await MemberAsync(organizationId, cancellationToken);
        var project = await VisibleProjects(actor).SingleOrDefaultAsync(x => x.Id == request.ProjectId, cancellationToken)
            ?? throw NotFound("Project was not found.");
        await ValidateAssignee(project.Id, request.AssigneeId, cancellationToken);
        var task = new WorkTask { OrganizationId = organizationId, ProjectId = project.Id, Title = request.Title.Trim(), Description = request.Description?.Trim(),
            Status = Parse<TaskStatus>(request.Status, "status"), Priority = Parse<TaskPriority>(request.Priority, "priority"), AssigneeId = request.AssigneeId,
            DueDate = request.DueDate, Position = request.Position };
        db.Tasks.Add(task); Audit(organizationId, actor, "task.created", "task", task.Id, new { title = task.Title }); await db.SaveChangesAsync(cancellationToken);
        return StatusCode(201, new TaskResponse(task.Id, project.Id, project.Name, task.Title, task.Description, Name(task.Status), Name(task.Priority), task.AssigneeId, null, task.DueDate, task.Position));
    }

    [HttpPatch("tasks/{taskId:guid}")]
    public async Task<ActionResult<TaskResponse>> UpdateTask(Guid organizationId, Guid taskId, SaveTaskRequest request, CancellationToken cancellationToken)
    {
        var actor = await MemberAsync(organizationId, cancellationToken);
        var task = await VisibleTasks(actor).Include(x => x.Project).SingleOrDefaultAsync(x => x.Id == taskId, cancellationToken)
            ?? throw NotFound("Task was not found.");
        if (request.ProjectId != task.ProjectId) throw BadRequest("Moving a task between projects is not supported.");
        await ValidateAssignee(task.ProjectId, request.AssigneeId, cancellationToken);
        task.Title = request.Title.Trim(); task.Description = request.Description?.Trim(); task.Status = Parse<TaskStatus>(request.Status, "status");
        task.Priority = Parse<TaskPriority>(request.Priority, "priority"); task.AssigneeId = request.AssigneeId; task.DueDate = request.DueDate; task.Position = request.Position;
        Audit(organizationId, actor, "task.updated", "task", task.Id, new { title = task.Title }); await db.SaveChangesAsync(cancellationToken);
        return new TaskResponse(task.Id, task.ProjectId, task.Project.Name, task.Title, task.Description, Name(task.Status), Name(task.Priority), task.AssigneeId, null, task.DueDate, task.Position);
    }

    [HttpDelete("tasks/{taskId:guid}")]
    public async Task<IActionResult> DeleteTask(Guid organizationId, Guid taskId, CancellationToken cancellationToken)
    {
        var actor = await MemberAsync(organizationId, cancellationToken); var task = await VisibleTasks(actor).SingleOrDefaultAsync(x => x.Id == taskId, cancellationToken)
            ?? throw NotFound("Task was not found.");
        db.Tasks.Remove(task); Audit(organizationId, actor, "task.deleted", "task", task.Id, new { title = task.Title }); await db.SaveChangesAsync(cancellationToken); return NoContent();
    }

    [HttpGet("leave")]
    public async Task<ActionResult<IReadOnlyList<LeaveResponse>>> Leave(Guid organizationId, CancellationToken cancellationToken)
    {
        var member = await MemberAsync(organizationId, cancellationToken);
        var query = member.Role is OrganizationRole.Owner or OrganizationRole.Admin or OrganizationRole.Hr
            ? db.LeaveRequests : member.Role == OrganizationRole.Manager
                ? db.LeaveRequests.Where(x => x.Member.DepartmentId == member.DepartmentId || x.MemberId == member.Id)
                : db.LeaveRequests.Where(x => x.MemberId == member.Id);
        return await query.OrderByDescending(x => x.CreatedAt).Select(x => new LeaveResponse(x.Id, x.MemberId, x.Member.User.DisplayName,
            Name(x.Type), x.StartDate, x.EndDate, x.Reason, Name(x.Status), x.DecisionNote, x.CreatedAt)).ToListAsync(cancellationToken);
    }

    [HttpPost("leave")]
    public async Task<ActionResult<LeaveResponse>> CreateLeave(Guid organizationId, CreateLeaveRequest request, CancellationToken cancellationToken)
    {
        var actor = await MemberAsync(organizationId, cancellationToken); ValidateDates(request.StartDate, request.EndDate);
        var overlaps = await db.LeaveRequests.AnyAsync(x => x.MemberId == actor.Id && x.Status != LeaveRequestStatus.Cancelled
            && x.Status != LeaveRequestStatus.Rejected && x.StartDate <= request.EndDate && x.EndDate >= request.StartDate, cancellationToken);
        if (overlaps) throw Conflict("This leave request overlaps another pending or approved request.");
        var leave = new LeaveRequest { OrganizationId = organizationId, MemberId = actor.Id, Type = Parse<LeaveType>(request.Type, "type"),
            StartDate = request.StartDate, EndDate = request.EndDate, Reason = request.Reason.Trim() };
        db.LeaveRequests.Add(leave); Audit(organizationId, actor, "leave.requested", "leave_request", leave.Id, new { name = $"{Name(leave.Type)} leave" }); await db.SaveChangesAsync(cancellationToken);
        return StatusCode(201, new LeaveResponse(leave.Id, actor.Id, actor.User.DisplayName, Name(leave.Type), leave.StartDate, leave.EndDate, leave.Reason, Name(leave.Status), null, leave.CreatedAt));
    }

    [HttpPatch("leave/{leaveId:guid}/decision")]
    public async Task<ActionResult<LeaveResponse>> DecideLeave(Guid organizationId, Guid leaveId, DecideLeaveRequest request, CancellationToken cancellationToken)
    {
        var actor = await MemberAsync(organizationId, cancellationToken); var leave = await db.LeaveRequests.Include(x => x.Member).ThenInclude(x => x.User)
            .SingleOrDefaultAsync(x => x.Id == leaveId, cancellationToken) ?? throw NotFound("Leave request was not found.");
        if (leave.Status != LeaveRequestStatus.Pending) throw Conflict("Only pending leave can be decided.");
        if (leave.MemberId == actor.Id) throw Forbidden("You cannot approve or reject your own leave request.");
        var allowed = actor.Role is OrganizationRole.Owner or OrganizationRole.Admin or OrganizationRole.Hr
            || actor.Role == OrganizationRole.Manager && actor.DepartmentId != null && actor.DepartmentId == leave.Member.DepartmentId;
        if (!allowed) throw Forbidden("You are not allowed to decide this leave request.");
        var decision = Parse<LeaveRequestStatus>(request.Decision, "decision");
        if (decision is not (LeaveRequestStatus.Approved or LeaveRequestStatus.Rejected)) throw BadRequest("Decision must be approved or rejected.");
        leave.Status = decision; leave.DecidedById = actor.Id; leave.DecidedAt = DateTimeOffset.UtcNow; leave.DecisionNote = request.Note?.Trim();
        Audit(organizationId, actor, $"leave.{Name(decision)}", "leave_request", leave.Id, new { name = $"{leave.Member.User.DisplayName}'s leave" }); await db.SaveChangesAsync(cancellationToken);
        return new LeaveResponse(leave.Id, leave.MemberId, leave.Member.User.DisplayName, Name(leave.Type), leave.StartDate, leave.EndDate, leave.Reason, Name(leave.Status), leave.DecisionNote, leave.CreatedAt);
    }

    [HttpPatch("leave/{leaveId:guid}/cancel")]
    public async Task<IActionResult> CancelLeave(Guid organizationId, Guid leaveId, CancellationToken cancellationToken)
    {
        var actor = await MemberAsync(organizationId, cancellationToken); var leave = await db.LeaveRequests.SingleOrDefaultAsync(x => x.Id == leaveId, cancellationToken)
            ?? throw NotFound("Leave request was not found.");
        if (leave.MemberId != actor.Id) throw Forbidden("You can only cancel your own leave request.");
        if (leave.Status is LeaveRequestStatus.Rejected or LeaveRequestStatus.Cancelled) throw Conflict("This leave request cannot be cancelled.");
        leave.Status = LeaveRequestStatus.Cancelled; Audit(organizationId, actor, "leave.cancelled", "leave_request", leave.Id, new { name = "leave request" });
        await db.SaveChangesAsync(cancellationToken); return NoContent();
    }

    [HttpGet("announcements")]
    public async Task<ActionResult<IReadOnlyList<AnnouncementResponse>>> Announcements(Guid organizationId, CancellationToken cancellationToken)
    {
        await MemberAsync(organizationId, cancellationToken);
        return await db.Announcements.OrderByDescending(x => x.Pinned).ThenByDescending(x => x.CreatedAt)
            .Select(x => new AnnouncementResponse(x.Id, x.Title, x.Body, x.Pinned, x.Author.User.DisplayName, x.CreatedAt)).ToListAsync(cancellationToken);
    }

    [HttpPost("announcements")]
    public async Task<ActionResult<AnnouncementResponse>> CreateAnnouncement(Guid organizationId, SaveAnnouncementRequest request, CancellationToken cancellationToken)
    {
        var actor = await MemberAsync(organizationId, cancellationToken); Require(actor, OrganizationRole.Owner, OrganizationRole.Admin, OrganizationRole.Hr);
        var announcement = new Announcement { OrganizationId = organizationId, AuthorId = actor.Id, Title = request.Title.Trim(), Body = request.Body.Trim(), Pinned = request.Pinned };
        db.Announcements.Add(announcement); Audit(organizationId, actor, "announcement.posted", "announcement", announcement.Id, new { title = announcement.Title });
        await db.SaveChangesAsync(cancellationToken); return StatusCode(201, new AnnouncementResponse(announcement.Id, announcement.Title, announcement.Body, announcement.Pinned, actor.User.DisplayName, announcement.CreatedAt));
    }

    [HttpPatch("announcements/{announcementId:guid}")]
    public async Task<ActionResult<AnnouncementResponse>> UpdateAnnouncement(Guid organizationId, Guid announcementId, SaveAnnouncementRequest request, CancellationToken cancellationToken)
    {
        var actor = await MemberAsync(organizationId, cancellationToken); Require(actor, OrganizationRole.Owner, OrganizationRole.Admin, OrganizationRole.Hr);
        var announcement = await db.Announcements.Include(x => x.Author).ThenInclude(x => x.User).SingleOrDefaultAsync(x => x.Id == announcementId, cancellationToken)
            ?? throw NotFound("Announcement was not found.");
        announcement.Title = request.Title.Trim(); announcement.Body = request.Body.Trim(); announcement.Pinned = request.Pinned;
        Audit(organizationId, actor, "announcement.updated", "announcement", announcement.Id, new { title = announcement.Title }); await db.SaveChangesAsync(cancellationToken);
        return new AnnouncementResponse(announcement.Id, announcement.Title, announcement.Body, announcement.Pinned, announcement.Author.User.DisplayName, announcement.CreatedAt);
    }

    [HttpDelete("announcements/{announcementId:guid}")]
    public async Task<IActionResult> DeleteAnnouncement(Guid organizationId, Guid announcementId, CancellationToken cancellationToken)
    {
        var actor = await MemberAsync(organizationId, cancellationToken); Require(actor, OrganizationRole.Owner, OrganizationRole.Admin, OrganizationRole.Hr);
        var announcement = await db.Announcements.SingleOrDefaultAsync(x => x.Id == announcementId, cancellationToken) ?? throw NotFound("Announcement was not found.");
        db.Announcements.Remove(announcement); Audit(organizationId, actor, "announcement.deleted", "announcement", announcement.Id, new { title = announcement.Title });
        await db.SaveChangesAsync(cancellationToken); return NoContent();
    }

    private async Task<OrganizationMember> MemberAsync(Guid organizationId, CancellationToken cancellationToken)
    {
        var userId = Guid.TryParse(User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value, out var id) ? id : throw new WorkspaceException(401, "Invalid user identity.");
        var member = await db.OrganizationMembers.IgnoreQueryFilters().Include(x => x.User)
            .SingleOrDefaultAsync(x => x.OrganizationId == organizationId && x.UserId == userId && x.Status == MembershipStatus.Active, cancellationToken)
            ?? throw NotFound("Organization was not found or you are not an active member.");
        db.CurrentOrganizationId = organizationId; return member;
    }

    private IQueryable<Project> VisibleProjects(OrganizationMember member) => member.Role switch
    {
        OrganizationRole.Owner or OrganizationRole.Admin => db.Projects,
        OrganizationRole.Hr => db.Projects.Where(_ => false),
        _ => db.Projects.Where(x => x.Members.Any(link => link.MemberId == member.Id))
    };
    private IQueryable<WorkTask> VisibleTasks(OrganizationMember member) => member.Role switch
    {
        OrganizationRole.Owner or OrganizationRole.Admin => db.Tasks,
        OrganizationRole.Hr => db.Tasks.Where(_ => false),
        _ => db.Tasks.Where(x => x.Project.Members.Any(link => link.MemberId == member.Id))
    };
    private async Task ValidateManager(Guid? id, CancellationToken token)
    {
        if (id is null) return;
        var manager = await db.OrganizationMembers.SingleOrDefaultAsync(x => x.Id == id && x.Status == MembershipStatus.Active, token)
            ?? throw BadRequest("Manager does not belong to this workspace.");
        if ((int)manager.Role < (int)OrganizationRole.Manager) throw BadRequest("Department manager must have manager role or higher.");
    }
    private async Task ValidateAssignee(Guid projectId, Guid? assigneeId, CancellationToken token)
    {
        if (assigneeId is null) return;
        if (!await db.OrganizationMembers.AnyAsync(x => x.Id == assigneeId && x.Status == MembershipStatus.Active, token))
            throw BadRequest("Task assignee must be an active workspace member.");
        if (!await db.ProjectMembers.AnyAsync(x => x.ProjectId == projectId && x.MemberId == assigneeId, token))
            db.ProjectMembers.Add(new ProjectMember { OrganizationId = db.CurrentOrganizationId!.Value,
                ProjectId = projectId, MemberId = assigneeId.Value });
    }
    private void Audit(Guid organizationId, OrganizationMember actor, string action, string entityType, Guid entityId, object metadata) =>
        db.ActivityLogs.Add(new ActivityLog { OrganizationId = organizationId, ActorId = actor.Id, Action = action,
            EntityType = entityType, EntityId = entityId, Metadata = JsonSerializer.Serialize(metadata) });
    private static void Require(OrganizationMember member, params OrganizationRole[] roles)
    { if (!roles.Contains(member.Role)) throw Forbidden("You do not have permission to perform this action."); }
    private static void ValidateDates(DateOnly? start, DateOnly? end)
    { if (start is not null && end is not null && end < start) throw BadRequest("End date cannot be before start date."); }
    private static T Parse<T>(string value, string field) where T : struct, Enum
    {
        var normalized = value.Replace("-", "", StringComparison.Ordinal).Replace("_", "", StringComparison.Ordinal);
        foreach (var candidate in Enum.GetValues<T>())
            if (string.Equals(candidate.ToString(), normalized, StringComparison.OrdinalIgnoreCase)) return candidate;
        throw BadRequest($"Invalid {field} value.");
    }
    private static string Name<T>(T value) where T : struct, Enum
    {
        var name = value.ToString();
        return string.Concat(name.Select((character, index) => char.IsUpper(character) && index > 0 ? $"-{char.ToLowerInvariant(character)}" : char.ToLowerInvariant(character).ToString()));
    }
    private static WorkspaceException BadRequest(string message) => new(400, message);
    private static WorkspaceException Forbidden(string message) => new(403, message);
    private static WorkspaceException NotFound(string message) => new(404, message);
    private static WorkspaceException Conflict(string message) => new(409, message);
}
