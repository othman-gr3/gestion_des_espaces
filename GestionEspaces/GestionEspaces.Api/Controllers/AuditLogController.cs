using GestionEspaces.Api.Common;
using GestionEspaces.Application.UseCases;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GestionEspaces.Api.Controllers;

/// <summary>
/// Exposes the audit trail (who did what, when) — Administrateur only.
/// </summary>
[ApiController]
[Route("api/audit-log")]
public sealed class AuditLogController : ControllerBase
{
    private readonly AuditLogUseCases _auditLogUseCases;

    public AuditLogController(AuditLogUseCases auditLogUseCases)
    {
        _auditLogUseCases = auditLogUseCases;
    }

    [HttpGet]
    [Authorize(Policy = "ReferentielAdmin")]
    public async Task<IActionResult> SearchAsync([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default)
    {
        var result = await _auditLogUseCases.SearchAsync(pageNumber, pageSize, cancellationToken);
        return this.ToActionResult(result, Ok);
    }
}
