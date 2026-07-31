using GestionEspaces.Api.Common;
using GestionEspaces.Application.DTOs.Agents;
using GestionEspaces.Application.DTOs.Assignments;
using GestionEspaces.Application.UseCases;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GestionEspaces.Api.Controllers;

/// <summary>
/// Exposes agent creation and assignment endpoints.
/// </summary>
[ApiController]
[Route("api/agents")]
public sealed class AgentsController : ControllerBase
{
    private readonly CreateAgentUseCase _createAgentUseCase;
    private readonly AssignAgentToOfficeUseCase _assignAgentToOfficeUseCase;
    private readonly AssignAssetToAgentUseCase _assignAssetToAgentUseCase;

    public AgentsController(
        CreateAgentUseCase createAgentUseCase,
        AssignAgentToOfficeUseCase assignAgentToOfficeUseCase,
        AssignAssetToAgentUseCase assignAssetToAgentUseCase)
    {
        _createAgentUseCase = createAgentUseCase;
        _assignAgentToOfficeUseCase = assignAgentToOfficeUseCase;
        _assignAssetToAgentUseCase = assignAssetToAgentUseCase;
    }

    [HttpPost]
    [Authorize(Policy = "Gestion")]
    public async Task<IActionResult> CreateAsync([FromBody] CreateAgentRequest request, CancellationToken cancellationToken)
    {
        var result = await _createAgentUseCase.ExecuteAsync(request, cancellationToken);
        return this.ToActionResult(result, agent => Created($"/api/agents/{agent.IdAgent}", agent));
    }

    [HttpPost("{agentId:int}/office-assignments")]
    [Authorize(Policy = "Gestion")]
    public async Task<IActionResult> AssignAgentToOfficeAsync(int agentId, [FromBody] AssignAgentToOfficeRequest request, CancellationToken cancellationToken)
    {
        var command = request with { AgentId = agentId };
        var result = await _assignAgentToOfficeUseCase.ExecuteAsync(command, cancellationToken);
        return this.ToActionResult(result, response => Ok(response));
    }

    [HttpPost("{agentId:int}/asset-assignments")]
    [Authorize(Policy = "Gestion")]
    public async Task<IActionResult> AssignAssetToAgentAsync(int agentId, [FromBody] AssignAssetToAgentRequest request, CancellationToken cancellationToken)
    {
        var command = request with { AgentId = agentId };
        var result = await _assignAssetToAgentUseCase.ExecuteAsync(command, cancellationToken);
        return this.ToActionResult(result, response => Ok(response));
    }
}