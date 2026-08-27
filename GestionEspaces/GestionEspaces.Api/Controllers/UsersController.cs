using GestionEspaces.Infrastructure.Persistence;
using GestionEspaces.Infrastructure.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GestionEspaces.Api.Controllers;

/// <summary>
/// Admin-only management of login accounts: list existing accounts, create new ones,
/// and swap a Gestionnaire/Agent account between those two roles. Deliberately never
/// grants or revokes Administrateur through this endpoint — that stays config/seed-only
/// so an Admin can't accidentally lock themselves out or hand out superuser rights.
/// </summary>
[ApiController]
[Route("api/users")]
[Authorize(Policy = "ReferentielAdmin")]
public sealed class UsersController : ControllerBase
{
    private static readonly string[] AssignableRoles = ["Gestionnaire", "Agent"];

    private readonly GestionEspacesDbContext _dbContext;

    public UsersController(GestionEspacesDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpGet]
    public async Task<IActionResult> ListAsync(CancellationToken cancellationToken)
    {
        var users = await _dbContext.AppUsers
            .OrderBy(u => u.Role).ThenBy(u => u.Name)
            .Select(u => new { u.IdAppUser, u.Email, u.Name, u.Role, u.Image })
            .ToListAsync(cancellationToken);

        return Ok(users);
    }

    [HttpPost]
    public async Task<IActionResult> CreateAsync([FromBody] CreateUserRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Name) || string.IsNullOrWhiteSpace(request.Password))
            return BadRequest(new { detail = "L'email, le nom et le mot de passe sont obligatoires." });

        if (!AssignableRoles.Contains(request.Role))
            return BadRequest(new { detail = "Le rôle doit être Gestionnaire ou Agent." });

        if (request.Password.Length < 8)
            return BadRequest(new { detail = "Le mot de passe doit contenir au moins 8 caractères." });

        var emailTaken = await _dbContext.AppUsers
            .AnyAsync(u => u.Email.ToLower() == request.Email.ToLower(), cancellationToken);
        if (emailTaken)
            return BadRequest(new { detail = "Un compte existe déjà avec cet email." });

        // An Agent-role account must match an existing Agent record by email — that's
        // exactly how AgentSelfServiceUseCase resolves "me" from the JWT claim, so a
        // mismatched email would silently break every self-service endpoint.
        if (request.Role == "Agent")
        {
            var agentExists = await _dbContext.Agents
                .AnyAsync(a => a.Email != null && a.Email.ToLower() == request.Email.ToLower(), cancellationToken);
            if (!agentExists)
                return BadRequest(new { detail = "Aucune fiche agent ne correspond à cet email — un compte Agent doit correspondre à une fiche agent existante." });
        }

        var user = new AppUser(request.Email, PasswordHasher.Hash(request.Password), request.Role, request.Name);
        _dbContext.AppUsers.Add(user);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return Ok(new { user.IdAppUser, user.Email, user.Name, user.Role });
    }

    [HttpPut("{idAppUser:int}/role")]
    public async Task<IActionResult> ChangeRoleAsync(int idAppUser, [FromBody] ChangeRoleRequest request, CancellationToken cancellationToken)
    {
        if (!AssignableRoles.Contains(request.Role))
            return BadRequest(new { detail = "Le rôle doit être Gestionnaire ou Agent." });

        var user = await _dbContext.AppUsers.SingleOrDefaultAsync(u => u.IdAppUser == idAppUser, cancellationToken);
        if (user is null)
            return NotFound();

        if (!AssignableRoles.Contains(user.Role))
            return BadRequest(new { detail = "Le rôle de ce compte ne peut pas être modifié depuis cette interface." });

        if (request.Role == "Agent")
        {
            var agentExists = await _dbContext.Agents
                .AnyAsync(a => a.Email != null && a.Email.ToLower() == user.Email.ToLower(), cancellationToken);
            if (!agentExists)
                return BadRequest(new { detail = "Aucune fiche agent ne correspond à l'email de ce compte — impossible de le repasser en rôle Agent." });
        }

        user.ChangeRole(request.Role);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return NoContent();
    }
}

public sealed record CreateUserRequest(string Email, string Name, string Role, string Password);
public sealed record ChangeRoleRequest(string Role);
