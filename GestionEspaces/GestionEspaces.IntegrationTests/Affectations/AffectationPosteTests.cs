using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using GestionEspaces.IntegrationTests.Infrastructure;

namespace GestionEspaces.IntegrationTests.Affectations;

/// <summary>
/// Integration tests for AffectationPoste — assignment of agents to office desks.
/// Verifies business rules: one active assignment per agent, one per bureau.
/// </summary>
public sealed class AffectationPosteTests : IClassFixture<SqlServerFixture>
{
    private readonly SqlServerFixture _fixture;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public AffectationPosteTests(SqlServerFixture fixture)
    {
        _fixture = fixture;
    }

    // ── Helpers ────────────────────────────────────────────────────────────────

    private async Task<int> CreateSiteAsync(HttpClient client, string code)
    {
        var resp = await client.PostAsJsonAsync("/api/sites", new
        {
            nom = $"Site {code}",
            code,
            rue = "1 rue Test",
            ville = "Paris",
            codePostal = "75001",
            pays = "France",
            image = (string?)null
        });
        resp.EnsureSuccessStatusCode();
        var json = await resp.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        return json.GetProperty("idSite").GetInt32();
    }

    private async Task<int> CreateBatimentAsync(HttpClient client, int idSite, string nom)
    {
        var resp = await client.PostAsJsonAsync("/api/batiments", new
        {
            nom,
            nombreEtages = 2,
            superficie = 300f,
            image = (string?)null,
            idSite
        });
        resp.EnsureSuccessStatusCode();
        var json = await resp.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        return json.GetProperty("idBatiment").GetInt32();
    }

    private async Task<int> CreateBureauAsync(HttpClient client, int idBatiment, string numero)
    {
        var resp = await client.PostAsJsonAsync("/api/bureaux", new
        {
            numero,
            type = "Bureau individuel",
            capacite = 1,
            superficie = 12f,
            etage = 1,
            image = (string?)null,
            idBatiment,
            statut = 0
        });
        resp.EnsureSuccessStatusCode();
        var json = await resp.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        return json.GetProperty("idBureau").GetInt32();
    }

    private async Task<int> CreateAgentAsync(HttpClient client, string matricule)
    {
        var resp = await client.PostAsJsonAsync("/api/agents", new
        {
            nom = "TestAgent",
            prenom = "Affectation",
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

    // ── Happy path ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task AssignAgent_HappyPath_ThenClose_ThenReassign()
    {
        var client = _fixture.CreateClient();
        client.DefaultRequestHeaders.Add("Authorization", AuthHelper.BearerFor(AuthHelper.Roles.Administrateur));

        var idSite = await CreateSiteAsync(client, "AFP01");
        var idBatiment = await CreateBatimentAsync(client, idSite, "Bâtiment AFP");
        var idBureau = await CreateBureauAsync(client, idBatiment, "A-001");
        var idAgent = await CreateAgentAsync(client, "AFP-AGT-01");

        // Assignment endpoints are shared between Administrateur and Gestionnaire —
        // exercise them as Gestionnaire, which has no other referentiel rights.
        client.DefaultRequestHeaders.Remove("Authorization");
        client.DefaultRequestHeaders.Add("Authorization", AuthHelper.BearerFor(AuthHelper.Roles.Gestionnaire));

        // Assign agent to bureau — should succeed.
        var assignResponse = await client.PostAsJsonAsync($"/api/agents/{idAgent}/office-assignments", new
        {
            agentId = idAgent,
            bureauId = idBureau,
            dateAffectation = DateTime.UtcNow
        });
        Assert.Equal(HttpStatusCode.OK, assignResponse.StatusCode);
        var assignment = await assignResponse.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        var affId = assignment.GetProperty("assignmentId").GetInt32();

        // GET assignments for agent — should return one active record.
        var getResponse = await client.GetAsync($"/api/agents/{idAgent}/office-assignments");
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
        var list = await getResponse.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        Assert.True(list.GetArrayLength() >= 1);

        // Close the assignment.
        var closeRequest = new HttpRequestMessage(HttpMethod.Delete,
            $"/api/agents/{idAgent}/office-assignments/{affId}")
        {
            Content = JsonContent.Create(new { dateFin = DateTime.UtcNow })
        };
        var closeResponse = await client.SendAsync(closeRequest);
        Assert.Equal(HttpStatusCode.OK, closeResponse.StatusCode);

        // Re-assign after close — should succeed (no active conflict).
        var reassignResponse = await client.PostAsJsonAsync($"/api/agents/{idAgent}/office-assignments", new
        {
            agentId = idAgent,
            bureauId = idBureau,
            dateAffectation = DateTime.UtcNow
        });
        Assert.Equal(HttpStatusCode.OK, reassignResponse.StatusCode);
    }

    // ── Conflict: agent already assigned ──────────────────────────────────────

    [Fact]
    public async Task AssignAgent_WhenAlreadyActive_Returns409()
    {
        var client = _fixture.CreateClient();
        client.DefaultRequestHeaders.Add("Authorization", AuthHelper.BearerFor(AuthHelper.Roles.Administrateur));

        var idSite = await CreateSiteAsync(client, "AFP02");
        var idBatiment = await CreateBatimentAsync(client, idSite, "Bâtiment AFP2");
        var idBureau1 = await CreateBureauAsync(client, idBatiment, "B-001");
        var idBureau2 = await CreateBureauAsync(client, idBatiment, "B-002");
        var idAgent = await CreateAgentAsync(client, "AFP-AGT-02");

        client.DefaultRequestHeaders.Remove("Authorization");
        client.DefaultRequestHeaders.Add("Authorization", AuthHelper.BearerFor(AuthHelper.Roles.Gestionnaire));

        // First assignment — succeeds.
        var first = await client.PostAsJsonAsync($"/api/agents/{idAgent}/office-assignments", new
        {
            agentId = idAgent,
            bureauId = idBureau1,
            dateAffectation = DateTime.UtcNow
        });
        Assert.Equal(HttpStatusCode.OK, first.StatusCode);

        // Second assignment to different bureau — agent already has active → 409.
        var second = await client.PostAsJsonAsync($"/api/agents/{idAgent}/office-assignments", new
        {
            agentId = idAgent,
            bureauId = idBureau2,
            dateAffectation = DateTime.UtcNow
        });
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    // ── Conflict: bureau already assigned ────────────────────────────────────

    [Fact]
    public async Task AssignTwoAgentsToSameBureau_Returns409()
    {
        var client = _fixture.CreateClient();
        client.DefaultRequestHeaders.Add("Authorization", AuthHelper.BearerFor(AuthHelper.Roles.Administrateur));

        var idSite = await CreateSiteAsync(client, "AFP03");
        var idBatiment = await CreateBatimentAsync(client, idSite, "Bâtiment AFP3");
        var idBureau = await CreateBureauAsync(client, idBatiment, "C-001");
        var idAgent1 = await CreateAgentAsync(client, "AFP-AGT-03");
        var idAgent2 = await CreateAgentAsync(client, "AFP-AGT-04");

        client.DefaultRequestHeaders.Remove("Authorization");
        client.DefaultRequestHeaders.Add("Authorization", AuthHelper.BearerFor(AuthHelper.Roles.Gestionnaire));

        var first = await client.PostAsJsonAsync($"/api/agents/{idAgent1}/office-assignments", new
        {
            agentId = idAgent1,
            bureauId = idBureau,
            dateAffectation = DateTime.UtcNow
        });
        Assert.Equal(HttpStatusCode.OK, first.StatusCode);

        // Second agent to same bureau (capacity = 1, already occupied) → 409.
        var second = await client.PostAsJsonAsync($"/api/agents/{idAgent2}/office-assignments", new
        {
            agentId = idAgent2,
            bureauId = idBureau,
            dateAffectation = DateTime.UtcNow
        });
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }
}
