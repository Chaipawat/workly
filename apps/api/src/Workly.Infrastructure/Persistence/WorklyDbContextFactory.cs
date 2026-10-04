using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Workly.Infrastructure.Persistence;

public sealed class WorklyDbContextFactory : IDesignTimeDbContextFactory<WorklyDbContext>
{
    public WorklyDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("WORKLY_DB_CONNECTION")
            ?? "Host=localhost;Port=5432;Database=workly;Username=workly;Password=workly_dev_password";

        var options = new DbContextOptionsBuilder<WorklyDbContext>()
            .UseNpgsql(connectionString)
            .UseSnakeCaseNamingConvention()
            .Options;

        return new WorklyDbContext(options);
    }
}
