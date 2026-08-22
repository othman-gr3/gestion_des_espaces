using GestionEspaces.Application.Common;
using GestionEspaces.Application.DTOs.OfficeSearchAi;
using GestionEspaces.Application.Interfaces;
using GestionEspaces.Application.Interfaces.Repositories;
using GestionEspaces.Domain.Entities;

namespace GestionEspaces.Application.UseCases;

/// <summary>
/// Orchestrates the AI-assisted office search: ask the assistant to turn a natural-language
/// request into structured criteria, then query the referentiel with them. If the assistant
/// is unavailable or returns nothing usable, falls back to a plain keyword search against
/// the office number so the feature degrades rather than failing outright.
/// </summary>
public sealed class OfficeSearchAiUseCase
{
    private readonly IOfficeSearchAssistant _assistant;
    private readonly IBureauRepository _bureauRepository;
    private readonly IBatimentRepository _batimentRepository;
    private readonly ISiteRepository _siteRepository;

    public OfficeSearchAiUseCase(
        IOfficeSearchAssistant assistant,
        IBureauRepository bureauRepository,
        IBatimentRepository batimentRepository,
        ISiteRepository siteRepository)
    {
        _assistant = assistant;
        _bureauRepository = bureauRepository;
        _batimentRepository = batimentRepository;
        _siteRepository = siteRepository;
    }

    public async Task<Result<OfficeSearchAiResponse>> ExecuteAsync(string? query, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return Result<OfficeSearchAiResponse>.Failure(new ErrorDetail("ValidationError", "La requête ne peut pas être vide.", "Query"));
        }

        var batimentOptions = await BuildBatimentOptionsAsync(cancellationToken);
        var criteria = await _assistant.InterpretAsync(query, batimentOptions, cancellationToken);

        if (criteria is null)
        {
            return await SearchByKeywordAsync(query, cancellationToken);
        }

        var statut = criteria.Statut.HasValue && Enum.IsDefined(typeof(StatutBureau), criteria.Statut.Value)
            ? (StatutBureau?)criteria.Statut.Value
            : null;

        var items = await _bureauRepository.SearchAsync(criteria.IdBatiment, null, statut, 1, 200, cancellationToken);

        IEnumerable<Bureau> filtered = items;
        if (criteria.CapaciteMin.HasValue)
        {
            filtered = filtered.Where(bureau => bureau.Capacite >= criteria.CapaciteMin.Value);
        }

        if (criteria.EtageMin.HasValue)
        {
            filtered = filtered.Where(bureau => bureau.Etage >= criteria.EtageMin.Value);
        }

        if (criteria.Type.HasValue && Enum.IsDefined(typeof(TypeBureau), criteria.Type.Value))
        {
            var type = (TypeBureau)criteria.Type.Value;
            filtered = filtered.Where(bureau => bureau.Type == type);
        }

        return Result<OfficeSearchAiResponse>.Success(new OfficeSearchAiResponse(
            filtered.Select(bureau => bureau.ToDto()).ToArray(),
            criteria.Summary,
            true));
    }

    private async Task<Result<OfficeSearchAiResponse>> SearchByKeywordAsync(string query, CancellationToken cancellationToken)
    {
        var items = await _bureauRepository.SearchAsync(null, query, null, 1, 50, cancellationToken);

        return Result<OfficeSearchAiResponse>.Success(new OfficeSearchAiResponse(
            items.Select(bureau => bureau.ToDto()).ToArray(),
            "Assistant IA indisponible — recherche par mot-clé sur le numéro de bureau.",
            false));
    }

    private async Task<IReadOnlyList<BatimentOption>> BuildBatimentOptionsAsync(CancellationToken cancellationToken)
    {
        var batiments = await _batimentRepository.SearchAsync(null, null, 1, 200, cancellationToken);
        var sites = await _siteRepository.SearchAsync(null, 1, 200, cancellationToken);
        var siteNamesById = sites.ToDictionary(site => site.IdSite, site => site.Nom);

        return batiments
            .Select(batiment => new BatimentOption(batiment.IdBatiment, batiment.Nom, siteNamesById.GetValueOrDefault(batiment.IdSite, string.Empty)))
            .ToArray();
    }
}
