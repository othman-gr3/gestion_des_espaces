using GestionEspaces.Api.Common;
using GestionEspaces.Application.DTOs.Sites;
using GestionEspaces.Application.UseCases;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GestionEspaces.Api.Controllers;

[ApiController]
[Route("api/sites")]
public sealed class SitesController : ControllerBase
{
    private readonly SiteUseCases _siteUseCases;

    public SitesController(SiteUseCases siteUseCases)
    {
        _siteUseCases = siteUseCases;
    }

    [HttpGet("{idSite:int}")]
    [Authorize(Policy = "Lecture")]
    public async Task<IActionResult> GetByIdAsync(int idSite, CancellationToken cancellationToken)
    {
        var result = await _siteUseCases.GetByIdAsync(idSite, cancellationToken);
        return this.ToActionResult(result, Ok);
    }

    [HttpGet]
    [Authorize(Policy = "Lecture")]
    public async Task<IActionResult> SearchAsync([FromQuery] string? searchText, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default)
    {
        var result = await _siteUseCases.SearchAsync(new SearchSitesRequest(searchText, pageNumber, pageSize), cancellationToken);
        return this.ToActionResult(result, Ok);
    }

    [HttpPost]
    [Authorize(Policy = "Gestion")]
    public async Task<IActionResult> CreateAsync([FromBody] CreateSiteRequest request, CancellationToken cancellationToken)
    {
        var result = await _siteUseCases.CreateAsync(request, cancellationToken);
        return this.ToActionResult(result, site => Created($"/api/sites/{site.IdSite}", site));
    }

    [HttpPut("{idSite:int}")]
    [Authorize(Policy = "Gestion")]
    public async Task<IActionResult> UpdateAsync(int idSite, [FromBody] UpdateSiteRequest request, CancellationToken cancellationToken)
    {
        var result = await _siteUseCases.UpdateAsync(idSite, request, cancellationToken);
        return this.ToActionResult(result, Ok);
    }

    [HttpDelete("{idSite:int}")]
    [Authorize(Policy = "Gestion")]
    public async Task<IActionResult> DeleteAsync(int idSite, CancellationToken cancellationToken)
    {
        var result = await _siteUseCases.DeleteAsync(idSite, cancellationToken);
        return this.ToActionResult(result);
    }
}