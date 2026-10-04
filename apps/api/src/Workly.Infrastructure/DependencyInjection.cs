using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Workly.Infrastructure.Persistence;

namespace Workly.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException(
                "Connection string 'DefaultConnection' was not found.");

        services.AddDbContext<WorklyDbContext>(options =>
            options
                .UseNpgsql(connectionString)
                .UseSnakeCaseNamingConvention());

        services
            .AddHealthChecks()
            .AddNpgSql(
                connectionString,
                name: "postgresql",
                tags: ["database", "ready"],
                timeout: TimeSpan.FromSeconds(5));

        return services;
    }
}
