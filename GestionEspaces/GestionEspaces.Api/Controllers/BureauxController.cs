using GestionEspaces.Api.Common;
using GestionEspaces.Application.DTOs.Bureaux;
using GestionEspaces.Application.DTOs.OfficeSearchAi;
using GestionEspaces.Application.UseCases;
using GestionEspaces.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace GestionEspaces.Api.Controllers;

[ApiController]
[Route("api/bureaux")]
public sealed class BureauxController : ControllerBase
{
    private readonly BureauUseCases _bureauUseCases;
    private readonly OfficeSearchAiUseCase _officeSearchAiUseCase;

    public BureauxController(BureauUseCases bureauUseCases, OfficeSearchAiUseCase officeSearchAiUseCase)
    {
        _bureauUseCases = bureauUseCases;
        _officeSearchAiUseCase = officeSearchAiUseCase;
    }

    [HttpGet("{idBureau:int}")]
    [Authorize(Policy = "ReferentielLecture")]
    public async Task<IActionResult> GetByIdAsync(int idBureau, CancellationToken cancellationToken)
    {
        var result = await _bureauUseCases.GetByIdAsync(idBureau, cancellationToken);
        return this.ToActionResult(result, Ok);
    }

    [HttpGet]
    [Authorize(Policy = "ReferentielLecture")]
    public async Task<IActionResult> SearchAsync([FromQuery] int? idBatiment, [FromQuery] string? searchText, [FromQuery] StatutBureau? statut, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default)
    {
        var result = await _bureauUseCases.SearchAsync(new SearchBureauxRequest(idBatiment, searchText, statut, pageNumber, pageSize), cancellationToken);
        return this.ToActionResult(result, Ok);
    }

    // AI-assisted natural-language office search — translates a French free-text request
    // into structured criteria via an LLM, degrading to keyword search if that's unavailable.
    [HttpPost("ai-search")]
    [Authorize(Policy = "ReferentielLecture")]
    [EnableRateLimiting("AiSearchPolicy")]
    public async Task<IActionResult> AiSearchAsync([FromBody] OfficeSearchAiRequest request, CancellationToken cancellationToken)
    {
        var result = await _officeSearchAiUseCase.ExecuteAsync(request.Query, cancellationToken);
        return this.ToActionResult(result, Ok);
    }

    [HttpPost]
    [Authorize(Policy = "ReferentielAdmin")]
    public async Task<IActionResult> CreateAsync([FromBody] CreateBureauRequest request, CancellationToken cancellationToken)
    {
        var result = await _bureauUseCases.CreateAsync(request, cancellationToken);
        return this.ToActionResult(result, bureau => Created($"/api/bureaux/{bureau.IdBureau}", bureau));
    }

    [HttpPut("{idBureau:int}")]
    [Authorize(Policy = "ReferentielAdmin")]
    public async Task<IActionResult> UpdateAsync(int idBureau, [FromBody] UpdateBureauRequest request, CancellationToken cancellationToken)
    {
        var result = await _bureauUseCases.UpdateAsync(idBureau, request, cancellationToken);
        return this.ToActionResult(result, Ok);
    }

    [HttpDelete("{idBureau:int}")]
    [Authorize(Policy = "ReferentielAdmin")]
    public async Task<IActionResult> DeleteAsync(int idBureau, CancellationToken cancellationToken)
    {
        var result = await _bureauUseCases.DeleteAsync(idBureau, cancellationToken);
        return this.ToActionResult(result);
    }
}
