using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Workly.Infrastructure;
using Workly.Infrastructure.Persistence;

namespace Workly.Tests;

public sealed class DependencyInjectionTests
{
    [Fact]
    public void AddInfrastructure_WhenConnectionStringIsMissing_Throws()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder().Build();

        var exception = Assert.Throws<InvalidOperationException>(() =>
        {
            services.AddInfrastructure(configuration);
        });

        Assert.Equal(
            "Connection string 'DefaultConnection' was not found.",
            exception.Message);
    }

    [Fact]
    public void AddInfrastructure_WhenConfigured_RegistersDbContext()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] =
                    "Host=localhost;Database=workly;Username=workly;Password=test"
            })
            .Build();

        services.AddInfrastructure(configuration);

        Assert.Contains(
            services,
            descriptor => descriptor.ServiceType == typeof(WorklyDbContext));
    }

    [Fact]
    public void WorklyDbContext_ModelContainsTheCompleteInitialSchema()
    {
        var options = new DbContextOptionsBuilder<WorklyDbContext>()
            .UseNpgsql("Host=localhost;Database=workly;Username=workly;Password=test")
            .UseSnakeCaseNamingConvention()
            .Options;

        using var context = new WorklyDbContext(options);
        var tableNames = context.Model.GetEntityTypes()
            .Select(entity => entity.GetTableName())
            .OfType<string>()
            .ToHashSet(StringComparer.Ordinal);

        string[] expectedTables =
        [
            "users",
            "organizations",
            "organization_members",
            "departments",
            "projects",
            "project_members",
            "tasks",
            "task_comments",
            "leave_requests",
            "announcements",
            "invitations",
            "activity_logs",
            "refresh_tokens"
        ];

        Assert.Subset(expectedTables.ToHashSet(StringComparer.Ordinal), tableNames);
    }
}
