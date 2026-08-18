namespace GestionEspaces.Application.DTOs.Assignments;

/// <summary>
/// Request payload for assigning an agent to an office.
/// </summary>
public sealed record AssignAgentToOfficeRequest(int AgentId, int BureauId, DateTime DateAffectation, string? Motif = null);