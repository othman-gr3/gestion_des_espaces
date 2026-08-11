using GestionEspaces.Api.Common;
using GestionEspaces.Application.DTOs.Reservations;
using GestionEspaces.Application.UseCases;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GestionEspaces.Api.Controllers;

/// <summary>
/// Manages booking (reservation) of meeting rooms by agents.
/// </summary>
[ApiController]
[Route("api/reservations")]
public sealed class ReservationsController : ControllerBase
{
    private readonly ReservationUseCases _useCases;

    public ReservationsController(ReservationUseCases useCases)
    {
        _useCases = useCases;
    }

    [HttpGet("{id:int}")]
    [Authorize(Policy = "Lecture")]
    public async Task<IActionResult> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        var result = await _useCases.GetByIdAsync(id, cancellationToken);
        return this.ToActionResult(result, Ok);
    }

    [HttpGet]
    [Authorize(Policy = "Lecture")]
    public async Task<IActionResult> SearchAsync(
        [FromQuery] int? bureauId,
        [FromQuery] int? agentId,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] string? statut,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        Domain.Entities.StatutReservation? parsedStatut = null;
        if (!string.IsNullOrWhiteSpace(statut) &&
            Enum.TryParse<Domain.Entities.StatutReservation>(statut, ignoreCase: true, out var s))
        {
            parsedStatut = s;
        }

        var result = await _useCases.SearchAsync(
            new SearchReservationsRequest(bureauId, agentId, from, to, parsedStatut, pageNumber, pageSize),
            cancellationToken);

        return this.ToActionResult(result, Ok);
    }

    /// <summary>
    /// Creates a reservation for the specified agent.
    /// Agent identity comes from the route, not the body.
    /// </summary>
    [HttpPost("agents/{agentId:int}")]
    [Authorize(Policy = "Gestion")]
    public async Task<IActionResult> CreateAsync(int agentId, [FromBody] CreateReservationRequest request, CancellationToken cancellationToken)
    {
        var result = await _useCases.CreateAsync(agentId, request, cancellationToken);
        return this.ToActionResult(result, r => Created($"/api/reservations/{r.IdReservation}", r));
    }

    [HttpPut("{id:int}")]
    [Authorize(Policy = "Gestion")]
    public async Task<IActionResult> UpdateAsync(int id, [FromBody] UpdateReservationRequest request, CancellationToken cancellationToken)
    {
        var result = await _useCases.UpdateAsync(id, request, cancellationToken);
        return this.ToActionResult(result, Ok);
    }

    [HttpPost("{id:int}/confirmer")]
    [Authorize(Policy = "Gestion")]
    public async Task<IActionResult> ConfirmerAsync(int id, [FromBody] ConcurrencyTokenRequest request, CancellationToken cancellationToken)
    {
        var result = await _useCases.ConfirmerAsync(id, request.ConcurrencyToken, cancellationToken);
        return this.ToActionResult(result, Ok);
    }

    [HttpPost("{id:int}/annuler")]
    [Authorize(Policy = "Gestion")]
    public async Task<IActionResult> AnnulerAsync(int id, [FromBody] ConcurrencyTokenRequest request, CancellationToken cancellationToken)
    {
        var result = await _useCases.AnnulerAsync(id, request.ConcurrencyToken, cancellationToken);
        return this.ToActionResult(result, Ok);
    }

    [HttpPost("{id:int}/rejeter")]
    [Authorize(Policy = "Gestion")]
    public async Task<IActionResult> RejeterAsync(int id, [FromBody] ConcurrencyTokenRequest request, CancellationToken cancellationToken)
    {
        var result = await _useCases.RejeterAsync(id, request.ConcurrencyToken, cancellationToken);
        return this.ToActionResult(result, Ok);
    }
}

/// <summary>Simple wrapper to carry a ConcurrencyToken in workflow action bodies.</summary>
public sealed record ConcurrencyTokenRequest(string ConcurrencyToken);
