using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using GestionEspaces.IntegrationTests.Infrastructure;

namespace GestionEspaces.IntegrationTests.Agents;

/// <summary>
/// Integration tests for the Agent self-service additions (history, profile edit, requests)
/// and the Administrateur/Gestionnaire side of the request workflow.
/// </summary>
public sealed class AgentSelfServiceAndDemandesTests : IClassFixture<SqlServerFixture>
{
    private readonly SqlServerFixture _fixture;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public AgentSelfServiceAndDemandesTests(SqlServerFixture fixture)
    {
        _fixture = fixture;
    }

    private async Task<(int IdAgent, string Email, string Token)> CreateAgentAsync(HttpClient client, string matricule)
    {
        var email = $"{matricule.ToLowerInvariant()}@selfservice.test";
        var resp = await client.PostAsJsonAsync("/api/agents", new
        {
            nom = "SelfService",
            prenom = "Agent",
            matricule,
            email,
            telephone = "0600000000",
            fonction = (string?)null,
            departement = (string?)null,
            dateEmbauche = (DateTime?)null,
            image = (string?)null
        });
        resp.EnsureSuccessStatusCode();
        var json = await resp.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        return (json.GetProperty("idAgent").GetInt32(), email, json.GetProperty("concurrencyToken").GetString()!);
    }

    [Fact]
    public async Task CreateAndListMyDemande_AsAgent_Succeeds()
    {
        var admin = _fixture.CreateClient();
        admin.DefaultRequestHeaders.Add("Authorization", AuthHelper.BearerFor(AuthHelper.Roles.Administrateur));
        var (_, email, _) = await CreateAgentAsync(admin, "SS-DEM-01");

        var client = _fixture.CreateClient();
        client.DefaultRequestHeaders.Add("Authorization", AuthHelper.BearerFor(AuthHelper.Roles.Agent, email));

        var createResponse = await client.PostAsJsonAsync("/api/agents/me/demandes", new
        {
            type = 0, // ChangementBureau
            description = "Je souhaite être réaffecté plus près de mon service."
        });
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        var listResponse = await client.GetAsync("/api/agents/me/demandes");
        Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);
        var list = await listResponse.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        Assert.Equal(1, list.GetArrayLength());
        Assert.Equal(0, list[0].GetProperty("statut").GetInt32()); // EnAttente
    }

    [Fact]
    public async Task CreateMyDemande_WithBlankDescription_ReturnsValidationError()
    {
        var admin = _fixture.CreateClient();
        admin.DefaultRequestHeaders.Add("Authorization", AuthHelper.BearerFor(AuthHelper.Roles.Administrateur));
        var (_, email, _) = await CreateAgentAsync(admin, "SS-DEM-02");

        var client = _fixture.CreateClient();
        client.DefaultRequestHeaders.Add("Authorization", AuthHelper.BearerFor(AuthHelper.Roles.Agent, email));

        var response = await client.PostAsJsonAsync("/api/agents/me/demandes", new { type = 0, description = "   " });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UpdateMyProfile_AsAgent_UpdatesTelephoneOnly()
    {
        var admin = _fixture.CreateClient();
        admin.DefaultRequestHeaders.Add("Authorization", AuthHelper.BearerFor(AuthHelper.Roles.Administrateur));
        var (idAgent, email, token) = await CreateAgentAsync(admin, "SS-PROF-01");

        var client = _fixture.CreateClient();
        client.DefaultRequestHeaders.Add("Authorization", AuthHelper.BearerFor(AuthHelper.Roles.Agent, email));

        var updateResponse = await client.PutAsJsonAsync("/api/agents/me/profile", new
        {
            concurrencyToken = token,
            telephone = "0611223344"
        });
        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);
        var updated = await updateResponse.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        Assert.Equal("0611223344", updated.GetProperty("telephone").GetString());

        // The rest of the record is untouched by the self-service edit.
        var getResponse = await admin.GetAsync($"/api/agents/{idAgent}");
        var fetched = await getResponse.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        Assert.Equal("SelfService", fetched.GetProperty("nom").GetString());
        Assert.Equal("0611223344", fetched.GetProperty("telephone").GetString());
    }

    [Fact]
    public async Task GetMyHistory_AsAgent_ReturnsEmptyListsWhenNoAssignments()
    {
        var admin = _fixture.CreateClient();
        admin.DefaultRequestHeaders.Add("Authorization", AuthHelper.BearerFor(AuthHelper.Roles.Administrateur));
        var (_, email, _) = await CreateAgentAsync(admin, "SS-HIST-01");

        var client = _fixture.CreateClient();
        client.DefaultRequestHeaders.Add("Authorization", AuthHelper.BearerFor(AuthHelper.Roles.Agent, email));

        var response = await client.GetAsync("/api/agents/me/history");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        Assert.Equal(0, body.GetProperty("postes").GetArrayLength());
        Assert.Equal(0, body.GetProperty("actifs").GetArrayLength());
    }

    [Fact]
    public async Task SearchDemandes_AsAdministrateur_ReturnsCreatedDemande()
    {
        var admin = _fixture.CreateClient();
        admin.DefaultRequestHeaders.Add("Authorization", AuthHelper.BearerFor(AuthHelper.Roles.Administrateur));
        var (_, email, _) = await CreateAgentAsync(admin, "SS-DEM-03");

        var agentClient = _fixture.CreateClient();
        agentClient.DefaultRequestHeaders.Add("Authorization", AuthHelper.BearerFor(AuthHelper.Roles.Agent, email));
        var createResponse = await agentClient.PostAsJsonAsync("/api/agents/me/demandes", new { type = 1, description = "Le climatiseur de mon bureau est en panne." });
        createResponse.EnsureSuccessStatusCode();

        var searchResponse = await admin.GetAsync("/api/demandes");
        Assert.Equal(HttpStatusCode.OK, searchResponse.StatusCode);
        var body = await searchResponse.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        Assert.True(body.GetProperty("totalCount").GetInt32() >= 1);
    }

    [Fact]
    public async Task ResoudreDemande_AsGestionnaire_TransitionsToResolue()
    {
        var admin = _fixture.CreateClient();
        admin.DefaultRequestHeaders.Add("Authorization", AuthHelper.BearerFor(AuthHelper.Roles.Administrateur));
        var (_, email, _) = await CreateAgentAsync(admin, "SS-DEM-04");

        var agentClient = _fixture.CreateClient();
        agentClient.DefaultRequestHeaders.Add("Authorization", AuthHelper.BearerFor(AuthHelper.Roles.Agent, email));
        var createResponse = await agentClient.PostAsJsonAsync("/api/agents/me/demandes", new { type = 2, description = "Mon écran ne s'allume plus." });
        var created = await createResponse.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        var idDemande = created.GetProperty("idDemande").GetInt32();
        var demandeToken = created.GetProperty("concurrencyToken").GetString();

        var gestionnaire = _fixture.CreateClient();
        gestionnaire.DefaultRequestHeaders.Add("Authorization", AuthHelper.BearerFor(AuthHelper.Roles.Gestionnaire));

        var resolveResponse = await gestionnaire.PostAsJsonAsync($"/api/demandes/{idDemande}/resoudre", new
        {
            concurrencyToken = demandeToken,
            reponse = "Écran remplacé le jour même."
        });
        Assert.Equal(HttpStatusCode.OK, resolveResponse.StatusCode);
        var resolved = await resolveResponse.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        Assert.Equal(2, resolved.GetProperty("statut").GetInt32()); // Resolue
    }

    [Fact]
    public async Task ResoudreDemande_WithoutReponse_ReturnsValidationError()
    {
        var admin = _fixture.CreateClient();
        admin.DefaultRequestHeaders.Add("Authorization", AuthHelper.BearerFor(AuthHelper.Roles.Administrateur));
        var (_, email, _) = await CreateAgentAsync(admin, "SS-DEM-05");

        var agentClient = _fixture.CreateClient();
        agentClient.DefaultRequestHeaders.Add("Authorization", AuthHelper.BearerFor(AuthHelper.Roles.Agent, email));
        var createResponse = await agentClient.PostAsJsonAsync("/api/agents/me/demandes", new { type = 3, description = "Autre demande." });
        var created = await createResponse.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        var idDemande = created.GetProperty("idDemande").GetInt32();
        var demandeToken = created.GetProperty("concurrencyToken").GetString();

        var gestionnaire = _fixture.CreateClient();
        gestionnaire.DefaultRequestHeaders.Add("Authorization", AuthHelper.BearerFor(AuthHelper.Roles.Gestionnaire));

        var response = await gestionnaire.PostAsJsonAsync($"/api/demandes/{idDemande}/resoudre", new { concurrencyToken = demandeToken, reponse = "" });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // ── RBAC ───────────────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateMyDemande_AsAdministrateur_Returns403()
    {
        var client = _fixture.CreateClient();
        client.DefaultRequestHeaders.Add("Authorization", AuthHelper.BearerFor(AuthHelper.Roles.Administrateur));

        var response = await client.PostAsJsonAsync("/api/agents/me/demandes", new { type = 0, description = "Test" });
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task SearchDemandes_AsAgent_Returns403()
    {
        var client = _fixture.CreateClient();
        client.DefaultRequestHeaders.Add("Authorization", AuthHelper.BearerFor(AuthHelper.Roles.Agent, "someone@selfservice.test"));

        var response = await client.GetAsync("/api/demandes");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
