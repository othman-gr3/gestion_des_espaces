using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
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
        {
            return BadRequest(new { detail = "L'adresse email et le mot de passe sont obligatoires." });
        }

        // Understated, simple dev logic:
        // - if email contains "gestionnaire" or "admin", user gets "Gestionnaire" role.
        // - otherwise user gets "Lecteur" role.
        string role = request.Email.Contains("gestion") || request.Email.Contains("admin")
            ? "Gestionnaire"
            : "Lecteur";

        var jwtSection = _configuration.GetSection("Jwt");
        var signingKey = jwtSection["SigningKey"] ?? "Dev-GestionEspaces-SuperSecretKey-32Chars!!";
        var issuer = jwtSection["Issuer"] ?? "GestionEspaces";
        var audience = jwtSection["Audience"] ?? "GestionEspacesApi";

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, request.Email),
            new Claim(ClaimTypes.Name, request.Email.Split('@')[0]),
            new Claim(ClaimTypes.Role, role)
        };

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddDays(7), // Generous for local dev/testing
            signingCredentials: credentials);

        var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

        return Ok(new
        {
            token = tokenString,
            email = request.Email,
            role = role,
            name = request.Email.Split('@')[0]
        });
    }
}

public sealed record LoginRequest(string Email, string Password);
