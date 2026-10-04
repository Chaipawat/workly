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

Set `WORKLY_DB_CONNECTION` when running EF commands against a non-default database.

## Quality commands

```bash
dotnet build apps/api/Workly.sln
dotnet test apps/api/Workly.sln
dotnet ef migrations has-pending-model-changes --project apps/api/src/Workly.Infrastructure
```
