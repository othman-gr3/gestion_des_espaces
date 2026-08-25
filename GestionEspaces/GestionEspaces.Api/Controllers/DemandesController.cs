using GestionEspaces.Api.Common;
using GestionEspaces.Application.DTOs.Demandes;
using GestionEspaces.Application.UseCases;
using GestionEspaces.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GestionEspaces.Api.Controllers;

/// <summary>
/// Administrateur/Gestionnaire side of the agent-request workflow.
/// </summary>
[ApiController]
[Route("api/demandes")]
public sealed class DemandesController : ControllerBase
{
    private readonly DemandeUseCases _demandeUseCases;

    public DemandesController(DemandeUseCases demandeUseCases)
    {
        _demandeUseCases = demandeUseCases;
    }

    [HttpGet]
    [Authorize(Policy = "ReferentielLecture")]
    public async Task<IActionResult> SearchAsync([FromQuery] StatutDemande? statut, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default)
    {
        var result = await _demandeUseCases.SearchAsync(new SearchDemandesRequest(statut, pageNumber, pageSize), cancellationToken);
        return this.ToActionResult(result, Ok);
    }

    [HttpPost("{idDemande:int}/prendre-en-charge")]
    [Authorize(Policy = "GestionAffectations")]
    public async Task<IActionResult> PrendreEnChargeAsync(int idDemande, [FromBody] TakeChargeDemandeRequest request, CancellationToken cancellationToken)
    {
        var result = await _demandeUseCases.PrendreEnChargeAsync(idDemande, request, cancellationToken);
        return this.ToActionResult(result, Ok);
    }

    [HttpPost("{idDemande:int}/resoudre")]
    [Authorize(Policy = "GestionAffectations")]
    public async Task<IActionResult> ResoudreAsync(int idDemande, [FromBody] ResolveDemandeRequest request, CancellationToken cancellationToken)
    {
        var result = await _demandeUseCases.ResoudreAsync(idDemande, request, cancellationToken);
        return this.ToActionResult(result, Ok);
    }

    [HttpPost("{idDemande:int}/rejeter")]
    [Authorize(Policy = "GestionAffectations")]
    public async Task<IActionResult> RejeterAsync(int idDemande, [FromBody] RejectDemandeRequest request, CancellationToken cancellationToken)
    {
        var result = await _demandeUseCases.RejeterAsync(idDemande, request, cancellationToken);
        return this.ToActionResult(result, Ok);
    }
}
