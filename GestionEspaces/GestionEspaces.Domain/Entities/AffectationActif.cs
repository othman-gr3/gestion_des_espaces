using GestionEspaces.Domain.Exceptions;

namespace GestionEspaces.Domain.Entities;

public class AffectationActif
{
    public int IdAffectationActif { get; private set; }
    public int IdAgent { get; private set; }
    public Agent Agent { get; private set; } = null!;
    public int IdActif { get; private set; }
    public Actif Actif { get; private set; } = null!;
    public DateTime DateAffectation { get; private set; }
    public DateTime? DateFin { get; private set; }

    public bool EstActive => DateFin is null;

    private AffectationActif()
    {
    }

    internal AffectationActif(Agent agent, Actif actif, DateTime dateAffectation)
    {
        Agent = agent ?? throw new ArgumentNullException(nameof(agent));
        Actif = actif ?? throw new ArgumentNullException(nameof(actif));

        if (dateAffectation.Kind == DateTimeKind.Unspecified)
        {
            dateAffectation = DateTime.SpecifyKind(dateAffectation, DateTimeKind.Utc);
        }

        DateAffectation = dateAffectation;
        IdAgent = agent.IdAgent;
        IdActif = actif.IdActif;
    }

    public void Clore(DateTime dateFin)
    {
        if (!EstActive)
        {
            throw new BusinessRuleViolationException("L'affectation de l'actif est déjà clôturée.");
        }

        if (dateFin < DateAffectation)
        {
            throw new BusinessRuleViolationException("La date de fin ne peut pas être antérieure à la date d'affectation.");
        }

        DateFin = dateFin.Kind == DateTimeKind.Unspecified
            ? DateTime.SpecifyKind(dateFin, DateTimeKind.Utc)
            : dateFin;
    }
}