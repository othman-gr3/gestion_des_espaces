using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace GestionEspaces.Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController : ControllerBase
{
    private readonly IConfiguration _configuration;

    public AuthController(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    [HttpPost("login")]
    public IActionResult Login([FromBody] LoginRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
            return BadRequest(new { detail = "L'adresse email et le mot de passe sont obligatoires." });

        // Look up user in configuration
        var users = _configuration.GetSection("Users").Get<UserConfig[]>() ?? [];
        var user = users.FirstOrDefault(u =>
            string.Equals(u.Email, request.Email, StringComparison.OrdinalIgnoreCase));

        if (user is null || !VerifyPassword(request.Password, user.PasswordHash))
            return Unauthorized(new { detail = "Email ou mot de passe incorrect." });

        var jwtSection = _configuration.GetSection("Jwt");
        var signingKey = jwtSection["SigningKey"]
            ?? throw new InvalidOperationException("Jwt:SigningKey is not configured.");
        var issuer = jwtSection["Issuer"] ?? "GestionEspaces";
        var audience = jwtSection["Audience"] ?? "GestionEspacesApi";

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Email),
            new Claim(ClaimTypes.Name, user.Name),
            new Claim(ClaimTypes.Role, user.Role)
        };

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddHours(8),
            signingCredentials: credentials);

        var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

        return Ok(new
        {
            token = tokenString,
            email = user.Email,
            role = user.Role,
            name = user.Name
        });
    }

    // PBKDF2-SHA256 with 16-byte salt, 10 000 iterations, 32-byte hash
    // Format stored in config: Base64( salt[16] || hash[32] )
    private static bool VerifyPassword(string password, string storedHash)
    {
        try
        {
            var hashBytes = Convert.FromBase64String(storedHash);
            if (hashBytes.Length != 48) return false;
            var salt = hashBytes[..16];
            var expected = hashBytes[16..];
            var actual = new Rfc2898DeriveBytes(password, salt, 10000, HashAlgorithmName.SHA256).GetBytes(32);
            return CryptographicOperations.FixedTimeEquals(actual, expected);
        }
        catch
        {
            return false;
        }
    }
}

public sealed record LoginRequest(string Email, string Password);
public sealed record UserConfig(string Email, string PasswordHash, string Role, string Name);
