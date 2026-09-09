using GestionEspaces.Application.Common;
using GestionEspaces.Application.DTOs.AiSearch;
using GestionEspaces.Application.Interfaces;
using GestionEspaces.Application.Interfaces.Repositories;

namespace GestionEspaces.Application.UseCases;

/// <summary>
/// Orchestrates the AI-assisted agent search: ask the assistant to turn a natural-language
/// request into a search keyword, then query the référentiel with it. Falls back to using
/// the raw query text as the keyword if the assistant is unavailable, so the feature
/// degrades rather than failing outright — mirrors <see cref="OfficeSearchAiUseCase"/>.
/// </summary>
public sealed class AgentSearchAiUseCase
{
    private readonly IAgentSearchAssistant _assistant;
    private readonly IAgentRepository _agentRepository;

    public AgentSearchAiUseCase(IAgentSearchAssistant assistant, IAgentRepository agentRepository)
    {
        _assistant = assistant;
        _agentRepository = agentRepository;
    }

    public async Task<Result<AgentSearchAiResponse>> ExecuteAsync(string? query, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return Result<AgentSearchAiResponse>.Failure(new ErrorDetail("ValidationError", "La requête ne peut pas être vide.", "Query"));
        }

        var criteria = await _assistant.InterpretAsync(query, cancellationToken);

        if (criteria is null)
        {
            var fallbackItems = await _agentRepository.SearchAsync(query, 1, 50, cancellationToken);
            return Result<AgentSearchAiResponse>.Success(new AgentSearchAiResponse(
                fallbackItems.Select(agent => agent.ToDto()).ToArray(),
                "Assistant IA indisponible — recherche par mot-clé sur nom, prénom, matricule, département et fonction.",
                false));
        }

        var items = await _agentRepository.SearchAsync(criteria.Keyword, 1, 200, cancellationToken);

        return Result<AgentSearchAiResponse>.Success(new AgentSearchAiResponse(
            items.Select(agent => agent.ToDto()).ToArray(),
            criteria.Summary,
            true));
    }
}
