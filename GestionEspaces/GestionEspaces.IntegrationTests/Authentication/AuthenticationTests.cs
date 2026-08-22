using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using GestionEspaces.IntegrationTests.Infrastructure;

namespace GestionEspaces.IntegrationTests.Authentication;

/// <summary>
/// Integration tests for the real login/refresh/logout flow — exercised against the
/// actual configured users (appsettings.json), unlike most other tests which bypass
/// login entirely via <see cref="AuthHelper"/> to mint JWTs directly.
/// </summary>
public sealed class AuthenticationTests : IClassFixture<SqlServerFixture>
{
    private readonly SqlServerFixture _fixture;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public AuthenticationTests(SqlServerFixture fixture)
    {
        _fixture = fixture;
    }

    private static async Task<JsonElement> LoginAsync(HttpClient client, string email, string password)
    {
        var response = await client.PostAsJsonAsync("/api/auth/login", new { email, password });
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
    }

    [Fact]
    public async Task Login_WithValidCredentials_ReturnsAccessAndRefreshTokens()
    {
        var client = _fixture.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/login", new { email = "admin@onee.ma", password = "Admin123!" });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        Assert.False(string.IsNullOrWhiteSpace(body.GetProperty("token").GetString()));
        Assert.False(string.IsNullOrWhiteSpace(body.GetProperty("refreshToken").GetString()));
    }

    [Fact]
    public async Task Login_WithInvalidPassword_Returns401()
    {
        var client = _fixture.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/login", new { email = "admin@onee.ma", password = "WrongPassword!" });
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Refresh_WithValidToken_ReturnsNewPair_AndRevokesTheOldOne()
    {
        var client = _fixture.CreateClient();

        var login = await LoginAsync(client, "admin@onee.ma", "Admin123!");
        var originalRefreshToken = login.GetProperty("refreshToken").GetString();

        var refreshResponse = await client.PostAsJsonAsync("/api/auth/refresh", new { refreshToken = originalRefreshToken });
        Assert.Equal(HttpStatusCode.OK, refreshResponse.StatusCode);

        var refreshed = await refreshResponse.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        var newRefreshToken = refreshed.GetProperty("refreshToken").GetString();
        Assert.NotEqual(originalRefreshToken, newRefreshToken);

        // Rotation: the token that was just spent must not work a second time.
        var replay = await client.PostAsJsonAsync("/api/auth/refresh", new { refreshToken = originalRefreshToken });
        Assert.Equal(HttpStatusCode.Unauthorized, replay.StatusCode);
    }

    [Fact]
    public async Task Refresh_WithGarbageToken_Returns401()
    {
        var client = _fixture.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/refresh", new { refreshToken = "not-a-real-token" });
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Logout_RevokesRefreshToken_SoItCanNoLongerBeUsedToRefresh()
    {
        var client = _fixture.CreateClient();

        var login = await LoginAsync(client, "admin@onee.ma", "Admin123!");
        var refreshToken = login.GetProperty("refreshToken").GetString();

        var logoutResponse = await client.PostAsJsonAsync("/api/auth/logout", new { refreshToken });
        Assert.Equal(HttpStatusCode.NoContent, logoutResponse.StatusCode);

        var refreshAfterLogout = await client.PostAsJsonAsync("/api/auth/refresh", new { refreshToken });
        Assert.Equal(HttpStatusCode.Unauthorized, refreshAfterLogout.StatusCode);
    }
}
