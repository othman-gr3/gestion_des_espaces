using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using GestionEspaces.IntegrationTests.Infrastructure;

namespace GestionEspaces.IntegrationTests.Bureaux;

/// <summary>
/// Integration tests for the AI-assisted office search endpoint. The test host has no
/// real OpenRouter API key configured, so these exercise the graceful-degradation path
/// (falls back to keyword search) — the exact same path a production deployment takes
/// if the key is ever missing or OpenRouter is unreachable.
/// </summary>
public sealed class BureauAiSearchTests : IClassFixture<SqlServerFixture>
{
    private readonly SqlServerFixture _fixture;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public BureauAiSearchTests(SqlServerFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task AiSearch_WithoutConfiguredApiKey_DegradesToKeywordSearch()
    {
        var client = _fixture.CreateClient();
        client.DefaultRequestHeaders.Add("Authorization", AuthHelper.BearerFor(AuthHelper.Roles.Administrateur));

        var response = await client.PostAsJsonAsync("/api/bureaux/ai-search", new { query = "un bureau disponible" });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        Assert.False(body.GetProperty("usedAi").GetBoolean());
        Assert.True(body.TryGetProperty("results", out _));
    }

    [Fact]
    public async Task AiSearch_WithBlankQuery_ReturnsValidationError()
    {
        var client = _fixture.CreateClient();
        client.DefaultRequestHeaders.Add("Authorization", AuthHelper.BearerFor(AuthHelper.Roles.Gestionnaire));

        var response = await client.PostAsJsonAsync("/api/bureaux/ai-search", new { query = "   " });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task AiSearch_WithAgentToken_Returns403()
    {
        var client = _fixture.CreateClient();
        client.DefaultRequestHeaders.Add("Authorization", AuthHelper.BearerFor(AuthHelper.Roles.Agent));

        var response = await client.PostAsJsonAsync("/api/bureaux/ai-search", new { query = "un bureau disponible" });
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task AiSearch_WithoutToken_Returns401()
    {
        var client = _fixture.CreateClient();

        var response = await client.PostAsJsonAsync("/api/bureaux/ai-search", new { query = "un bureau disponible" });
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
