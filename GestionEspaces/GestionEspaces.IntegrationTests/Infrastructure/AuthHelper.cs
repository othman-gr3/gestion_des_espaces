using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace GestionEspaces.IntegrationTests.Infrastructure;

/// <summary>
/// Mints signed JWTs for integration tests using the same configuration
/// that <see cref="GestionEspacesWebApplicationFactory"/> injects.
/// </summary>
public static class AuthHelper
{
    public const string TestSigningKey = "IntegrationTest-SuperSecretKey-32Chars!!";
    public const string TestIssuer = "GestionEspaces";
    public const string TestAudience = "GestionEspacesApi";

    /// <summary>Roles accepted by the API.</summary>
    public static class Roles
    {
        public const string Administrateur = "Administrateur";
        public const string Gestionnaire = "Gestionnaire";
        public const string Agent = "Agent";
    }

    /// <summary>
    /// Creates a Bearer authorization header value for the given role, using a
    /// generic "test-user" identity claim.
    /// </summary>
    public static string BearerFor(string role) => BearerFor(role, "test-user");

    /// <summary>
    /// Creates a Bearer authorization header value for the given role, with the
    /// NameIdentifier claim set to <paramref name="email"/>. Used for Agent
    /// self-service tests, where the API resolves the agent by this claim.
    /// </summary>
    public static string BearerFor(string role, string email)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(TestSigningKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, email),
            new Claim(ClaimTypes.Name, "Test User"),
            new Claim(ClaimTypes.Role, role)
        };

        var token = new JwtSecurityToken(
            issuer: TestIssuer,
            audience: TestAudience,
            claims: claims,
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: credentials);

        return "Bearer " + new JwtSecurityTokenHandler().WriteToken(token);
    }
}
