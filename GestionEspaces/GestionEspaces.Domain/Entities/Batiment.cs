namespace GestionEspaces.Domain.Entities;

public class Batiment
{
    public int IdBatiment { get; private set; }
    public byte[] Version { get; private set; } = Array.Empty<byte>();
    public string Nom { get; private set; } = string.Empty;
    public int NombreEtages { get; private set; }
    public float Superficie { get; private set; }
    public string? Image { get; private set; }

    public int IdSite { get; private set; }
    public Site Site { get; private set; } = null!;

    private readonly List<Bureau> _bureaux = new();
    public IReadOnlyCollection<Bureau> Bureaux => _bureaux.AsReadOnly();

    private Batiment() { }

    public Batiment(string nom, int nombreEtages, float superficie, string? image, int idSite)
    {
        if (string.IsNullOrWhiteSpace(nom))
            throw new ArgumentException("Le nom du bâtiment est obligatoire.", nameof(nom));
        if (nombreEtages < 0)
            throw new ArgumentException("Le nombre d'étages ne peut pas être négatif.", nameof(nombreEtages));

        Nom = nom;
        NombreEtages = nombreEtages;
        Superficie = superficie;
        Image = image;
        IdSite = idSite;
    }

    public void MettreAJour(string nom, int nombreEtages, float superficie, string? image, int idSite)
    {
        if (string.IsNullOrWhiteSpace(nom))
        {
            throw new ArgumentException("Le nom du bâtiment est obligatoire.", nameof(nom));
        }

        if (nombreEtages < 0)
        {
            throw new ArgumentException("Le nombre d'étages ne peut pas être négatif.", nameof(nombreEtages));
        }

        Nom = nom.Trim();
        NombreEtages = nombreEtages;
        Superficie = superficie;
        Image = image;
        IdSite = idSite;
    }

    internal void DefinirSite(Site site)
    {
        Site = site ?? throw new ArgumentNullException(nameof(site));
        IdSite = site.IdSite;
    }
}