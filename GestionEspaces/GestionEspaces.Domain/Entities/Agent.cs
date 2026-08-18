using GestionEspaces.Domain.Exceptions;

namespace GestionEspaces.Domain.Entities;

public class Agent
{
    public int IdAgent { get; private set; }
    public byte[] Version { get; private set; } = Array.Empty<byte>();
    public string Nom { get; private set; } = string.Empty;
    public string Prenom { get; private set; } = string.Empty;
    public string Matricule { get; private set; } = string.Empty;
    public string? Email { get; private set; }
    public string? Telephone { get; private set; }
    public string? Fonction { get; private set; }
    public string? Departement { get; private set; }
    public DateTime? DateEmbauche { get; private set; }
    public string? Image { get; private set; }

    private readonly List<AffectationPoste> _affectationsPoste = new();
    public IReadOnlyCollection<AffectationPoste> AffectationsPoste => _affectationsPoste.AsReadOnly();

    private readonly List<AffectationActif> _affectationsActif = new();
    public IReadOnlyCollection<AffectationActif> AffectationsActif => _affectationsActif.AsReadOnly();

    private Agent() { }

    public Agent(string nom, string prenom, string matricule, string? email,
                 string? telephone, string? fonction, string? departement,
                 DateTime? dateEmbauche, string? image)
    {
        if (string.IsNullOrWhiteSpace(nom))
            throw new ArgumentException("Le nom est obligatoire.", nameof(nom));
        if (string.IsNullOrWhiteSpace(prenom))
            throw new ArgumentException("Le prénom est obligatoire.", nameof(prenom));
        if (string.IsNullOrWhiteSpace(matricule))
            throw new ArgumentException("Le matricule est obligatoire.", nameof(matricule));

        Nom = nom;
        Prenom = prenom;
        Matricule = matricule;
        Email = email;
        Telephone = telephone;
        Fonction = fonction;
        Departement = departement;
        DateEmbauche = dateEmbauche;
        Image = image;
    }

    public void MettreAJour(string nom, string prenom, string matricule, string? email,
        string? telephone, string? fonction, string? departement,
        DateTime? dateEmbauche, string? image)
    {
        if (string.IsNullOrWhiteSpace(nom))
            throw new ArgumentException("Le nom est obligatoire.", nameof(nom));
        if (string.IsNullOrWhiteSpace(prenom))
            throw new ArgumentException("Le prénom est obligatoire.", nameof(prenom));
        if (string.IsNullOrWhiteSpace(matricule))
            throw new ArgumentException("Le matricule est obligatoire.", nameof(matricule));

        Nom = nom;
        Prenom = prenom;
        Matricule = matricule;
        Email = email;
        Telephone = telephone;
        Fonction = fonction;
        Departement = departement;
        DateEmbauche = dateEmbauche;
        Image = image;
    }

    public AffectationPoste AffecterAuBureau(Bureau bureau, DateTime dateAffectation, string? motif = null)
    {
        if (bureau is null)
        {
            throw new ArgumentNullException(nameof(bureau));
        }

        if (_affectationsPoste.Any(affectation => affectation.EstActive))
        {
            throw new BusinessRuleViolationException("L'agent possède déjà une affectation de poste active.");
        }

        if (!bureau.EstDisponible())
        {
            throw new BusinessRuleViolationException($"Le bureau {bureau.Numero} n'est pas disponible pour une affectation.");
        }

        if (bureau.Affectations.Any(affectation => affectation.EstActive))
        {
            throw new BusinessRuleViolationException($"Le bureau {bureau.Numero} possède déjà une affectation de poste active.");
        }

        var affectation = new AffectationPoste(this, bureau, dateAffectation, motif);
        _affectationsPoste.Add(affectation);
        bureau.AjouterAffectationPoste(affectation);
        bureau.MarquerOccupe();

        return affectation;
    }

    public AffectationActif AffecterActif(Actif actif, DateTime dateAffectation)
    {
        if (actif is null)
        {
            throw new ArgumentNullException(nameof(actif));
        }

        return actif.AffecterA(this, dateAffectation);
    }

    public void CloreAffectationPoste(int idAffectationPoste, DateTime dateFin)
    {
        var affectation = _affectationsPoste.SingleOrDefault(item => item.IdAffectationPoste == idAffectationPoste)
            ?? throw new BusinessRuleViolationException("Aucune affectation de poste correspondante n'a été trouvée.");

        affectation.Clore(dateFin);
    }

    public void CloreAffectationActif(int idAffectationActif, DateTime dateFin)
    {
        var affectation = _affectationsActif.SingleOrDefault(item => item.IdAffectationActif == idAffectationActif)
            ?? throw new BusinessRuleViolationException("Aucune affectation d'actif correspondante n'a été trouvée.");

        affectation.Clore(dateFin);
    }

    internal void AjouterAffectationPoste(AffectationPoste affectation)
    {
        if (affectation is null)
        {
            throw new ArgumentNullException(nameof(affectation));
        }

        if (_affectationsPoste.Contains(affectation))
        {
            return;
        }

        _affectationsPoste.Add(affectation);
    }

    internal void AjouterAffectationActif(AffectationActif affectation)
    {
        if (affectation is null)
        {
            throw new ArgumentNullException(nameof(affectation));
        }

        if (_affectationsActif.Contains(affectation))
        {
            return;
        }

        _affectationsActif.Add(affectation);
    }
}