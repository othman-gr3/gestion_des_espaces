using GestionEspaces.Api.Common;
using GestionEspaces.Application.DTOs.Agents;
using GestionEspaces.Application.DTOs.Assignments;
using GestionEspaces.Application.UseCases;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GestionEspaces.Api.Controllers;

/// <summary>
/// Exposes full CRUD for agents plus office and asset assignment endpoints.
/// </summary>
[ApiController]
[Route("api/agents")]
public sealed class AgentsController : ControllerBase
{
    private readonly AgentUseCases _agentUseCases;
    private readonly AssignAgentToOfficeUseCase _assignAgentToOfficeUseCase;
    private readonly AssignAssetToAgentUseCase _assignAssetToAgentUseCase;
    private readonly CloseAffectationPosteUseCase _closePosteUseCase;
    private readonly CloseAffectationActifUseCase _closeActifUseCase;
    private readonly QueryAffectationsUseCase _queryAffectationsUseCase;

    public AgentsController(
        AgentUseCases agentUseCases,
        AssignAgentToOfficeUseCase assignAgentToOfficeUseCase,
        AssignAssetToAgentUseCase assignAssetToAgentUseCase,
        CloseAffectationPosteUseCase closePosteUseCase,
        CloseAffectationActifUseCase closeActifUseCase,
        QueryAffectationsUseCase queryAffectationsUseCase)
    {
        _agentUseCases = agentUseCases;
        _assignAgentToOfficeUseCase = assignAgentToOfficeUseCase;
        _assignAssetToAgentUseCase = assignAssetToAgentUseCase;
        _closePosteUseCase = closePosteUseCase;
        _closeActifUseCase = closeActifUseCase;
        _queryAffectationsUseCase = queryAffectationsUseCase;
    }

    // ── CRUD ──────────────────────────────────────────────────────────────────

    [HttpGet("{idAgent:int}")]
    [Authorize(Policy = "Lecture")]
    public async Task<IActionResult> GetByIdAsync(int idAgent, CancellationToken cancellationToken)
    {
        var result = await _agentUseCases.GetByIdAsync(idAgent, cancellationToken);
        return this.ToActionResult(result, Ok);
    }

    [HttpGet]
    [Authorize(Policy = "Lecture")]
    public async Task<IActionResult> SearchAsync(
        [FromQuery] string? searchText,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await _agentUseCases.SearchAsync(new SearchAgentsRequest(searchText, pageNumber, pageSize), cancellationToken);
        return this.ToActionResult(result, Ok);
    }

    [HttpPost]
    [Authorize(Policy = "Gestion")]
    public async Task<IActionResult> CreateAsync([FromBody] CreateAgentRequest request, CancellationToken cancellationToken)
    {
        var result = await _agentUseCases.CreateAsync(request, cancellationToken);
        return this.ToActionResult(result, agent => Created($"/api/agents/{agent.IdAgent}", agent));
    }

    [HttpPut("{idAgent:int}")]
    [Authorize(Policy = "Gestion")]
    public async Task<IActionResult> UpdateAsync(int idAgent, [FromBody] UpdateAgentRequest request, CancellationToken cancellationToken)
    {
        var result = await _agentUseCases.UpdateAsync(idAgent, request, cancellationToken);
        return this.ToActionResult(result, Ok);
    }

    [HttpDelete("{idAgent:int}")]
    [Authorize(Policy = "Gestion")]
    public async Task<IActionResult> DeleteAsync(int idAgent, CancellationToken cancellationToken)
    {
        var result = await _agentUseCases.DeleteAsync(idAgent, cancellationToken);
        return this.ToActionResult(result);
    }

    // ── Office assignments ────────────────────────────────────────────────────

    [HttpGet("{agentId:int}/office-assignments")]
    [Authorize(Policy = "Lecture")]
    public async Task<IActionResult> GetOfficeAssignmentsAsync(int agentId, CancellationToken cancellationToken)
    {
        var result = await _queryAffectationsUseCase.GetPostesForAgentAsync(agentId, cancellationToken);
        return this.ToActionResult(result, Ok);
    }

    [HttpPost("{agentId:int}/office-assignments")]
    [Authorize(Policy = "Gestion")]
    public async Task<IActionResult> AssignAgentToOfficeAsync(int agentId, [FromBody] AssignAgentToOfficeRequest request, CancellationToken cancellationToken)
    {
        var command = request with { AgentId = agentId };
        var result = await _assignAgentToOfficeUseCase.ExecuteAsync(command, cancellationToken);
        return this.ToActionResult(result, response => Ok(response));
    }

    [HttpDelete("{agentId:int}/office-assignments/{affId:int}")]
    [Authorize(Policy = "Gestion")]
    public async Task<IActionResult> CloseOfficeAssignmentAsync(int agentId, int affId, [FromBody] CloseAffectationRequest request, CancellationToken cancellationToken)
    {
        var result = await _closePosteUseCase.ExecuteAsync(agentId, affId, request, cancellationToken);
        return this.ToActionResult(result, Ok);
    }

    // ── Asset assignments ─────────────────────────────────────────────────────

    [HttpGet("{agentId:int}/asset-assignments")]
    [Authorize(Policy = "Lecture")]
    public async Task<IActionResult> GetAssetAssignmentsAsync(int agentId, CancellationToken cancellationToken)
    {
        var result = await _queryAffectationsUseCase.GetActifsForAgentAsync(agentId, cancellationToken);
        return this.ToActionResult(result, Ok);
    }

    [HttpPost("{agentId:int}/asset-assignments")]
    [Authorize(Policy = "Gestion")]
    public async Task<IActionResult> AssignAssetToAgentAsync(int agentId, [FromBody] AssignAssetToAgentRequest request, CancellationToken cancellationToken)
    {
        var command = request with { AgentId = agentId };
        var result = await _assignAssetToAgentUseCase.ExecuteAsync(command, cancellationToken);
        return this.ToActionResult(result, response => Ok(response));
    }

    [HttpDelete("{agentId:int}/asset-assignments/{affId:int}")]
    [Authorize(Policy = "Gestion")]
    public async Task<IActionResult> CloseAssetAssignmentAsync(int agentId, int affId, [FromBody] CloseAffectationRequest request, CancellationToken cancellationToken)
    {
        var result = await _closeActifUseCase.ExecuteAsync(agentId, affId, request, cancellationToken);
        return this.ToActionResult(result, Ok);
    }
}