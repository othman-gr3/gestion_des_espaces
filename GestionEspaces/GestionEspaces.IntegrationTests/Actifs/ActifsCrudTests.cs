using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using GestionEspaces.IntegrationTests.Infrastructure;

namespace GestionEspaces.IntegrationTests.Actifs;

/// <summary>
/// Integration tests for the Actifs CRUD endpoints — including the concurrency fix on UpdateAsync.
/// </summary>
public sealed class ActifsCrudTests : IClassFixture<SqlServerFixture>
{
    private readonly SqlServerFixture _fixture;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public ActifsCrudTests(SqlServerFixture fixture)
    {
        _fixture = fixture;
    }

    // ── Authorization ──────────────────────────────────────────────────────────

    [Fact]
    public async Task GetById_WithoutToken_Returns401()
    {
        var client = _fixture.CreateClient();
        var response = await client.GetAsync("/api/actifs/1");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Post_WithGestionnaireToken_Returns403()
    {
        var client = _fixture.CreateClient();
        client.DefaultRequestHeaders.Add("Authorization", AuthHelper.BearerFor(AuthHelper.Roles.Gestionnaire));

        var response = await client.PostAsJsonAsync("/api/actifs", new
        {
            nom = "PC Test",
            type = "Ordinateur",
            marque = "Dell",
            modele = "Latitude 5000",
            numeroSerie = (string?)null,
            dateAchat = (DateTime?)null,
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
        var createResponse = await client.PostAsJsonAsync("/api/actifs", new
        {
            nom = "Laptop Intégration",
            type = "Ordinateur",
            marque = "Lenovo",
            modele = "ThinkPad X1",
            numeroSerie = "SN-CRUD-001",
            dateAchat = new DateTime(2023, 6, 1),
            image = (string?)null
        });
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        var created = await createResponse.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        var idActif = created.GetProperty("idActif").GetInt32();
        var token = created.GetProperty("concurrencyToken").GetString();

        Assert.True(idActif > 0);
        Assert.NotNull(token);

        // 2. GET by id (Lecteur can read)
        client.DefaultRequestHeaders.Remove("Authorization");
        client.DefaultRequestHeaders.Add("Authorization", AuthHelper.BearerFor(AuthHelper.Roles.Administrateur));

        var getResponse = await client.GetAsync($"/api/actifs/{idActif}");
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
        var fetched = await getResponse.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        Assert.Equal("Laptop Intégration", fetched.GetProperty("nom").GetString());

        // 3. UPDATE (requires Gestionnaire + concurrency token)
        client.DefaultRequestHeaders.Remove("Authorization");
        client.DefaultRequestHeaders.Add("Authorization", AuthHelper.BearerFor(AuthHelper.Roles.Administrateur));

        var updateResponse = await client.PutAsJsonAsync($"/api/actifs/{idActif}", new
        {
            concurrencyToken = token,
            nom = "Laptop Intégration Mis À Jour",
            type = "Ordinateur",
            marque = "Lenovo",
            modele = "ThinkPad X1 Carbon",
            numeroSerie = "SN-CRUD-001",
            dateAchat = new DateTime(2023, 6, 1),
            image = (string?)null,
            etat = 1  // Bon
        });
        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);

        var updated = await updateResponse.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        var newToken = updated.GetProperty("concurrencyToken").GetString();
        Assert.NotNull(newToken);
        Assert.NotEqual(token, newToken);  // rowversion must have advanced

        // 4. DELETE
        var deleteResponse = await client.DeleteAsync($"/api/actifs/{idActif}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        // 5. Verify gone
        var getAfterDelete = await client.GetAsync($"/api/actifs/{idActif}");
        Assert.Equal(HttpStatusCode.NotFound, getAfterDelete.StatusCode);
    }

    // ── Concurrency conflict (validates the bug fix) ───────────────────────────

    [Fact]
    public async Task Update_WithStaleToken_Returns409()
    {
        var client = _fixture.CreateClient();
        client.DefaultRequestHeaders.Add("Authorization", AuthHelper.BearerFor(AuthHelper.Roles.Administrateur));

        var createResponse = await client.PostAsJsonAsync("/api/actifs", new
        {
            nom = "Actif Conflit",
            type = "Imprimante",
            marque = "HP",
            modele = "LaserJet",
            numeroSerie = "SN-CONF-001",
            dateAchat = (DateTime?)null,
            image = (string?)null
        });
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var created = await createResponse.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        var idActif = created.GetProperty("idActif").GetInt32();
        var token = created.GetProperty("concurrencyToken").GetString()!;

        // First update — succeeds.
        var firstUpdate = await client.PutAsJsonAsync($"/api/actifs/{idActif}", new
        {
            concurrencyToken = token,
            nom = "Actif Conflit V2",
            type = "Imprimante",
            marque = "HP",
            modele = "LaserJet Pro",
            numeroSerie = "SN-CONF-001",
            dateAchat = (DateTime?)null,
            image = (string?)null,
            etat = 1  // Bon
        });
        Assert.Equal(HttpStatusCode.OK, firstUpdate.StatusCode);

        // Second update with the OLD (stale) token → 409.
        var conflictResponse = await client.PutAsJsonAsync($"/api/actifs/{idActif}", new
        {
            concurrencyToken = token,   // stale
            nom = "Actif Conflit V3",
            type = "Imprimante",
            marque = "HP",
            modele = "LaserJet Pro",
            numeroSerie = "SN-CONF-001",
            dateAchat = (DateTime?)null,
            image = (string?)null,
            etat = 0  // Neuf
        });
        Assert.Equal(HttpStatusCode.Conflict, conflictResponse.StatusCode);
    }

    // ── Search / Pagination ────────────────────────────────────────────────────

    [Fact]
    public async Task Search_ReturnsPaged()
    {
        var client = _fixture.CreateClient();
        client.DefaultRequestHeaders.Add("Authorization", AuthHelper.BearerFor(AuthHelper.Roles.Administrateur));

        await client.PostAsJsonAsync("/api/actifs", new
        {
            nom = "Actif Recherche",
            type = "Téléphone",
            marque = "Apple",
            modele = "iPhone 15",
            numeroSerie = "SN-SRCH-001",
            dateAchat = (DateTime?)null,
            image = (string?)null
        });

        client.DefaultRequestHeaders.Remove("Authorization");
        client.DefaultRequestHeaders.Add("Authorization", AuthHelper.BearerFor(AuthHelper.Roles.Administrateur));

        var searchResponse = await client.GetAsync("/api/actifs?searchText=Actif%20Recherche&pageNumber=1&pageSize=10");
        Assert.Equal(HttpStatusCode.OK, searchResponse.StatusCode);

        var result = await searchResponse.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        Assert.True(result.GetProperty("totalCount").GetInt32() >= 1);
    }
}
