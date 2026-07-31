using GestionEspaces.Domain.Entities;
using GestionEspaces.Domain.Exceptions;
using GestionEspaces.Domain.ValueObjects;

namespace GestionEspaces.Tests.Domain;

public class AssignmentRulesTests
{
    [Fact]
    public void Agent_CanBeAssignedToAnAvailableOffice()
    {
        var agent = CreateAgent();
        var bureau = CreateBureau();

        var affectation = agent.AffecterAuBureau(bureau, new DateTime(2026, 1, 10, 8, 0, 0, DateTimeKind.Utc));

        Assert.True(affectation.EstActive);
        Assert.Single(agent.AffectationsPoste);
        Assert.Single(bureau.Affectations);
    }

    [Fact]
    public void Agent_CannotReceiveTwoActiveOfficeAssignments()
    {
        var agent = CreateAgent();
        var bureau = CreateBureau();
        var secondBureau = CreateSecondBureau();

        agent.AffecterAuBureau(bureau, new DateTime(2026, 1, 10, 8, 0, 0, DateTimeKind.Utc));

        var exception = Assert.Throws<BusinessRuleViolationException>(() =>
            agent.AffecterAuBureau(secondBureau, new DateTime(2026, 1, 11, 8, 0, 0, DateTimeKind.Utc)));

        Assert.Contains("déjà une affectation de poste active", exception.Message);
    }

    [Fact]
    public void Actif_CannotBeAssignedTwiceAtTheSameTime()
    {
        var agent = CreateAgent();
        var secondAgent = CreateSecondAgent();
        var actif = CreateActif();

        actif.AffecterA(agent, new DateTime(2026, 1, 10, 8, 0, 0, DateTimeKind.Utc));

        var exception = Assert.Throws<BusinessRuleViolationException>(() =>
            actif.AffecterA(secondAgent, new DateTime(2026, 1, 11, 8, 0, 0, DateTimeKind.Utc)));

        Assert.Contains("déjà une affectation active", exception.Message);
    }

    [Fact]
    public void ClosedOfficeAssignment_AllowsNewAssignment()
    {
        var agent = CreateAgent();
        var bureau = CreateBureau();
        var secondBureau = CreateSecondBureau();

        var affectation = agent.AffecterAuBureau(bureau, new DateTime(2026, 1, 10, 8, 0, 0, DateTimeKind.Utc));
        affectation.Clore(new DateTime(2026, 1, 20, 18, 0, 0, DateTimeKind.Utc));

        var secondAffectation = agent.AffecterAuBureau(secondBureau, new DateTime(2026, 1, 21, 8, 0, 0, DateTimeKind.Utc));

        Assert.False(affectation.EstActive);
        Assert.True(secondAffectation.EstActive);
        Assert.Equal(2, agent.AffectationsPoste.Count);
    }

    private static Agent CreateAgent() => new(
        "Doe",
        "Jane",
        "MAT-001",
        "jane.doe@gestionespaces.local",
        "+33 6 11 22 33 44",
        "Responsable",
        "IT",
        new DateTime(2025, 1, 1),
        null);

    private static Agent CreateSecondAgent() => new(
        "Smith",
        "John",
        "MAT-002",
        "john.smith@gestionespaces.local",
        "+33 6 55 66 77 88",
        "Technicien",
        "Support",
        new DateTime(2025, 2, 1),
        null);

    private static Bureau CreateBureau() => new(
        "B-101",
        "Individuel",
        1,
        18.5f,
        1,
        null,
        1);

    private static Bureau CreateSecondBureau() => new(
        "B-102",
        "Individuel",
        1,
        19.0f,
        1,
        null,
        1);

    private static Actif CreateActif() => new(
        "Ordinateur portable",
        "Informatique",
        "Dell",
        "Latitude",
        "SN-0001",
        new DateTime(2025, 3, 1),
        null);
}