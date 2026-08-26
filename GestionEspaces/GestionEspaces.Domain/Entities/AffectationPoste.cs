using GestionEspaces.Domain.Common;
using GestionEspaces.Domain.Events;
using GestionEspaces.Domain.Exceptions;

namespace GestionEspaces.Domain.Entities;

public class AffectationPoste : EntityBase
{
    public int IdAffectationPoste { get; private set; }
    public int IdAgent { get; private set; }
    public Agent Agent { get; private set; } = null!;
    public int IdBureau { get; private set; }
    public Bureau Bureau { get; private set; } = null!;
    public DateTime DateAffectation { get; private set; }
    public DateTime? DateFin { get; private set; }
    public string? Motif { get; private set; }
    public StatutAffectation Statut { get; private set; } = StatutAffectation.Active;

    public bool EstActive => Statut == StatutAffectation.Active;

    private AffectationPoste()
    {
    }

    internal AffectationPoste(Agent agent, Bureau bureau, DateTime dateAffectation, string? motif = null)
    {
        Agent = agent ?? throw new ArgumentNullException(nameof(agent));
        Bureau = bureau ?? throw new ArgumentNullException(nameof(bureau));

        if (dateAffectation.Kind == DateTimeKind.Unspecified)
        {
            dateAffectation = DateTime.SpecifyKind(dateAffectation, DateTimeKind.Utc);
        }

        DateAffectation = dateAffectation;
        IdAgent = agent.IdAgent;
        IdBureau = bureau.IdBureau;
        Motif = motif;
    }

    public void Clore(DateTime dateFin)
    {
        if (!EstActive)
        {
            throw new BusinessRuleViolationException("L'affectation de poste est déjà clôturée.");
        }

        if (dateFin < DateAffectation)
        {
            throw new BusinessRuleViolationException("La date de fin ne peut pas être antérieure à la date d'affectation.");
        }

        DateFin = dateFin.Kind == DateTimeKind.Unspecified
            ? DateTime.SpecifyKind(dateFin, DateTimeKind.Utc)
            : dateFin;
        Statut = StatutAffectation.Terminee;

        RaiseDomainEvent(new AffectationPosteClotureeEvent(IdAffectationPoste, IdAgent, IdBureau, DateFin.Value));
    }
}