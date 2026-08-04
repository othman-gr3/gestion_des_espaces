using GestionEspaces.Domain.Exceptions;

namespace GestionEspaces.Domain.Entities;

/// <summary>
/// Represents a temporary booking of a meeting room (bureau) by an agent
/// for a specific time window.  Conflict detection (overlapping bookings on
/// the same bureau) is enforced at the use-case layer before creation.
/// </summary>
public class Reservation
{
    public int IdReservation { get; private set; }
    public byte[] Version { get; private set; } = Array.Empty<byte>();

    public int IdBureau { get; private set; }
    public Bureau Bureau { get; private set; } = null!;

    public int IdAgent { get; private set; }
    public Agent Agent { get; private set; } = null!;

    public DateTime DateDebut { get; private set; }
    public DateTime DateFin { get; private set; }
    public StatutReservation Statut { get; private set; } = StatutReservation.EnAttente;
    public string? Motif { get; private set; }

    private Reservation() { }

    public Reservation(int idBureau, int idAgent, DateTime dateDebut, DateTime dateFin, string? motif)
    {
        if (dateFin <= dateDebut)
            throw new ArgumentException("La date de fin doit être postérieure à la date de début.", nameof(dateFin));

        IdBureau = idBureau;
        IdAgent = idAgent;
        DateDebut = dateDebut.Kind == DateTimeKind.Unspecified
            ? DateTime.SpecifyKind(dateDebut, DateTimeKind.Utc)
            : dateDebut;
        DateFin = dateFin.Kind == DateTimeKind.Unspecified
            ? DateTime.SpecifyKind(dateFin, DateTimeKind.Utc)
            : dateFin;
        Motif = motif;
        Statut = StatutReservation.EnAttente;
    }

    public void Confirmer()
    {
        if (Statut != StatutReservation.EnAttente)
            throw new BusinessRuleViolationException("Seules les réservations en attente peuvent être confirmées.");

        Statut = StatutReservation.Confirmee;
    }

    public void Annuler()
    {
        if (Statut == StatutReservation.Annulee || Statut == StatutReservation.Rejetee)
            throw new BusinessRuleViolationException("La réservation est déjà annulée ou rejetée.");

        Statut = StatutReservation.Annulee;
    }

    public void Rejeter()
    {
        if (Statut != StatutReservation.EnAttente)
            throw new BusinessRuleViolationException("Seules les réservations en attente peuvent être rejetées.");

        Statut = StatutReservation.Rejetee;
    }

    public void MettreAJour(DateTime dateDebut, DateTime dateFin, string? motif)
    {
        if (Statut != StatutReservation.EnAttente)
            throw new BusinessRuleViolationException("Seules les réservations en attente peuvent être modifiées.");

        if (dateFin <= dateDebut)
            throw new ArgumentException("La date de fin doit être postérieure à la date de début.", nameof(dateFin));

        DateDebut = dateDebut.Kind == DateTimeKind.Unspecified
            ? DateTime.SpecifyKind(dateDebut, DateTimeKind.Utc)
            : dateDebut;
        DateFin = dateFin.Kind == DateTimeKind.Unspecified
            ? DateTime.SpecifyKind(dateFin, DateTimeKind.Utc)
            : dateFin;
        Motif = motif;
    }
}
