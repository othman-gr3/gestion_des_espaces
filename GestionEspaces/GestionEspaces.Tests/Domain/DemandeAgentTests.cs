using GestionEspaces.Domain.Entities;
using GestionEspaces.Domain.Exceptions;

namespace GestionEspaces.Tests.Domain;

public class DemandeAgentTests
{
    [Fact]
    public void Creating_ARequest_StartsAtEnAttenteAndRaisesAnEvent()
    {
        var agent = CreateAgent();

        var demande = new DemandeAgent(agent, TypeDemande.ChangementBureau, "Je souhaite être réaffecté.", new DateTime(2026, 1, 10));

        Assert.Equal(StatutDemande.EnAttente, demande.Statut);
        Assert.Single(demande.GetDomainEvents());
    }

    [Fact]
    public void Creating_WithBlankDescription_Throws()
    {
        var agent = CreateAgent();

        Assert.Throws<ArgumentException>(() => new DemandeAgent(agent, TypeDemande.Autre, "   ", DateTime.UtcNow));
    }

    [Fact]
    public void PrendreEnCharge_FromEnAttente_TransitionsToEnCours()
    {
        var demande = CreateDemande();

        demande.PrendreEnCharge();

        Assert.Equal(StatutDemande.EnCours, demande.Statut);
    }

    [Fact]
    public void PrendreEnCharge_WhenAlreadyResolue_Throws()
    {
        var demande = CreateDemande();
        demande.Resoudre("Traité.", DateTime.UtcNow);

        Assert.Throws<BusinessRuleViolationException>(() => demande.PrendreEnCharge());
    }

    [Fact]
    public void Resoudre_SetsStatutReponseAndDateTraitement()
    {
        var demande = CreateDemande();
        var dateTraitement = new DateTime(2026, 2, 1);

        demande.Resoudre("Bureau réaffecté.", dateTraitement);

        Assert.Equal(StatutDemande.Resolue, demande.Statut);
        Assert.Equal("Bureau réaffecté.", demande.Reponse);
        Assert.Equal(dateTraitement, demande.DateTraitement);
    }

    [Fact]
    public void Resoudre_WithBlankReponse_Throws()
    {
        var demande = CreateDemande();

        Assert.Throws<ArgumentException>(() => demande.Resoudre("  ", DateTime.UtcNow));
    }

    [Fact]
    public void Resoudre_WhenAlreadyClosed_Throws()
    {
        var demande = CreateDemande();
        demande.Rejeter("Hors périmètre.", DateTime.UtcNow);

        Assert.Throws<BusinessRuleViolationException>(() => demande.Resoudre("Trop tard.", DateTime.UtcNow));
    }

    [Fact]
    public void Rejeter_SetsStatutRejeteeAndReponse()
    {
        var demande = CreateDemande();

        demande.Rejeter("Ne relève pas de ce service.", DateTime.UtcNow);

        Assert.Equal(StatutDemande.Rejetee, demande.Statut);
        Assert.Equal("Ne relève pas de ce service.", demande.Reponse);
    }

    private static Agent CreateAgent() => new(
        "Doe",
        "Jane",
        "MAT-DEM-001",
        "jane.doe@gestionespaces.local",
        null,
        null,
        null,
        new DateTime(2025, 1, 1),
        null);

    private static DemandeAgent CreateDemande() =>
        new(CreateAgent(), TypeDemande.ProblemeBureau, "Le chauffage ne fonctionne pas.", new DateTime(2026, 1, 10));
}
