namespace GestionEspaces.Application.DTOs.Assignments;

/// <summary>
/// Request payload for assigning an asset to an agent.
/// </summary>
public sealed record AssignAssetToAgentRequest(int AgentId, int ActifId, DateTime DateAffectation);