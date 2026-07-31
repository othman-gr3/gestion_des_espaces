using GestionEspaces.Domain.Exceptions;

namespace GestionEspaces.Domain.Entities;

public enum EtatActif
{
    Neuf,
    Bon,
    ARepairer,
    HorsService
}

public class Actif
{
    public int IdActif { get; private set; }
    public byte[] Version { get; private set; } = Array.Empty<byte>();
    public string Nom { get; private set; } = string.Empty;
    public string? Type { get; private set; }
    public string? Marque { get; private set; }
    public string? Modele { get; private set; }
    public string? NumeroSerie { get; private set; }
    public DateTime? DateAchat { get; private set; }
    public EtatActif Etat { get; private set; } = EtatActif.Neuf;
    public string? Image { get; private set; }

    private readonly List<AffectationActif> _affectations = new();
    public IReadOnlyCollection<AffectationActif> Affectations => _affectations.AsReadOnly();

    private Actif() { }

    public Actif(string nom, string? type, string? marque, string? modele,
                 string? numeroSerie, DateTime? dateAchat, string? image)
    {
        if (string.IsNullOrWhiteSpace(nom))
            throw new ArgumentException("Le nom de l'actif est obligatoire.", nameof(nom));

        Nom = nom;
        Type = type;
        Marque = marque;
        Modele = modele;
        NumeroSerie = numeroSerie;
        DateAchat = dateAchat;
        Image = image;
    }

    public void MettreAJour(string nom, string? type, string? marque, string? modele,
        string? numeroSerie, DateTime? dateAchat, string? image, EtatActif etat)
    {
        if (string.IsNullOrWhiteSpace(nom))
        {
            throw new ArgumentException("Le nom de l'actif est obligatoire.", nameof(nom));
        }

        Nom = nom.Trim();
        Type = type?.Trim();
        Marque = marque?.Trim();
        Modele = modele?.Trim();
        NumeroSerie = numeroSerie?.Trim();
        DateAchat = dateAchat;
        Image = image;
        Etat = etat;
    }

    public void MarquerARepairer() => Etat = EtatActif.ARepairer;
    public void MarquerHorsService() => Etat = EtatActif.HorsService;
    public void MarquerBonEtat() => Etat = EtatActif.Bon;
    public bool EstDisponible() => Etat != EtatActif.HorsService;

    public AffectationActif AffecterA(Agent agent, DateTime dateAffectation)
    {
        if (agent is null)
        {
            throw new ArgumentNullException(nameof(agent));
        }

        if (_affectations.Any(affectation => affectation.EstActive))
        {
            throw new BusinessRuleViolationException($"L'actif '{Nom}' possède déjà une affectation active.");
        }

        if (!EstDisponible())
        {
            throw new BusinessRuleViolationException($"L'actif '{Nom}' n'est pas disponible pour une affectation.");
        }

        var affectation = new AffectationActif(agent, this, dateAffectation);
        _affectations.Add(affectation);
        agent.AjouterAffectationActif(affectation);

        return affectation;
    }
}