using System.Security.Claims;
using GestionEspaces.Application.Interfaces;

namespace GestionEspaces.Api.Security;

/// <summary>
/// Reads the authenticated user's identity from the current HTTP request's JWT claims,
/// matching the claim types <see cref="Controllers.AuthController"/> issues at login.
/// </summary>
public sealed class HttpCurrentUserContext : ICurrentUserContext
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public HttpCurrentUserContext(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public string? Email => _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);

    public string? Role => _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.Role);
}
