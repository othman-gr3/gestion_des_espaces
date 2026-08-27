using GestionEspaces.Infrastructure.Persistence;
using GestionEspaces.Infrastructure.Security;
using Microsoft.AspNetCore.Authorization;
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

        var user = await _dbContext.AppUsers
            .SingleOrDefaultAsync(u => u.Email.ToLower() == request.Email.ToLower(), cancellationToken);

        if (user is null || !PasswordHasher.Verify(request.Password, user.PasswordHash))
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

    // Self-service password change — the only mutation a logged-in user can make to their
    // own account. Requires the current password (not just a valid access token) so a
    // stolen/leftover session on a shared machine can't lock the real owner out.
    [HttpPost("change-password")]
    [Authorize]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.CurrentPassword) || string.IsNullOrWhiteSpace(request.NewPassword))
            return BadRequest(new { detail = "Le mot de passe actuel et le nouveau mot de passe sont obligatoires." });

        if (request.NewPassword.Length < 8)
            return BadRequest(new { detail = "Le nouveau mot de passe doit contenir au moins 8 caractères." });

        var email = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(email))
            return Unauthorized();

        var user = await _dbContext.AppUsers
            .SingleOrDefaultAsync(u => u.Email.ToLower() == email.ToLower(), cancellationToken);

        if (user is null || !PasswordHasher.Verify(request.CurrentPassword, user.PasswordHash))
            return BadRequest(new { detail = "Le mot de passe actuel est incorrect." });

        user.ChangePassword(PasswordHasher.Hash(request.NewPassword));
        await _dbContext.SaveChangesAsync(cancellationToken);

        return NoContent();
    }

    // Current account's own data, including fields the login response doesn't carry
    // (e.g. Image) — the frontend calls this on the account page instead of relying on
    // whatever was cached in localStorage at login time.
    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> GetMeAsync(CancellationToken cancellationToken)
    {
        var email = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(email))
            return Unauthorized();

        var user = await _dbContext.AppUsers
            .SingleOrDefaultAsync(u => u.Email.ToLower() == email.ToLower(), cancellationToken);

        if (user is null)
            return Unauthorized();

        return Ok(new { user.Email, user.Name, user.Role, user.Image });
    }

    // Self-service photo update — same Image field already used on Agents/Sites/Bâtiments/
    // Bureaux/Actifs, just scoped to the caller's own account instead of an admin editing
    // someone else's record.
    [HttpPut("me/image")]
    [Authorize]
    public async Task<IActionResult> UpdateMyImageAsync([FromBody] UpdateImageRequest request, CancellationToken cancellationToken)
    {
        var email = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(email))
            return Unauthorized();

        var user = await _dbContext.AppUsers
            .SingleOrDefaultAsync(u => u.Email.ToLower() == email.ToLower(), cancellationToken);

        if (user is null)
            return Unauthorized();

        user.UpdateImage(string.IsNullOrWhiteSpace(request.Image) ? null : request.Image);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return Ok(new { user.Email, user.Name, user.Role, user.Image });
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

        var user = await _dbContext.AppUsers
            .SingleOrDefaultAsync(u => u.Email.ToLower() == stored.UserEmail.ToLower(), cancellationToken);

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

}

public sealed record LoginRequest(string Email, string Password);
public sealed record RefreshRequest(string RefreshToken);
public sealed record ChangePasswordRequest(string CurrentPassword, string NewPassword);
public sealed record UpdateImageRequest(string? Image);
