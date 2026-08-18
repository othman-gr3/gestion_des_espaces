using GestionEspaces.Domain.Exceptions;
using GestionEspaces.Domain.ValueObjects;

namespace GestionEspaces.Domain.Entities;

public class Site
{
    public int IdSite { get; private set; }
    public byte[] Version { get; private set; } = Array.Empty<byte>();
    public string Nom { get; private set; } = string.Empty;
    public string Code { get; private set; } = string.Empty;
    public AdresseSite Adresse { get; private set; } = null!;
    public string? Telephone { get; private set; }
    public string? Email { get; private set; }
    public string? Image { get; private set; }

    private readonly List<Batiment> _batiments = new();
    public IReadOnlyCollection<Batiment> Batiments => _batiments.AsReadOnly();

    private Site()
    {
    }

    public Site(string nom, string code, AdresseSite adresse, string? telephone, string? email, string? image)
    {
        if (string.IsNullOrWhiteSpace(nom))
        {
            throw new ArgumentException("Le nom du site est obligatoire.", nameof(nom));
        }

        if (string.IsNullOrWhiteSpace(code))
        {
            throw new ArgumentException("Le code du site est obligatoire.", nameof(code));
        }

        Nom = nom.Trim();
        Code = code.Trim().ToUpperInvariant();
        Adresse = adresse ?? throw new ArgumentNullException(nameof(adresse));
        Telephone = telephone;
        Email = email;
        Image = image;
    }

    public void MettreAJour(string nom, string code, AdresseSite adresse, string? telephone, string? email, string? image)
    {
        if (string.IsNullOrWhiteSpace(nom))
        {
            throw new ArgumentException("Le nom du site est obligatoire.", nameof(nom));
        }

        if (string.IsNullOrWhiteSpace(code))
        {
            throw new ArgumentException("Le code du site est obligatoire.", nameof(code));
        }

        Nom = nom.Trim();
        Code = code.Trim().ToUpperInvariant();
        Adresse = adresse ?? throw new ArgumentNullException(nameof(adresse));
        Telephone = telephone;
        Email = email;
        Image = image;
    }

    internal void AjouterBatiment(Batiment batiment)
    {
        if (batiment is null)
        {
            throw new ArgumentNullException(nameof(batiment));
        }

        if (_batiments.Any(existingBatiment => existingBatiment.Nom.Equals(batiment.Nom, StringComparison.OrdinalIgnoreCase)))
        {
            throw new BusinessRuleViolationException($"Le bâtiment '{batiment.Nom}' existe déjà sur ce site.");
        }

        _batiments.Add(batiment);
        batiment.DefinirSite(this);
    }
}