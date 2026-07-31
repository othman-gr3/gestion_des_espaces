namespace GestionEspaces.Application.DTOs.Assignments;

/// <summary>
/// Shared response model for assignment operations.
/// </summary>
public sealed record AssignmentUseCaseResult(
    int AssignmentId,
    int AgentId,
    int ResourceId,
    DateTime DateAffectation,
    DateTime? DateFin);