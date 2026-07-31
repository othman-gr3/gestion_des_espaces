using GestionEspaces.Api.Common;
using GestionEspaces.Application.DTOs.Batiments;
using GestionEspaces.Application.UseCases;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GestionEspaces.Api.Controllers;

[ApiController]
[Route("api/batiments")]
public sealed class BatimentsController : ControllerBase
{
    private readonly BatimentUseCases _batimentUseCases;

    public BatimentsController(BatimentUseCases batimentUseCases)
    {
        _batimentUseCases = batimentUseCases;
    }

    [HttpGet("{idBatiment:int}")]
    [Authorize(Policy = "Lecture")]
    public async Task<IActionResult> GetByIdAsync(int idBatiment, CancellationToken cancellationToken)
    {
        var result = await _batimentUseCases.GetByIdAsync(idBatiment, cancellationToken);
        return this.ToActionResult(result, Ok);
    }

    [HttpGet]
    [Authorize(Policy = "Lecture")]
    public async Task<IActionResult> SearchAsync([FromQuery] int? idSite, [FromQuery] string? searchText, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default)
    {
        var result = await _batimentUseCases.SearchAsync(new SearchBatimentsRequest(idSite, searchText, pageNumber, pageSize), cancellationToken);
        return this.ToActionResult(result, Ok);
    }

    [HttpPost]
    [Authorize(Policy = "Gestion")]
    public async Task<IActionResult> CreateAsync([FromBody] CreateBatimentRequest request, CancellationToken cancellationToken)
    {
        var result = await _batimentUseCases.CreateAsync(request, cancellationToken);
        return this.ToActionResult(result, batiment => Created($"/api/batiments/{batiment.IdBatiment}", batiment));
    }

    [HttpPut("{idBatiment:int}")]
    [Authorize(Policy = "Gestion")]
    public async Task<IActionResult> UpdateAsync(int idBatiment, [FromBody] UpdateBatimentRequest request, CancellationToken cancellationToken)
    {
        var result = await _batimentUseCases.UpdateAsync(idBatiment, request, cancellationToken);
        return this.ToActionResult(result, Ok);
    }

    [HttpDelete("{idBatiment:int}")]
    [Authorize(Policy = "Gestion")]
    public async Task<IActionResult> DeleteAsync(int idBatiment, CancellationToken cancellationToken)
    {
        var result = await _batimentUseCases.DeleteAsync(idBatiment, cancellationToken);
        return this.ToActionResult(result);
    }
}