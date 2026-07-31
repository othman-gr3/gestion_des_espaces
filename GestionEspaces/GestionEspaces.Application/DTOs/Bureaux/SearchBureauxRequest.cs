using GestionEspaces.Domain.Entities;

namespace GestionEspaces.Application.DTOs.Bureaux;

public sealed record SearchBureauxRequest(
    int? IdBatiment,
    string? SearchText,
    StatutBureau? Statut,
    int PageNumber = 1,
    int PageSize = 20);