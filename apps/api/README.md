# Workly API

ASP.NET Core .NET 10 Web API with PostgreSQL 17 and EF Core 10.

## Local setup

From the repository root:

```bash
docker compose up -d postgres
dotnet tool restore
dotnet ef database update --project apps/api/src/Workly.Infrastructure
dotnet run --project apps/api/src/Workly.Api
```

Swagger is available in Development at the URL printed by `dotnet run`; database
readiness is exposed at `/health`.

Auth endpoints: `/api/auth/register`, `/login`, `/refresh`, `/logout`, `/me`, `/profile`, and `/password`
(all under `/api/auth`). See [the Auth learning guide](../../docs/auth-learning-flow.md)
for Swagger steps, token behavior, signing-key setup, and integration tests.

Workspace endpoints: `GET/POST /api/organizations`, `PATCH /api/organizations/{organizationId}`, and
`GET /api/organizations/{organizationId}/dashboard`. Organization-scoped endpoints also provide CRUD and
workflow operations for people, departments, projects, tasks, leave, and announcements. Registration creates the first workspace and owner
membership atomically. See [the workspace/dashboard learning guide](../../docs/workspace-dashboard-learning-flow.md)
for the tenant boundary, request flow, and production-style debugging drills.

Set `WORKLY_DB_CONNECTION` when running EF commands against a non-default database.

## Quality commands

```bash
dotnet build apps/api/Workly.sln
dotnet test apps/api/Workly.sln
dotnet ef migrations has-pending-model-changes --project apps/api/src/Workly.Infrastructure
```
