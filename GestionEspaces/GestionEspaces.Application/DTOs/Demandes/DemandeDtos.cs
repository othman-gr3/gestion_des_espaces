using GestionEspaces.Domain.Entities;

namespace GestionEspaces.Application.DTOs.Demandes;

public sealed record DemandeDto(
    int IdDemande,
    string? ConcurrencyToken,
    int IdAgent,
    string AgentNomComplet,
    TypeDemande Type,
    string Description,
    StatutDemande Statut,
    DateTime DateCreation,
    DateTime? DateTraitement,
    string? Reponse);

public sealed record CreateDemandeRequest(TypeDemande Type, string Description);

public sealed record TakeChargeDemandeRequest(string ConcurrencyToken);

public sealed record ResolveDemandeRequest(string ConcurrencyToken, string Reponse);

public sealed record RejectDemandeRequest(string ConcurrencyToken, string Reponse);

public sealed record SearchDemandesRequest(StatutDemande? Statut, int PageNumber = 1, int PageSize = 20);
