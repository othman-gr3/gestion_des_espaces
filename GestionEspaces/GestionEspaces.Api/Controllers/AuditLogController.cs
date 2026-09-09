using GestionEspaces.Api.Common;
using GestionEspaces.Application.UseCases;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

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

    // AI-assisted anomaly detection over the recent audit trail — falls back to a rule-based
    // heuristic (activity bursts, off-hours actions) when the assistant is unavailable.
    [HttpPost("anomalies")]
    [Authorize(Policy = "ReferentielAdmin")]
    [EnableRateLimiting("AiSearchPolicy")]
    public async Task<IActionResult> AnalyzeAnomaliesAsync(CancellationToken cancellationToken)
    {
        var result = await _auditLogUseCases.AnalyzeAnomaliesAsync(cancellationToken);
        return this.ToActionResult(result, Ok);
    }
}
