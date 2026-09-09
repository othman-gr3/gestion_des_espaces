using GestionEspaces.Application.DTOs.Actifs;

namespace GestionEspaces.Application.DTOs.AiSearch;

/// <summary>Structured search criteria extracted from a natural-language asset request.</summary>
public sealed record ActifSearchCriteria(int? Etat, string? Keyword, string Summary);

public sealed record ActifSearchAiRequest(string Query);

public sealed record ActifSearchAiResponse(
    IReadOnlyCollection<ActifDto> Results,
    string Summary,
    bool UsedAi);
