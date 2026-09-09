using GestionEspaces.Application.Common;
using GestionEspaces.Application.DTOs.AiSearch;
using GestionEspaces.Application.Interfaces;
using GestionEspaces.Application.Interfaces.Repositories;
using GestionEspaces.Domain.Entities;

namespace GestionEspaces.Application.UseCases;

/// <summary>
/// Orchestrates the AI-assisted asset search: ask the assistant to turn a natural-language
/// request into an état filter + keyword, then query the référentiel with them. Falls back
/// to using the raw query text as the keyword if the assistant is unavailable — mirrors
/// <see cref="OfficeSearchAiUseCase"/>.
/// </summary>
public sealed class ActifSearchAiUseCase
{
    private readonly IActifSearchAssistant _assistant;
    private readonly IActifRepository _actifRepository;

    public ActifSearchAiUseCase(IActifSearchAssistant assistant, IActifRepository actifRepository)
    {
        _assistant = assistant;
        _actifRepository = actifRepository;
    }

    public async Task<Result<ActifSearchAiResponse>> ExecuteAsync(string? query, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return Result<ActifSearchAiResponse>.Failure(new ErrorDetail("ValidationError", "La requête ne peut pas être vide.", "Query"));
        }

        var criteria = await _assistant.InterpretAsync(query, cancellationToken);

        if (criteria is null)
        {
            var fallbackItems = await _actifRepository.SearchAsync(query, null, 1, 50, cancellationToken);
            return Result<ActifSearchAiResponse>.Success(new ActifSearchAiResponse(
                fallbackItems.Select(actif => actif.ToDto()).ToArray(),
                "Assistant IA indisponible — recherche par mot-clé sur la désignation, le type et le numéro de série.",
                false));
        }

        var etat = criteria.Etat.HasValue && Enum.IsDefined(typeof(EtatActif), criteria.Etat.Value)
            ? (EtatActif?)criteria.Etat.Value
            : null;

        var items = await _actifRepository.SearchAsync(criteria.Keyword, etat, 1, 200, cancellationToken);

        return Result<ActifSearchAiResponse>.Success(new ActifSearchAiResponse(
            items.Select(actif => actif.ToDto()).ToArray(),
            criteria.Summary,
            true));
    }
}
