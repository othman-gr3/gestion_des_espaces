using GestionEspaces.Domain.Common;
using GestionEspaces.Domain.Events;

namespace GestionEspaces.Domain.Entities;

public enum StatutBureau
{
    Disponible,
    Occupe,
    EnMaintenance
}

public enum TypeBureau
{
    Individuel,
    OpenSpace,
    SalleReunion
}

public class Bureau : EntityBase
{
    public int IdBureau { get; private set; }
    public byte[] Version { get; private set; } = Array.Empty<byte>();
    public string Numero { get; private set; } = string.Empty;
    public TypeBureau? Type { get; private set; }
    public int Capacite { get; private set; }
    public float Superficie { get; private set; }
    public int Etage { get; private set; }
    public StatutBureau Statut { get; private set; } = StatutBureau.Disponible;
    public string? Image { get; private set; }

    public int IdBatiment { get; private set; }
    public Batiment Batiment { get; private set; } = null!;

    private readonly List<AffectationPoste> _affectations = new();
    public IReadOnlyCollection<AffectationPoste> Affectations => _affectations.AsReadOnly();

    private Bureau() { }

    public Bureau(string numero, TypeBureau? type, int capacite, float superficie,
                  int etage, string? image, int idBatiment)
    {
        if (string.IsNullOrWhiteSpace(numero))
            throw new ArgumentException("Le numéro de bureau est obligatoire.", nameof(numero));
        if (capacite <= 0)
            throw new ArgumentException("La capacité doit être positive.", nameof(capacite));

        Numero = numero;
        Type = type;
        Capacite = capacite;
        Superficie = superficie;
        Etage = etage;
        Image = image;
        IdBatiment = idBatiment;
    }

    public void MettreAJour(string numero, TypeBureau? type, int capacite, float superficie, int etage, string? image, int idBatiment)
    {
        if (string.IsNullOrWhiteSpace(numero))
        {
            throw new ArgumentException("Le numéro de bureau est obligatoire.", nameof(numero));
        }

        if (capacite <= 0)
        {
            throw new ArgumentException("La capacité doit être positive.", nameof(capacite));
        }

        Numero = numero.Trim();
        Type = type;
        Capacite = capacite;
        Superficie = superficie;
        Etage = etage;
        Image = image;
        IdBatiment = idBatiment;
    }

    public void MettreEnMaintenance()
    {
        Statut = StatutBureau.EnMaintenance;
        RaiseDomainEvent(new BureauMisEnMaintenanceEvent(IdBureau, Numero));
    }

    public void RemettreEnService()
    {
        Statut = StatutBureau.Disponible;
        RaiseDomainEvent(new BureauRemisEnServiceEvent(IdBureau, Numero));
    }

    public bool EstDisponible() => Statut == StatutBureau.Disponible;

    /// <summary>
    /// Marks the office as occupied. Called automatically when an agent is assigned to it
    /// (<see cref="Agent.AffecterAuBureau"/>) — never invoked directly from outside the domain.
    /// </summary>
    internal void MarquerOccupe() => Statut = StatutBureau.Occupe;

    internal void AjouterAffectationPoste(AffectationPoste affectation)
    {
        if (affectation is null)
        {
            throw new ArgumentNullException(nameof(affectation));
        }

        if (_affectations.Contains(affectation))
        {
            return;
        }

        _affectations.Add(affectation);
    }
}