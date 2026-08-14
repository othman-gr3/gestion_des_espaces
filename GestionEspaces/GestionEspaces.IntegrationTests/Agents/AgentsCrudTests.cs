using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using GestionEspaces.IntegrationTests.Infrastructure;

namespace GestionEspaces.IntegrationTests.Agents;

/// <summary>
/// Integration tests for the Agents CRUD endpoints against a real SQL Server container.
/// </summary>
public sealed class AgentsCrudTests : IClassFixture<SqlServerFixture>
{
    private readonly SqlServerFixture _fixture;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public AgentsCrudTests(SqlServerFixture fixture)
    {
        _fixture = fixture;
    }

    // ── Authorization ──────────────────────────────────────────────────────────

    [Fact]
    public async Task GetById_WithoutToken_Returns401()
    {
        var client = _fixture.CreateClient();
        var response = await client.GetAsync("/api/agents/1");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Post_WithGestionnaireToken_Returns403()
    {
        var client = _fixture.CreateClient();
        client.DefaultRequestHeaders.Add("Authorization", AuthHelper.BearerFor(AuthHelper.Roles.Gestionnaire));

        var response = await client.PostAsJsonAsync("/api/agents", new
        {
            nom = "Dupont",
            prenom = "Jean",
            matricule = "FORB001",
            email = (string?)null,
            telephone = (string?)null,
            fonction = (string?)null,
            departement = (string?)null,
            dateEmbauche = (DateTime?)null,
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
        var createResponse = await client.PostAsJsonAsync("/api/agents", new
        {
            nom = "Martin",
            prenom = "Sophie",
            matricule = "AGT-CRUD-01",
            email = "sophie.martin@test.fr",
            telephone = (string?)null,
            fonction = "Ingénieure",
            departement = "IT",
            dateEmbauche = new DateTime(2022, 3, 1),
            image = (string?)null
        });
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        var created = await createResponse.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        var idAgent = created.GetProperty("idAgent").GetInt32();
        var token = created.GetProperty("concurrencyToken").GetString();

        Assert.True(idAgent > 0);
        Assert.NotNull(token);

        // 2. GET by id (Lecteur can read)
        client.DefaultRequestHeaders.Remove("Authorization");
        client.DefaultRequestHeaders.Add("Authorization", AuthHelper.BearerFor(AuthHelper.Roles.Administrateur));

        var getResponse = await client.GetAsync($"/api/agents/{idAgent}");
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
        var fetched = await getResponse.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        Assert.Equal("Martin", fetched.GetProperty("nom").GetString());
        Assert.Equal("Sophie", fetched.GetProperty("prenom").GetString());

        // 3. UPDATE (requires Gestionnaire)
        client.DefaultRequestHeaders.Remove("Authorization");
        client.DefaultRequestHeaders.Add("Authorization", AuthHelper.BearerFor(AuthHelper.Roles.Administrateur));

        var updateResponse = await client.PutAsJsonAsync($"/api/agents/{idAgent}", new
        {
            concurrencyToken = token,
            nom = "Martin",
            prenom = "Sophie Updated",
            matricule = "AGT-CRUD-01",
            email = "sophie.martin@test.fr",
            telephone = "0612345678",
            fonction = "Lead Ingénieure",
            departement = "IT",
            dateEmbauche = new DateTime(2022, 3, 1),
            image = (string?)null
        });
        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);

        var updated = await updateResponse.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        var newToken = updated.GetProperty("concurrencyToken").GetString();
        Assert.NotNull(newToken);
        Assert.NotEqual(token, newToken);  // rowversion must have advanced
        Assert.Equal("Sophie Updated", updated.GetProperty("prenom").GetString());

        // 4. DELETE
        var deleteResponse = await client.DeleteAsync($"/api/agents/{idAgent}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        // 5. Verify gone
        var getAfterDelete = await client.GetAsync($"/api/agents/{idAgent}");
        Assert.Equal(HttpStatusCode.NotFound, getAfterDelete.StatusCode);
    }

    // ── Concurrency conflict ───────────────────────────────────────────────────

    [Fact]
    public async Task Update_WithStaleToken_Returns409()
    {
        var client = _fixture.CreateClient();
        client.DefaultRequestHeaders.Add("Authorization", AuthHelper.BearerFor(AuthHelper.Roles.Administrateur));

        var createResponse = await client.PostAsJsonAsync("/api/agents", new
        {
            nom = "Conflit",
            prenom = "Test",
            matricule = "AGT-CONF-01",
            email = (string?)null,
            telephone = (string?)null,
            fonction = (string?)null,
            departement = (string?)null,
            dateEmbauche = (DateTime?)null,
            image = (string?)null
        });
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var created = await createResponse.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        var idAgent = created.GetProperty("idAgent").GetInt32();
        var token = created.GetProperty("concurrencyToken").GetString()!;

        // First update — succeeds and advances the rowversion.
        var firstUpdate = await client.PutAsJsonAsync($"/api/agents/{idAgent}", new
        {
            concurrencyToken = token,
            nom = "Conflit",
            prenom = "Test V2",
            matricule = "AGT-CONF-01",
            email = (string?)null,
            telephone = (string?)null,
            fonction = (string?)null,
            departement = (string?)null,
            dateEmbauche = (DateTime?)null,
            image = (string?)null
        });
        Assert.Equal(HttpStatusCode.OK, firstUpdate.StatusCode);

        // Second update with the OLD (stale) token → 409 Conflict.
        var conflictResponse = await client.PutAsJsonAsync($"/api/agents/{idAgent}", new
        {
            concurrencyToken = token,   // stale
            nom = "Conflit",
            prenom = "Test V3",
            matricule = "AGT-CONF-01",
            email = (string?)null,
            telephone = (string?)null,
            fonction = (string?)null,
            departement = (string?)null,
            dateEmbauche = (DateTime?)null,
            image = (string?)null
        });
        Assert.Equal(HttpStatusCode.Conflict, conflictResponse.StatusCode);
    }

    // ── Duplicate matricule ────────────────────────────────────────────────────

    [Fact]
    public async Task Create_WithDuplicateMatricule_Returns409()
    {
        var client = _fixture.CreateClient();
        client.DefaultRequestHeaders.Add("Authorization", AuthHelper.BearerFor(AuthHelper.Roles.Administrateur));

        var payload = new
        {
            nom = "Durand",
            prenom = "Paul",
            matricule = "AGT-DUP-01",
            email = (string?)null,
            telephone = (string?)null,
            fonction = (string?)null,
            departement = (string?)null,
            dateEmbauche = (DateTime?)null,
            image = (string?)null
        };

        var first = await client.PostAsJsonAsync("/api/agents", payload);
        Assert.Equal(HttpStatusCode.Created, first.StatusCode);

        var duplicate = await client.PostAsJsonAsync("/api/agents", payload);
        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);
    }

    // ── Search / Pagination ────────────────────────────────────────────────────

    [Fact]
    public async Task Search_ReturnsPaged()
    {
        var client = _fixture.CreateClient();
        client.DefaultRequestHeaders.Add("Authorization", AuthHelper.BearerFor(AuthHelper.Roles.Administrateur));

        await client.PostAsJsonAsync("/api/agents", new
        {
            nom = "Recherche",
            prenom = "Agent",
            matricule = "AGT-SRCH-01",
            email = (string?)null,
            telephone = (string?)null,
            fonction = (string?)null,
            departement = (string?)null,
            dateEmbauche = (DateTime?)null,
            image = (string?)null
        });

        client.DefaultRequestHeaders.Remove("Authorization");
        client.DefaultRequestHeaders.Add("Authorization", AuthHelper.BearerFor(AuthHelper.Roles.Administrateur));

        var searchResponse = await client.GetAsync("/api/agents?searchText=Recherche&pageNumber=1&pageSize=10");
        Assert.Equal(HttpStatusCode.OK, searchResponse.StatusCode);

        var result = await searchResponse.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        Assert.True(result.GetProperty("totalCount").GetInt32() >= 1);
    }
}
