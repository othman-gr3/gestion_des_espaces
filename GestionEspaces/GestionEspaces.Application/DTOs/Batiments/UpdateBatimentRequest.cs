namespace GestionEspaces.Application.DTOs.Batiments;

public sealed record UpdateBatimentRequest(
    string ConcurrencyToken,
    string Nom,
    int NombreEtages,
    float Superficie,
    string? Image,
    int IdSite);