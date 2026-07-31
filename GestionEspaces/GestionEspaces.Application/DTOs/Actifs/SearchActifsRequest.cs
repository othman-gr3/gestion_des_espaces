using GestionEspaces.Domain.Entities;

namespace GestionEspaces.Application.DTOs.Actifs;

public sealed record SearchActifsRequest(
    string? SearchText,
    EtatActif? Etat,
    int PageNumber = 1,
    int PageSize = 20);