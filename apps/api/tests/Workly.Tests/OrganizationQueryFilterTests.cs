using Microsoft.EntityFrameworkCore;
using Workly.Infrastructure.Persistence;

namespace Workly.Tests;

public sealed class OrganizationQueryFilterTests
{
    [Fact]
    public void BusinessEntitiesHaveFiltersButAccountEntitiesDoNot()
    {
        using var context = CreateContext();
        foreach (var entity in context.Model.GetEntityTypes())
        {
            var isAccountEntity = entity.GetTableName() is "users" or "refresh_tokens";
            Assert.Equal(!isAccountEntity, entity.GetDeclaredQueryFilters().Any());
        }
    }

    [Fact]
    public void QueryUsesTheCurrentContextOrganizationEvenWhenModelIsCached()
    {
        using var first = CreateContext();
        using var second = CreateContext();
        first.CurrentOrganizationId = Guid.CreateVersion7();
        second.CurrentOrganizationId = Guid.CreateVersion7();
        var firstSql = first.Projects.ToQueryString();
        var secondSql = second.Projects.ToQueryString();
        Assert.Contains(first.CurrentOrganizationId.Value.ToString(), firstSql);
        Assert.Contains(second.CurrentOrganizationId.Value.ToString(), secondSql);
        Assert.DoesNotContain(first.CurrentOrganizationId.Value.ToString(), secondSql);
        Assert.Contains("WHERE", secondSql);
    }

    private static WorklyDbContext CreateContext() => new(
        new DbContextOptionsBuilder<WorklyDbContext>()
            .UseNpgsql("Host=localhost;Database=workly;Username=workly;Password=test")
            .UseSnakeCaseNamingConvention()
            .Options);
}
