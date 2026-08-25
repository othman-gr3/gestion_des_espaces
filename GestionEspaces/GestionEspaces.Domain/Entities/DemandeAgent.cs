using GestionEspaces.Domain.Common;
using GestionEspaces.Domain.Events;
using GestionEspaces.Domain.Exceptions;

namespace GestionEspaces.Domain.Entities;

public enum TypeDemande
{
    ChangementBureau,
    ProblemeBureau,
    ProblemeActif,
    Autre
}

public enum StatutDemande
{
    EnAttente,
    EnCours,
    Resolue,
    Rejetee
}

/// <summary>
/// A request an agent raises about their own workspace (office change, a problem with
/// their office or an asset) for a Gestionnaire/Administrateur to act on. Its own aggregate
/// root rather than part of <see cref="Agent"/> — it has an independent lifecycle
/// (EnAttente → EnCours → Resolue/Rejetee) driven by whoever handles it, not by the agent.
/// </summary>
public class DemandeAgent : EntityBase
{
    public int IdDemande { get; private set; }
    public byte[] Version { get; private set; } = Array.Empty<byte>();
    public int IdAgent { get; private set; }
    public Agent Agent { get; private set; } = null!;
    public TypeDemande Type { get; private set; }
    public string Description { get; private set; } = string.Empty;
    public StatutDemande Statut { get; private set; } = StatutDemande.EnAttente;
    public DateTime DateCreation { get; private set; }
    public DateTime? DateTraitement { get; private set; }
    public string? Reponse { get; private set; }

    private DemandeAgent()
    {
    }

    public DemandeAgent(Agent agent, TypeDemande type, string description, DateTime dateCreation)
    {
        Agent = agent ?? throw new ArgumentNullException(nameof(agent));

        if (string.IsNullOrWhiteSpace(description))
        {
            throw new ArgumentException("La description est obligatoire.", nameof(description));
        }

        IdAgent = agent.IdAgent;
        Type = type;
        Description = description.Trim();
        DateCreation = dateCreation;
        Statut = StatutDemande.EnAttente;

        // IdDemande isn't assigned by EF until SaveChanges runs — a fixed-at-construction
        // record would capture it as 0 here, so this event deliberately omits it.
        RaiseDomainEvent(new DemandeCreeeEvent(agent.IdAgent, type.ToString(), dateCreation));
    }

    public void PrendreEnCharge()
    {
        if (Statut != StatutDemande.EnAttente)
        {
            throw new BusinessRuleViolationException("Seule une demande en attente peut être prise en charge.");
        }

        Statut = StatutDemande.EnCours;
    }

    public void Resoudre(string reponse, DateTime dateTraitement)
    {
        if (Statut is StatutDemande.Resolue or StatutDemande.Rejetee)
        {
            throw new BusinessRuleViolationException("Cette demande est déjà clôturée.");
        }

        if (string.IsNullOrWhiteSpace(reponse))
        {
            throw new ArgumentException("Une réponse est requise pour résoudre une demande.", nameof(reponse));
        }

        Statut = StatutDemande.Resolue;
        Reponse = reponse.Trim();
        DateTraitement = dateTraitement;

        RaiseDomainEvent(new DemandeResolueEvent(IdDemande, IdAgent, dateTraitement));
    }

    public void Rejeter(string reponse, DateTime dateTraitement)
    {
        if (Statut is StatutDemande.Resolue or StatutDemande.Rejetee)
        {
            throw new BusinessRuleViolationException("Cette demande est déjà clôturée.");
        }

        if (string.IsNullOrWhiteSpace(reponse))
        {
            throw new ArgumentException("Un motif est requis pour rejeter une demande.", nameof(reponse));
        }

        Statut = StatutDemande.Rejetee;
        Reponse = reponse.Trim();
        DateTraitement = dateTraitement;

        RaiseDomainEvent(new DemandeRejeteeEvent(IdDemande, IdAgent, dateTraitement));
    }
}
