using GestionEspaces.Api.Common;
using GestionEspaces.Application.DTOs.Actifs;
using GestionEspaces.Application.DTOs.AiSearch;
using GestionEspaces.Application.UseCases;
using GestionEspaces.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace GestionEspaces.Api.Controllers;

[ApiController]
[Route("api/actifs")]
public sealed class ActifsController : ControllerBase
{
    private readonly ActifUseCases _actifUseCases;
    private readonly ActifSearchAiUseCase _actifSearchAiUseCase;

    public ActifsController(ActifUseCases actifUseCases, ActifSearchAiUseCase actifSearchAiUseCase)
    {
        _actifUseCases = actifUseCases;
        _actifSearchAiUseCase = actifSearchAiUseCase;
    }

    // AI-assisted natural-language asset search — mirrors BureauxController's ai-search.
    [HttpPost("ai-search")]
    [Authorize(Policy = "ReferentielLecture")]
    [EnableRateLimiting("AiSearchPolicy")]
    public async Task<IActionResult> AiSearchAsync([FromBody] ActifSearchAiRequest request, CancellationToken cancellationToken)
    {
        var result = await _actifSearchAiUseCase.ExecuteAsync(request.Query, cancellationToken);
        return this.ToActionResult(result, Ok);
    }

    [HttpGet("{idActif:int}")]
    [Authorize(Policy = "ReferentielLecture")]
    public async Task<IActionResult> GetByIdAsync(int idActif, CancellationToken cancellationToken)
    {
        var result = await _actifUseCases.GetByIdAsync(idActif, cancellationToken);
        return this.ToActionResult(result, Ok);
    }

    [HttpGet]
    [Authorize(Policy = "ReferentielLecture")]
    public async Task<IActionResult> SearchAsync([FromQuery] string? searchText, [FromQuery] EtatActif? etat, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default)
    {
        var result = await _actifUseCases.SearchAsync(new SearchActifsRequest(searchText, etat, pageNumber, pageSize), cancellationToken);
        return this.ToActionResult(result, Ok);
    }

    [HttpPost]
    [Authorize(Policy = "ReferentielAdmin")]
    public async Task<IActionResult> CreateAsync([FromBody] CreateActifRequest request, CancellationToken cancellationToken)
    {
        var result = await _actifUseCases.CreateAsync(request, cancellationToken);
        return this.ToActionResult(result, actif => Created($"/api/actifs/{actif.IdActif}", actif));
    }

    [HttpPut("{idActif:int}")]
    [Authorize(Policy = "ReferentielAdmin")]
    public async Task<IActionResult> UpdateAsync(int idActif, [FromBody] UpdateActifRequest request, CancellationToken cancellationToken)
    {
        var result = await _actifUseCases.UpdateAsync(idActif, request, cancellationToken);
        return this.ToActionResult(result, Ok);
    }

    [HttpDelete("{idActif:int}")]
    [Authorize(Policy = "ReferentielAdmin")]
    public async Task<IActionResult> DeleteAsync(int idActif, CancellationToken cancellationToken)
    {
        var result = await _actifUseCases.DeleteAsync(idActif, cancellationToken);
        return this.ToActionResult(result);
    }
}
