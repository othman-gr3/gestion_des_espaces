using GestionEspaces.Application.Common;
using GestionEspaces.Application.DTOs.Actifs;
using GestionEspaces.Application.DTOs.Bureaux;
using GestionEspaces.Application.Interfaces.Repositories;

namespace GestionEspaces.Application.UseCases;

/// <summary>
/// Read-only self-service queries for the Agent role: an agent may only ever
/// see data tied to their own JWT-authenticated identity (matched by email),
/// never an arbitrary agent id from the URL.
/// </summary>
public sealed class AgentSelfServiceUseCase
{
    private readonly IAgentRepository _agentRepository;
    private readonly IAffectationRepository _affectationRepository;

    public AgentSelfServiceUseCase(IAgentRepository agentRepository, IAffectationRepository affectationRepository)
    {
        _agentRepository = agentRepository;
        _affectationRepository = affectationRepository;
    }

    public async Task<Result<BureauDto?>> GetMyOfficeAsync(string email, CancellationToken cancellationToken)
    {
        var agent = await _agentRepository.GetByEmailAsync(email, cancellationToken);
        if (agent is null)
        {
            return Result<BureauDto?>.Failure(new ErrorDetail("AgentNotFound", "Aucun agent associé à ce compte."));
        }

        var postes = await _affectationRepository.GetPostesForAgentAsync(agent.IdAgent, cancellationToken);
        var activeOffice = postes.FirstOrDefault(p => p.EstActive);

        return Result<BureauDto?>.Success(activeOffice?.Bureau.ToDto());
    }

    public async Task<Result<IReadOnlyList<ActifDto>>> GetMyAssetsAsync(string email, CancellationToken cancellationToken)
    {
        var agent = await _agentRepository.GetByEmailAsync(email, cancellationToken);
        if (agent is null)
        {
            return Result<IReadOnlyList<ActifDto>>.Failure(new ErrorDetail("AgentNotFound", "Aucun agent associé à ce compte."));
        }

        var affectations = await _affectationRepository.GetActifsForAgentAsync(agent.IdAgent, cancellationToken);
        var activeAssets = affectations
            .Where(a => a.EstActive)
            .Select(a => a.Actif.ToDto())
            .ToArray();

        return Result<IReadOnlyList<ActifDto>>.Success(activeAssets);
    }
}
