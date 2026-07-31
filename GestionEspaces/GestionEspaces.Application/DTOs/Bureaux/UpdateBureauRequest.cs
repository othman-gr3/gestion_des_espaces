using GestionEspaces.Domain.Entities;

namespace GestionEspaces.Application.DTOs.Bureaux;

public sealed record UpdateBureauRequest(
    string ConcurrencyToken,
    string Numero,
    string? Type,
    int Capacite,
    float Superficie,
    int Etage,
    string? Image,
    int IdBatiment,
    StatutBureau Statut);