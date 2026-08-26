using GestionEspaces.Domain.Common;
using GestionEspaces.Domain.Events;
using GestionEspaces.Domain.Exceptions;

namespace GestionEspaces.Domain.Entities;

public class AffectationActif : EntityBase
{
    public int IdAffectationActif { get; private set; }
    public int IdAgent { get; private set; }
    public Agent Agent { get; private set; } = null!;
    public int IdActif { get; private set; }
    public Actif Actif { get; private set; } = null!;
    public DateTime DateAffectation { get; private set; }
    public DateTime? DateFin { get; private set; }
    public StatutAffectation Statut { get; private set; } = StatutAffectation.Active;
    public EtatActif? EtatRetour { get; private set; }

    public bool EstActive => Statut == StatutAffectation.Active;

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

    /// <summary>
    /// Closes the assignment. <paramref name="etatRetour"/> records the condition the
    /// asset was returned in for THIS specific handover — kept here (not just applied to
    /// <see cref="Actif.Etat"/>) so that history survives even after the asset's current
    /// state changes again on a later assignment.
    /// </summary>
    public void Clore(DateTime dateFin, EtatActif? etatRetour = null)
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
        Statut = StatutAffectation.Terminee;
        EtatRetour = etatRetour;

        RaiseDomainEvent(new AffectationActifClotureeEvent(IdAffectationActif, IdAgent, IdActif, DateFin.Value));
    }
}