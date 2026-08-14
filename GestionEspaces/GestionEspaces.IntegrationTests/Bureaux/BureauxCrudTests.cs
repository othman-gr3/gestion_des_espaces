using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using GestionEspaces.IntegrationTests.Infrastructure;

namespace GestionEspaces.IntegrationTests.Bureaux;

/// <summary>
/// Integration tests for the Bureaux CRUD endpoints.
/// Depends on Sites and Batiments being creatable, so exercises both resources.
/// </summary>
public sealed class BureauxCrudTests : IClassFixture<SqlServerFixture>
{
    private readonly SqlServerFixture _fixture;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public BureauxCrudTests(SqlServerFixture fixture)
    {
        _fixture = fixture;
    }

    // ── Helpers ────────────────────────────────────────────────────────────────

    private async Task<int> CreateSiteAsync(HttpClient client, string code)
    {
        var response = await client.PostAsJsonAsync("/api/sites", new
        {
            nom = $"Site {code}",
            code,
            rue = "1 rue Test",
            ville = "Paris",
            codePostal = "75001",
            pays = "France",
            image = (string?)null
        });
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        return json.GetProperty("idSite").GetInt32();
    }

    private async Task<(int Id, string Token)> CreateBatimentAsync(HttpClient client, int idSite, string nom)
    {
        var response = await client.PostAsJsonAsync("/api/batiments", new
        {
            nom,
            nombreEtages = 3,
            superficie = 500f,
            image = (string?)null,
            idSite
        });
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        return (json.GetProperty("idBatiment").GetInt32(),
                json.GetProperty("concurrencyToken").GetString()!);
    }

    private async Task<(int Id, string Token)> CreateBureauAsync(HttpClient client, int idBatiment, string numero)
    {
        var response = await client.PostAsJsonAsync("/api/bureaux", new
        {
            numero,
            type = "Open-space",
            capacite = 6,
            superficie = 30f,
            etage = 1,
            image = (string?)null,
            idBatiment,
            statut = 0   // Disponible
        });
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        return (json.GetProperty("idBureau").GetInt32(),
                json.GetProperty("concurrencyToken").GetString()!);
    }

    // ── Authorization ──────────────────────────────────────────────────────────

    [Fact]
    public async Task GetBureau_WithoutToken_Returns401()
    {
        var client = _fixture.CreateClient();
        var response = await client.GetAsync("/api/bureaux/1");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task CreateBureau_WithGestionnaireToken_Returns403()
    {
        var client = _fixture.CreateClient();
        client.DefaultRequestHeaders.Add("Authorization", AuthHelper.BearerFor(AuthHelper.Roles.Gestionnaire));

        var response = await client.PostAsJsonAsync("/api/bureaux", new
        {
            numero = "B-001",
            type = "Bureau",
            capacite = 2,
            superficie = 15f,
            etage = 1,
            image = (string?)null,
            idBatiment = 1,
            statut = 0
        });
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    // ── Full CRUD round-trip ───────────────────────────────────────────────────

    [Fact]
    public async Task CreateGetUpdateDelete_Bureau_HappyPath()
    {
        var client = _fixture.CreateClient();
        client.DefaultRequestHeaders.Add("Authorization", AuthHelper.BearerFor(AuthHelper.Roles.Administrateur));

        var idSite = await CreateSiteAsync(client, "BRX01");
        var (idBatiment, _) = await CreateBatimentAsync(client, idSite, "Bâtiment A");
        var (idBureau, token) = await CreateBureauAsync(client, idBatiment, "B-201");

        Assert.True(idBureau > 0);
        Assert.NotNull(token);

        // GET
        client.DefaultRequestHeaders.Remove("Authorization");
        client.DefaultRequestHeaders.Add("Authorization", AuthHelper.BearerFor(AuthHelper.Roles.Administrateur));

        var getResponse = await client.GetAsync($"/api/bureaux/{idBureau}");
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
        var fetched = await getResponse.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        Assert.Equal("B-201", fetched.GetProperty("numero").GetString());

        // UPDATE — set to EnMaintenance
        client.DefaultRequestHeaders.Remove("Authorization");
        client.DefaultRequestHeaders.Add("Authorization", AuthHelper.BearerFor(AuthHelper.Roles.Administrateur));

        var updateResponse = await client.PutAsJsonAsync($"/api/bureaux/{idBureau}", new
        {
            concurrencyToken = token,
            numero = "B-201",
            type = "Open-space",
            capacite = 8,
            superficie = 30f,
            etage = 1,
            image = (string?)null,
            idBatiment,
            statut = 1   // EnMaintenance
        });
        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);
        var updated = await updateResponse.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        Assert.Equal(1, updated.GetProperty("statut").GetInt32()); // EnMaintenance

        var newToken = updated.GetProperty("concurrencyToken").GetString();
        Assert.NotNull(newToken);
        Assert.NotEqual(token, newToken);

        // DELETE
        var deleteResponse = await client.DeleteAsync($"/api/bureaux/{idBureau}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);
    }

    // ── Concurrency conflict ───────────────────────────────────────────────────

    [Fact]
    public async Task Update_Bureau_WithStaleToken_Returns409()
    {
        var client = _fixture.CreateClient();
        client.DefaultRequestHeaders.Add("Authorization", AuthHelper.BearerFor(AuthHelper.Roles.Administrateur));

        var idSite = await CreateSiteAsync(client, "BRX02");
        var (idBatiment, _) = await CreateBatimentAsync(client, idSite, "Bâtiment B");
        var (idBureau, token) = await CreateBureauAsync(client, idBatiment, "B-301");

        // First update — advances rowversion.
        var firstUpdate = await client.PutAsJsonAsync($"/api/bureaux/{idBureau}", new
        {
            concurrencyToken = token,
            numero = "B-301",
            type = "Open-space",
            capacite = 6,
            superficie = 30f,
            etage = 1,
            image = (string?)null,
            idBatiment,
            statut = 2   // HorsService
        });
        Assert.Equal(HttpStatusCode.OK, firstUpdate.StatusCode);

        // Second update with stale token → 409.
        var conflictResponse = await client.PutAsJsonAsync($"/api/bureaux/{idBureau}", new
        {
            concurrencyToken = token,   // stale
            numero = "B-301",
            type = "Open-space",
            capacite = 6,
            superficie = 30f,
            etage = 1,
            image = (string?)null,
            idBatiment,
            statut = 0
        });
        Assert.Equal(HttpStatusCode.Conflict, conflictResponse.StatusCode);
    }
}
