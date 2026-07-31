namespace GestionEspaces.Application.DTOs.Batiments;

public sealed record CreateBatimentRequest(
    string Nom,
    int NombreEtages,
    float Superficie,
    string? Image,
    int IdSite);