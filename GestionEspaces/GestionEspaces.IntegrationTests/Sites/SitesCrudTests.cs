using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using GestionEspaces.IntegrationTests.Infrastructure;

namespace GestionEspaces.IntegrationTests.Sites;

/// <summary>
/// Integration tests for the Sites CRUD endpoints against a real SQL Server container.
/// Each test class gets its own container + migrated schema via <see cref="SqlServerFixture"/>.
/// </summary>
public sealed class SitesCrudTests : IClassFixture<SqlServerFixture>
{
    private readonly SqlServerFixture _fixture;

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public SitesCrudTests(SqlServerFixture fixture)
    {
        _fixture = fixture;
    }

    // ── Authorization ──────────────────────────────────────────────────────────

    [Fact]
    public async Task GetById_WithoutToken_Returns401()
    {
        var client = _fixture.CreateClient();

        var response = await client.GetAsync("/api/sites/1");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Post_WithGestionnaireToken_Returns403()
    {
        var client = _fixture.CreateClient();
        client.DefaultRequestHeaders.Add("Authorization", AuthHelper.BearerFor(AuthHelper.Roles.Gestionnaire));

        var response = await client.PostAsJsonAsync("/api/sites", new
        {
            nom = "Test Site",
            code = "FORB01",
            rue = "1 rue Test",
            ville = "Paris",
            codePostal = "75001",
            pays = "France",
            image = (string?)null
        });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    // ── Full CRUD round-trip ───────────────────────────────────────────────────

    [Fact]
    public async Task CreateGetUpdateDelete_HappyPath()
    {
        var client = _fixture.CreateClient();
        client.DefaultRequestHeaders.Add("Authorization", AuthHelper.BearerFor(AuthHelper.Roles.Administrateur));

        // 1. CREATE
        var createResponse = await client.PostAsJsonAsync("/api/sites", new
        {
            nom = "Site Intégration",
            code = "INT001",
            rue = "10 avenue de la Paix",
            ville = "Lyon",
            codePostal = "69001",
            pays = "France",
            image = (string?)null
        });
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        var created = await createResponse.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        var idSite = created.GetProperty("idSite").GetInt32();
        var token = created.GetProperty("concurrencyToken").GetString();

        Assert.True(idSite > 0);
        Assert.NotNull(token);

        // 2. GET by id (Administrateur can read)
        client.DefaultRequestHeaders.Remove("Authorization");
        client.DefaultRequestHeaders.Add("Authorization", AuthHelper.BearerFor(AuthHelper.Roles.Administrateur));

        var getResponse = await client.GetAsync($"/api/sites/{idSite}");
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
        var fetched = await getResponse.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        Assert.Equal("Site Intégration", fetched.GetProperty("nom").GetString());

        // 3. UPDATE (requires Administrateur)
        client.DefaultRequestHeaders.Remove("Authorization");
        client.DefaultRequestHeaders.Add("Authorization", AuthHelper.BearerFor(AuthHelper.Roles.Administrateur));

        var updateResponse = await client.PutAsJsonAsync($"/api/sites/{idSite}", new
        {
            concurrencyToken = token,
            nom = "Site Intégration Mis À Jour",
            code = "INT001",
            rue = "10 avenue de la Paix",
            ville = "Lyon",
            codePostal = "69001",
            pays = "France",
            image = (string?)null
        });
        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);

        var updated = await updateResponse.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        var newToken = updated.GetProperty("concurrencyToken").GetString();
        Assert.NotNull(newToken);
        // Rowversion must have changed after the update.
        Assert.NotEqual(token, newToken);

        // 4. DELETE
        var deleteResponse = await client.DeleteAsync($"/api/sites/{idSite}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        // 5. Verify gone
        var getAfterDelete = await client.GetAsync($"/api/sites/{idSite}");
        Assert.Equal(HttpStatusCode.NotFound, getAfterDelete.StatusCode);
    }

    // ── Concurrency conflict ───────────────────────────────────────────────────

    [Fact]
    public async Task Update_WithStaleToken_Returns409()
    {
        var client = _fixture.CreateClient();
        client.DefaultRequestHeaders.Add("Authorization", AuthHelper.BearerFor(AuthHelper.Roles.Administrateur));

        // Create the site.
        var createResponse = await client.PostAsJsonAsync("/api/sites", new
        {
            nom = "Site Conflit",
            code = "CONF01",
            rue = "5 rue Conflit",
            ville = "Marseille",
            codePostal = "13001",
            pays = "France",
            image = (string?)null
        });
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var created = await createResponse.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        var idSite = created.GetProperty("idSite").GetInt32();
        var token = created.GetProperty("concurrencyToken").GetString()!;

        // First update — succeeds and advances the rowversion.
        var firstUpdate = await client.PutAsJsonAsync($"/api/sites/{idSite}", new
        {
            concurrencyToken = token,
            nom = "Site Conflit V2",
            code = "CONF01",
            rue = "5 rue Conflit",
            ville = "Marseille",
            codePostal = "13001",
            pays = "France",
            image = (string?)null
        });
        Assert.Equal(HttpStatusCode.OK, firstUpdate.StatusCode);

        // Second update with the OLD (stale) token → 409 Conflict.
        var conflictResponse = await client.PutAsJsonAsync($"/api/sites/{idSite}", new
        {
            concurrencyToken = token,   // stale — rowversion has changed
            nom = "Site Conflit V3",
            code = "CONF01",
            rue = "5 rue Conflit",
            ville = "Marseille",
            codePostal = "13001",
            pays = "France",
            image = (string?)null
        });

        Assert.Equal(HttpStatusCode.Conflict, conflictResponse.StatusCode);
    }

    // ── Search ─────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Search_WithAdministrateurToken_ReturnsPaged()
    {
        var client = _fixture.CreateClient();
        client.DefaultRequestHeaders.Add("Authorization", AuthHelper.BearerFor(AuthHelper.Roles.Administrateur));

        // Seed a site.
        await client.PostAsJsonAsync("/api/sites", new
        {
            nom = "Site Recherche",
            code = "RCH001",
            rue = "1 allée Recherche",
            ville = "Bordeaux",
            codePostal = "33000",
            pays = "France",
            image = (string?)null
        });

        client.DefaultRequestHeaders.Remove("Authorization");
        client.DefaultRequestHeaders.Add("Authorization", AuthHelper.BearerFor(AuthHelper.Roles.Administrateur));

        var searchResponse = await client.GetAsync("/api/sites?searchText=Recherche&pageNumber=1&pageSize=10");
        Assert.Equal(HttpStatusCode.OK, searchResponse.StatusCode);

        var result = await searchResponse.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        Assert.True(result.GetProperty("totalCount").GetInt32() >= 1);
    }
}
