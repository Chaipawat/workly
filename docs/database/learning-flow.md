# Database learning flow

1. Domain entities describe C# objects (`User`, `Organization`, etc.).
2. Infrastructure configurations map properties, relationships, and constraints.
3. `WorklyDbContext` discovers configurations and exposes `DbSet<T>` queries.
4. EF migrations record schema changes. `InitialCreate` creates the initial tables.
5. `dotnet ef database update` applies pending migrations to PostgreSQL.

From the repository root:

```powershell
docker compose up -d postgres
dotnet tool restore
dotnet ef database update --project apps/api/src/Workly.Infrastructure
docker exec workly-postgres psql -U workly -d workly -c '\dt'
docker exec workly-postgres psql -U workly -d workly -c '\d users'
Get-Content apps/api/tests/database/schema-smoke.sql -Raw | docker exec -i workly-postgres psql -v ON_ERROR_STOP=1 -U workly -d workly
```

The smoke script checks real PostgreSQL email uniqueness and cross-organization
foreign keys. It rolls back all sample rows.

## Organization scope

Business queries are filtered using `WorklyDbContext.CurrentOrganizationId`.
Without a selected organization, these queries return no rows. Account-level
`User` and `RefreshToken` queries are not organization-filtered.

Only trusted backend code may set the organization scope after verifying active
membership. This authentication/membership resolution is not implemented yet.
Never set scope directly from an unverified request header or route parameter.

Query filters protect normal reads; they do not authorize writes or raw SQL.
Services must validate write permissions and organization ownership. Composite
foreign keys independently reject references to parents from another organization.
`IgnoreQueryFilters()` requires explicit account-level authorization and a bounded
query, for example when resolving memberships for an authenticated user.

Schema completion does not include Register/Login endpoints. Auth is the next flow.
