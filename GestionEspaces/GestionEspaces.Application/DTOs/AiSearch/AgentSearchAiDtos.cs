using GestionEspaces.Application.DTOs.Agents;

namespace GestionEspaces.Application.DTOs.AiSearch;

/// <summary>Structured search criteria extracted from a natural-language agent request.</summary>
public sealed record AgentSearchCriteria(string? Keyword, string Summary);

public sealed record AgentSearchAiRequest(string Query);

public sealed record AgentSearchAiResponse(
    IReadOnlyCollection<AgentDto> Results,
    string Summary,
    bool UsedAi);
