using GestionEspaces.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
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
    private readonly GestionEspacesDbContext _dbContext;

    public AuthController(IConfiguration configuration, GestionEspacesDbContext dbContext)
    {
        _configuration = configuration;
        _dbContext = dbContext;
    }

    // Brute-force mitigation: caps login attempts per client IP (see Program.cs "LoginPolicy").
    [HttpPost("login")]
    [EnableRateLimiting("LoginPolicy")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
            return BadRequest(new { detail = "L'adresse email et le mot de passe sont obligatoires." });

        // Look up user in configuration
        var users = _configuration.GetSection("Users").Get<UserConfig[]>() ?? [];
        var user = users.FirstOrDefault(u =>
            string.Equals(u.Email, request.Email, StringComparison.OrdinalIgnoreCase));

        if (user is null || !VerifyPassword(request.Password, user.PasswordHash))
            return Unauthorized(new { detail = "Email ou mot de passe incorrect." });

        var accessToken = CreateAccessToken(user.Email, user.Name, user.Role);
        var refreshToken = await IssueRefreshTokenAsync(user.Email, cancellationToken);

        return Ok(new
        {
            token = accessToken,
            refreshToken,
            email = user.Email,
            role = user.Role,
            name = user.Name
        });
    }

    // Trades a still-active refresh token for a new access token, rotating the refresh
    // token in the same call (the old one is revoked, a new one issued) so a stolen
    // refresh token can only ever be replayed once before rotation invalidates it.
    [HttpPost("refresh")]
    [EnableRateLimiting("LoginPolicy")]
    public async Task<IActionResult> Refresh([FromBody] RefreshRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.RefreshToken))
            return Unauthorized(new { detail = "Jeton de rafraîchissement manquant." });

        var tokenHash = HashToken(request.RefreshToken);
        var stored = await _dbContext.RefreshTokens
            .SingleOrDefaultAsync(t => t.TokenHash == tokenHash, cancellationToken);

        if (stored is null || !stored.IsActive)
            return Unauthorized(new { detail = "Jeton de rafraîchissement invalide ou expiré." });

        var users = _configuration.GetSection("Users").Get<UserConfig[]>() ?? [];
        var user = users.FirstOrDefault(u =>
            string.Equals(u.Email, stored.UserEmail, StringComparison.OrdinalIgnoreCase));

        if (user is null)
            return Unauthorized(new { detail = "Utilisateur introuvable." });

        stored.Revoke(DateTime.UtcNow);

        var accessToken = CreateAccessToken(user.Email, user.Name, user.Role);
        var newRefreshToken = await IssueRefreshTokenAsync(user.Email, cancellationToken);

        return Ok(new
        {
            token = accessToken,
            refreshToken = newRefreshToken,
            email = user.Email,
            role = user.Role,
            name = user.Name
        });
    }

    // Revokes a refresh token — the concrete, actionable half of "logout" for stateless
    // JWTs: the still-valid access token simply expires shortly on its own, but the
    // refresh token that would have kept the session alive is invalidated immediately.
    [HttpPost("logout")]
    public async Task<IActionResult> Logout([FromBody] RefreshRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.RefreshToken))
            return NoContent();

        var tokenHash = HashToken(request.RefreshToken);
        var stored = await _dbContext.RefreshTokens
            .SingleOrDefaultAsync(t => t.TokenHash == tokenHash, cancellationToken);

        if (stored is not null && stored.IsActive)
        {
            stored.Revoke(DateTime.UtcNow);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        return NoContent();
    }

    private string CreateAccessToken(string email, string name, string role)
    {
        var jwtSection = _configuration.GetSection("Jwt");
        var signingKey = jwtSection["SigningKey"]
            ?? throw new InvalidOperationException("Jwt:SigningKey is not configured.");
        var issuer = jwtSection["Issuer"] ?? "GestionEspaces";
        var audience = jwtSection["Audience"] ?? "GestionEspacesApi";
        var accessTokenMinutes = jwtSection.GetValue<int?>("AccessTokenMinutes") ?? 30;

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, email),
            new Claim(ClaimTypes.Name, name),
            new Claim(ClaimTypes.Role, role)
        };

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(accessTokenMinutes),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private async Task<string> IssueRefreshTokenAsync(string email, CancellationToken cancellationToken)
    {
        var refreshTokenDays = _configuration.GetSection("Jwt").GetValue<int?>("RefreshTokenDays") ?? 7;

        var rawToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
        var entry = new RefreshToken(HashToken(rawToken), email, DateTime.UtcNow, DateTime.UtcNow.AddDays(refreshTokenDays));

        _dbContext.RefreshTokens.Add(entry);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return rawToken;
    }

    // Refresh tokens are stored only as a hash — never in the clear — so a database
    // leak alone can't be replayed as a working session, mirroring the password hashing below.
    private static string HashToken(string rawToken) =>
        Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(rawToken)));

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
            var actual = Rfc2898DeriveBytes.Pbkdf2(password, salt, 10000, HashAlgorithmName.SHA256, 32);
            return CryptographicOperations.FixedTimeEquals(actual, expected);
        }
        catch
        {
            return false;
        }
    }
}

public sealed record LoginRequest(string Email, string Password);
public sealed record RefreshRequest(string RefreshToken);
public sealed record UserConfig(string Email, string PasswordHash, string Role, string Name);
