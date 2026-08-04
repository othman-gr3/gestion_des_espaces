using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using GestionEspaces.IntegrationTests.Infrastructure;

namespace GestionEspaces.IntegrationTests.Affectations;

/// <summary>
/// Integration tests for AffectationActif — assignment of assets to agents.
/// Verifies: one active assignment per actif, close + reassign workflow.
/// </summary>
public sealed class AffectationActifTests : IClassFixture<SqlServerFixture>
{
    private readonly SqlServerFixture _fixture;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public AffectationActifTests(SqlServerFixture fixture)
    {
        _fixture = fixture;
    }

    // ── Helpers ────────────────────────────────────────────────────────────────

    private async Task<int> CreateAgentAsync(HttpClient client, string matricule)
    {
        var resp = await client.PostAsJsonAsync("/api/agents", new
        {
            nom = "AgentActif",
            prenom = "Test",
            matricule,
            email = (string?)null,
            telephone = (string?)null,
            fonction = (string?)null,
            departement = (string?)null,
            dateEmbauche = (DateTime?)null,
            image = (string?)null
        });
        resp.EnsureSuccessStatusCode();
        var json = await resp.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        return json.GetProperty("idAgent").GetInt32();
    }

    private async Task<int> CreateActifAsync(HttpClient client, string nom, string? numeroSerie = null)
    {
        var resp = await client.PostAsJsonAsync("/api/actifs", new
        {
            nom,
            type = "Ordinateur",
            marque = "Dell",
            modele = "XPS",
            numeroSerie,
            dateAchat = (DateTime?)null,
            image = (string?)null
        });
        resp.EnsureSuccessStatusCode();
        var json = await resp.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        return json.GetProperty("idActif").GetInt32();
    }

    // ── Happy path ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task AssignActif_HappyPath_ThenClose_ThenReassign()
    {
        var client = _fixture.CreateClient();
        client.DefaultRequestHeaders.Add("Authorization", AuthHelper.BearerFor(AuthHelper.Roles.Gestionnaire));

        var idAgent = await CreateAgentAsync(client, "AFA-AGT-01");
        var idActif = await CreateActifAsync(client, "Laptop AFA Test", "SN-AFA-001");

        // Assign actif to agent — should succeed.
        var assignResponse = await client.PostAsJsonAsync($"/api/agents/{idAgent}/asset-assignments", new
        {
            agentId = idAgent,
            actifId = idActif,
            dateAffectation = DateTime.UtcNow
        });
        Assert.Equal(HttpStatusCode.OK, assignResponse.StatusCode);
        var assignment = await assignResponse.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        var affId = assignment.GetProperty("assignmentId").GetInt32();

        // GET asset assignments for agent.
        var getResponse = await client.GetAsync($"/api/agents/{idAgent}/asset-assignments");
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
        var list = await getResponse.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        Assert.True(list.GetArrayLength() >= 1);

        // Close the assignment.
        var closeRequest = new HttpRequestMessage(HttpMethod.Delete,
            $"/api/agents/{idAgent}/asset-assignments/{affId}")
        {
            Content = JsonContent.Create(new { dateFin = DateTime.UtcNow })
        };
        var closeResponse = await client.SendAsync(closeRequest);
        Assert.Equal(HttpStatusCode.OK, closeResponse.StatusCode);

        // Re-assign same actif to same agent — should succeed (no active conflict).
        var reassign = await client.PostAsJsonAsync($"/api/agents/{idAgent}/asset-assignments", new
        {
            agentId = idAgent,
            actifId = idActif,
            dateAffectation = DateTime.UtcNow
        });
        Assert.Equal(HttpStatusCode.OK, reassign.StatusCode);
    }

    // ── Conflict: actif already assigned ────────────────────────────────────

    [Fact]
    public async Task AssignActif_WhenAlreadyAssigned_Returns409()
    {
        var client = _fixture.CreateClient();
        client.DefaultRequestHeaders.Add("Authorization", AuthHelper.BearerFor(AuthHelper.Roles.Gestionnaire));

        var idAgent1 = await CreateAgentAsync(client, "AFA-AGT-02");
        var idAgent2 = await CreateAgentAsync(client, "AFA-AGT-03");
        var idActif = await CreateActifAsync(client, "Actif Conflit Actif", "SN-AFA-002");

        // Assign to first agent — succeeds.
        var first = await client.PostAsJsonAsync($"/api/agents/{idAgent1}/asset-assignments", new
        {
            agentId = idAgent1,
            actifId = idActif,
            dateAffectation = DateTime.UtcNow
        });
        Assert.Equal(HttpStatusCode.OK, first.StatusCode);

        // Try to assign same actif to second agent — actif already active → 409.
        var second = await client.PostAsJsonAsync($"/api/agents/{idAgent2}/asset-assignments", new
        {
            agentId = idAgent2,
            actifId = idActif,
            dateAffectation = DateTime.UtcNow
        });
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }
}
