namespace GestionEspaces.Application.DTOs.Sites;

public sealed record CreateSiteRequest(
    string Nom,
    string Code,
    string Rue,
    string Ville,
    string CodePostal,
    string Pays,
    string? Telephone,
    string? Email,
    string? Image);