# Workspace and real dashboard learning flow

This slice replaces the dashboard fixture with organization-scoped data. Read the files in the order below,
then use the failure drills to practice diagnosing production-style problems.

## Request flow

1. `AuthService.RegisterAsync` creates the account, its first organization, an owner membership, and an audit event in one database transaction (`SaveChangesAsync`).
2. `useWorkspace` calls `GET /api/organizations`, restores the last selected organization, and provides the selection to the rest of the UI.
3. `useDashboard` calls `GET /api/organizations/{organizationId}/dashboard?date=YYYY-MM-DD` with the JWT through `apiFetch`.
4. `OrganizationsController` reads the user id from the validated JWT. It never accepts a user id from the browser.
5. `WorkspaceService.GetDashboardAsync` verifies active membership with `IgnoreQueryFilters`, then sets `CurrentOrganizationId` before any business query.
6. EF Core global query filters add the organization predicate to every business query. Project visibility adds the role rules from the ERD on top.

## Why membership is checked before setting the tenant

The context starts with no organization, so its global filters intentionally return no business rows. Membership lookup is the only query that bypasses the filters. After it proves that the authenticated user belongs to the requested organization, the service sets the tenant id. Missing or removed membership returns the same 404 response so the API does not reveal whether another tenant exists.

## Files to study

- `Workly.Application/Workspace/WorkspaceContracts.cs`: the public request/response boundary. Notice the explicit JSON name for `in-progress`; database enum names and frontend keys are not automatically identical.
- `Workly.Infrastructure/Workspace/WorkspaceService.cs`: authorization, tenant setup, visibility rules, dashboard projections, and bounded result sizes.
- `Workly.Api/Controllers/OrganizationsController.cs`: HTTP-only concerns and JWT identity extraction.
- `app/composables/useWorkspace.ts`: organization selection and backward-compatible onboarding for accounts created before this slice.
- `app/composables/useDashboard.ts`: loading, empty, error, retry-safe request versioning, and real API data.

## Failure drills

### Dashboard always returns empty data

Put a breakpoint immediately before `db.CurrentOrganizationId = organizationId`. Confirm the membership lookup uses `IgnoreQueryFilters` and that the tenant id is set before the first dashboard query. Use `ToQueryString()` on one query and look for the organization predicate.

### One user can see another organization's project

Treat this as a security incident. Check the composite foreign keys, `CurrentOrganizationId`, and the global filter test first. Then test the project visibility branch for the member's role. Never fix it by trusting an organization id or role sent by the frontend.

### Dashboard flashes old organization data after switching quickly

`useDashboard` increments `requestVersion` for each load. Only the latest response can replace the state. Inspect the Network panel: an older request may finish last, but it must be ignored.

### Registration succeeds but the workspace is missing

All registration rows are tracked before one `SaveChangesAsync`. Inspect the database constraint named in the exception and check slug generation. Do not add a second save call: that could leave a user without its required first workspace.

### Debug build reports locked DLL files

The API is already running and owns `bin/Debug` assemblies. Stop/restart that development process, or validate with `dotnet build apps/api/Workly.sln -c Release`. A file lock is not a compiler error.

## Verification

```powershell
dotnet build apps/api/Workly.sln -c Release
dotnet test apps/api/Workly.sln -c Release
npm.cmd run lint --workspace apps/web
npm.cmd run typecheck --workspace apps/web
npm.cmd run test --workspace apps/web
npm.cmd run build --workspace apps/web
```

Restart the running API after backend changes so it loads the new assemblies. Existing accounts with no membership receive a personal workspace from `useWorkspace`; newly registered accounts receive one atomically from the API.
