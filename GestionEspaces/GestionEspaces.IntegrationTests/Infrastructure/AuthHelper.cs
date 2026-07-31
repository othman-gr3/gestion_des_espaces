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
        public const string Lecteur = "Lecteur";
        public const string Gestionnaire = "Gestionnaire";
    }

    /// <summary>
    /// Creates a Bearer authorization header value for the given role.
    /// </summary>
    public static string BearerFor(string role)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(TestSigningKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "test-user"),
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
