using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using GestionEspaces.IntegrationTests.Infrastructure;

namespace GestionEspaces.IntegrationTests.Authorization;

/// <summary>
/// Integration tests for the 3-role RBAC model (Administrateur / Gestionnaire / Agent):
/// referentiel access is Administrateur-only, affectations are shared with Gestionnaire,
/// and Agent self-service endpoints only ever expose the caller's own data.
/// </summary>
public sealed class RoleBasedAccessTests : IClassFixture<SqlServerFixture>
{
    private readonly SqlServerFixture _fixture;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public RoleBasedAccessTests(SqlServerFixture fixture)
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

    private async Task<int> CreateAgentAsync(HttpClient client, string matricule, string email)
    {
        var resp = await client.PostAsJsonAsync("/api/agents", new
        {
            nom = "RbacAgent",
            prenom = "Test",
            matricule,
            email,
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

    private async Task<int> CreateActifAsync(HttpClient client, string nom, string numeroSerie)
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

    // ── Agent role: forbidden on referentiel CRUD ───────────────────────────────

    [Fact]
    public async Task Agent_GetSites_Returns403()
    {
        var client = _fixture.CreateClient();
        client.DefaultRequestHeaders.Add("Authorization", AuthHelper.BearerFor(AuthHelper.Roles.Agent, "agent@rbac.test"));

        var response = await client.GetAsync("/api/sites");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Agent_PostAgents_Returns403()
    {
        var client = _fixture.CreateClient();
        client.DefaultRequestHeaders.Add("Authorization", AuthHelper.BearerFor(AuthHelper.Roles.Agent, "agent@rbac.test"));

        var response = await client.PostAsJsonAsync("/api/agents", new
        {
            nom = "Interdit",
            prenom = "Test",
            matricule = "RBAC-FORB-01",
            email = (string?)null,
            telephone = (string?)null,
            fonction = (string?)null,
            departement = (string?)null,
            dateEmbauche = (DateTime?)null,
            image = (string?)null
        });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    // ── Gestionnaire role: forbidden on Administrateur-only referentiel CRUD ────

    [Fact]
    public async Task Gestionnaire_PostSites_Returns403()
    {
        var client = _fixture.CreateClient();
        client.DefaultRequestHeaders.Add("Authorization", AuthHelper.BearerFor(AuthHelper.Roles.Gestionnaire));

        var response = await client.PostAsJsonAsync("/api/sites", new
        {
            nom = "Site Interdit",
            code = "RBAC-GST-01",
            rue = "1 rue Test",
            ville = "Paris",
            codePostal = "75001",
            pays = "France",
            image = (string?)null
        });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Gestionnaire_PutActifs_Returns403()
    {
        var client = _fixture.CreateClient();
        client.DefaultRequestHeaders.Add("Authorization", AuthHelper.BearerFor(AuthHelper.Roles.Administrateur));

        var idActif = await CreateActifAsync(client, "Actif RBAC PUT", "SN-RBAC-PUT-01");

        client.DefaultRequestHeaders.Remove("Authorization");
        client.DefaultRequestHeaders.Add("Authorization", AuthHelper.BearerFor(AuthHelper.Roles.Gestionnaire));

        var response = await client.PutAsJsonAsync($"/api/actifs/{idActif}", new
        {
            concurrencyToken = (string?)null,
            nom = "Modifié",
            type = "Ordinateur",
            marque = "Dell",
            modele = "XPS",
            numeroSerie = "SN-RBAC-PUT-01",
            dateAchat = (DateTime?)null,
            image = (string?)null,
            etat = 0
        });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    // ── Gestionnaire role: read-only referentiel access allowed (needed to search/select) ─

    [Fact]
    public async Task Gestionnaire_GetActifsAndBureaux_Returns200()
    {
        var client = _fixture.CreateClient();
        client.DefaultRequestHeaders.Add("Authorization", AuthHelper.BearerFor(AuthHelper.Roles.Gestionnaire));

        var actifsResponse = await client.GetAsync("/api/actifs");
        Assert.Equal(HttpStatusCode.OK, actifsResponse.StatusCode);

        var bureauxResponse = await client.GetAsync("/api/bureaux");
        Assert.Equal(HttpStatusCode.OK, bureauxResponse.StatusCode);

        var agentsResponse = await client.GetAsync("/api/agents");
        Assert.Equal(HttpStatusCode.OK, agentsResponse.StatusCode);
    }

    // ── Agent self-service: 200 OK, scoped to caller's own data ────────────────

    [Fact]
    public async Task Agent_MyOfficeAndAssets_ReturnsOnlyOwnData()
    {
        var client = _fixture.CreateClient();
        client.DefaultRequestHeaders.Add("Authorization", AuthHelper.BearerFor(AuthHelper.Roles.Administrateur));

        var idSite = await CreateSiteAsync(client, "RBAC01");
        var idBatiment = await CreateBatimentAsync(client, idSite, "Bâtiment RBAC");
        var idBureauA = await CreateBureauAsync(client, idBatiment, "RBAC-A");

        var emailA = "agent.a@rbac.test";
        var emailB = "agent.b@rbac.test";
        var idAgentA = await CreateAgentAsync(client, "RBAC-AGT-A", emailA);
        var idAgentB = await CreateAgentAsync(client, "RBAC-AGT-B", emailB);

        var idActif = await CreateActifAsync(client, "Laptop RBAC", "SN-RBAC-001");

        // Only agent A gets an office and an asset assigned.
        client.DefaultRequestHeaders.Remove("Authorization");
        client.DefaultRequestHeaders.Add("Authorization", AuthHelper.BearerFor(AuthHelper.Roles.Gestionnaire));

        var assignOffice = await client.PostAsJsonAsync($"/api/agents/{idAgentA}/office-assignments", new
        {
            agentId = idAgentA,
            bureauId = idBureauA,
            dateAffectation = DateTime.UtcNow
        });
        Assert.Equal(HttpStatusCode.OK, assignOffice.StatusCode);

        var assignAsset = await client.PostAsJsonAsync($"/api/agents/{idAgentA}/asset-assignments", new
        {
            agentId = idAgentA,
            actifId = idActif,
            dateAffectation = DateTime.UtcNow
        });
        Assert.Equal(HttpStatusCode.OK, assignAsset.StatusCode);

        // Agent A reads their own office and assets — 200 OK with the assigned data.
        client.DefaultRequestHeaders.Remove("Authorization");
        client.DefaultRequestHeaders.Add("Authorization", AuthHelper.BearerFor(AuthHelper.Roles.Agent, emailA));

        var myOfficeA = await client.GetAsync("/api/agents/me/office");
        Assert.Equal(HttpStatusCode.OK, myOfficeA.StatusCode);
        var officeA = await myOfficeA.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        Assert.Equal(idBureauA, officeA.GetProperty("idBureau").GetInt32());

        var myAssetsA = await client.GetAsync("/api/agents/me/assets");
        Assert.Equal(HttpStatusCode.OK, myAssetsA.StatusCode);
        var assetsA = await myAssetsA.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        Assert.Equal(1, assetsA.GetArrayLength());
        Assert.Equal(idActif, assetsA[0].GetProperty("idActif").GetInt32());

        // Agent B has no assignments — access is still allowed (no 403), but there is
        // no office to return (204 No Content), and never agent A's data.
        client.DefaultRequestHeaders.Remove("Authorization");
        client.DefaultRequestHeaders.Add("Authorization", AuthHelper.BearerFor(AuthHelper.Roles.Agent, emailB));

        var myOfficeB = await client.GetAsync("/api/agents/me/office");
        Assert.Equal(HttpStatusCode.NoContent, myOfficeB.StatusCode);

        var myAssetsB = await client.GetAsync("/api/agents/me/assets");
        Assert.Equal(HttpStatusCode.OK, myAssetsB.StatusCode);
        var assetsB = await myAssetsB.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        Assert.Equal(0, assetsB.GetArrayLength());
    }
}
