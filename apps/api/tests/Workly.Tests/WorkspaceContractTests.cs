using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using Workly.Application.Workspace;

namespace Workly.Tests;

public sealed class WorkspaceContractTests
{
    [Theory]
    [InlineData("acme-studio", true)]
    [InlineData("team-42", true)]
    [InlineData("Acme Studio", false)]
    [InlineData("-team", false)]
    [InlineData("team_42", false)]
    public void OrganizationSlug_UsesUrlSafeFormat(string slug, bool expectedValid)
    {
        var request = new CreateOrganizationRequest { Name = "Acme Studio", Slug = slug };
        var results = new List<ValidationResult>();

        var valid = Validator.TryValidateObject(request, new ValidationContext(request), results, true);

        Assert.Equal(expectedValid, valid);
    }

    [Fact]
    public void DashboardTaskStatus_MatchesFrontendWireContract()
    {
        var json = JsonSerializer.Serialize(new TaskStatusResponse(2, 3, 4),
            new JsonSerializerOptions(JsonSerializerDefaults.Web));

        Assert.Contains("\"todo\":2", json);
        Assert.Contains("\"in-progress\":3", json);
        Assert.Contains("\"done\":4", json);
    }

    [Theory]
    [InlineData("New workspace name", true)]
    [InlineData("A", false)]
    [InlineData("", false)]
    public void WorkspaceName_HasUsefulLength(string name, bool expectedValid)
    {
        var request = new UpdateOrganizationRequest { Name = name, Slug = "workspace-name" };
        var results = new List<ValidationResult>();

        var valid = Validator.TryValidateObject(request, new ValidationContext(request), results, true);

        Assert.Equal(expectedValid, valid);
    }
}
