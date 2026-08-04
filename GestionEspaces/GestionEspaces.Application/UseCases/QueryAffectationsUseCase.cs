using GestionEspaces.Application.Common;
using GestionEspaces.Application.DTOs.Assignments;
using GestionEspaces.Application.Interfaces.Repositories;

namespace GestionEspaces.Application.UseCases;

/// <summary>
/// Queries affectation records for an agent.
/// </summary>
public sealed class QueryAffectationsUseCase
{
    private readonly IAgentRepository _agentRepository;
    private readonly IAffectationRepository _affectationRepository;

    public QueryAffectationsUseCase(IAgentRepository agentRepository, IAffectationRepository affectationRepository)
    {
        _agentRepository = agentRepository;
        _affectationRepository = affectationRepository;
    }

    public async Task<Result<IReadOnlyList<AffectationPosteDto>>> GetPostesForAgentAsync(int agentId, CancellationToken cancellationToken)
    {
        if (await _agentRepository.GetByIdAsync(agentId, cancellationToken) is null)
        {
            return Result<IReadOnlyList<AffectationPosteDto>>.Failure(new ErrorDetail("AgentNotFound", $"Agent {agentId} introuvable."));
        }

        var affectations = await _affectationRepository.GetPostesForAgentAsync(agentId, cancellationToken);
        return Result<IReadOnlyList<AffectationPosteDto>>.Success(affectations.Select(a => a.ToDto()).ToArray());
    }

    public async Task<Result<IReadOnlyList<AffectationActifDto>>> GetActifsForAgentAsync(int agentId, CancellationToken cancellationToken)
    {
        if (await _agentRepository.GetByIdAsync(agentId, cancellationToken) is null)
        {
            return Result<IReadOnlyList<AffectationActifDto>>.Failure(new ErrorDetail("AgentNotFound", $"Agent {agentId} introuvable."));
        }

        var affectations = await _affectationRepository.GetActifsForAgentAsync(agentId, cancellationToken);
        return Result<IReadOnlyList<AffectationActifDto>>.Success(affectations.Select(a => a.ToDto()).ToArray());
    }
}
