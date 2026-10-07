using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Workly.Application.Workspace;

namespace Workly.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/organizations")]
public sealed class OrganizationsController(IWorkspaceService workspace) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<OrganizationResponse>>> List(CancellationToken cancellationToken) =>
        Ok(await workspace.ListOrganizationsAsync(UserId(), cancellationToken));

    [HttpPost]
    public async Task<ActionResult<OrganizationResponse>> Create(CreateOrganizationRequest request,
        CancellationToken cancellationToken)
    {
        var organization = await workspace.CreateOrganizationAsync(UserId(), request, cancellationToken);
        return CreatedAtAction(nameof(Dashboard), new { organizationId = organization.Id }, organization);
    }

    [HttpPatch("{organizationId:guid}")]
    public async Task<ActionResult<OrganizationResponse>> Update(Guid organizationId,
        UpdateOrganizationRequest request, CancellationToken cancellationToken) =>
        Ok(await workspace.UpdateOrganizationAsync(UserId(), organizationId, request, cancellationToken));

    [HttpGet("{organizationId:guid}/dashboard")]
    public async Task<ActionResult<DashboardResponse>> Dashboard(Guid organizationId,
        [FromQuery] DateOnly? date, CancellationToken cancellationToken) =>
        Ok(await workspace.GetDashboardAsync(UserId(), organizationId,
            date ?? DateOnly.FromDateTime(DateTime.UtcNow), cancellationToken));

    private Guid UserId() => Guid.TryParse(User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value, out var userId)
        ? userId : throw new WorkspaceException(401, "The access token has no valid user identifier.");
}
