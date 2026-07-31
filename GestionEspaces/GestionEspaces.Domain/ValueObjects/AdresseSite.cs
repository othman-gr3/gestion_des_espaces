namespace GestionEspaces.Domain.ValueObjects;

/// <summary>
/// Postal address for a site.
/// </summary>
public sealed record AdresseSite
{
    public string Rue { get; }
    public string Ville { get; }
    public string CodePostal { get; }
    public string Pays { get; }

    public AdresseSite(string rue, string ville, string codePostal, string pays)
    {
        if (string.IsNullOrWhiteSpace(rue))
        {
            throw new ArgumentException("La rue du site est obligatoire.", nameof(rue));
        }

        if (string.IsNullOrWhiteSpace(ville))
        {
            throw new ArgumentException("La ville du site est obligatoire.", nameof(ville));
        }

        if (string.IsNullOrWhiteSpace(codePostal))
        {
            throw new ArgumentException("Le code postal du site est obligatoire.", nameof(codePostal));
        }

        if (string.IsNullOrWhiteSpace(pays))
        {
            throw new ArgumentException("Le pays du site est obligatoire.", nameof(pays));
        }

        Rue = rue.Trim();
        Ville = ville.Trim();
        CodePostal = codePostal.Trim();
        Pays = pays.Trim();
    }

    public override string ToString() => $"{Rue}, {CodePostal} {Ville}, {Pays}";
}