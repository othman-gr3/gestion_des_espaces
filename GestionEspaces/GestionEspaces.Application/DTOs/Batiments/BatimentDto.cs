namespace GestionEspaces.Application.DTOs.Batiments;

public sealed record BatimentDto(
    int IdBatiment,
    string? ConcurrencyToken,
    string Nom,
    int NombreEtages,
    float Superficie,
    string? Image,
    int IdSite);