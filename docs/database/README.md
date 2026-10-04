# Database documentation

The reviewed schema is implemented in the Domain entities and EF Core configurations.
The initial PostgreSQL migration includes the keys, tenant-safe composite foreign keys,
constraints, and indexes documented in [erd.md](./erd.md).

Apply it from the repository root with:

```bash
docker compose up -d postgres
dotnet tool restore
dotnet ef database update --project apps/api/src/Workly.Infrastructure
```
