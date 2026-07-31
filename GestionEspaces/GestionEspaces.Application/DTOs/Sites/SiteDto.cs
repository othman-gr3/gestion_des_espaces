namespace GestionEspaces.Application.DTOs.Sites;

public sealed record SiteDto(
    int IdSite,
    string? ConcurrencyToken,
    string Nom,
    string Code,
    string Rue,
    string Ville,
    string CodePostal,
    string Pays,
    string? Image);