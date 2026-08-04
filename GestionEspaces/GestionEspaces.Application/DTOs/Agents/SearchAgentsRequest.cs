namespace GestionEspaces.Application.DTOs.Agents;

/// <summary>
/// Request payload for searching/paginating agents.
/// </summary>
public sealed record SearchAgentsRequest(
    string? SearchText,
    int PageNumber,
    int PageSize);
