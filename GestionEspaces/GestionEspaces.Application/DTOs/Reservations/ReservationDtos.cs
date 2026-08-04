using GestionEspaces.Domain.Entities;

namespace GestionEspaces.Application.DTOs.Reservations;

/// <summary>Represents a reservation in API responses.</summary>
public sealed record ReservationDto(
    int IdReservation,
    string? ConcurrencyToken,
    int IdBureau,
    int IdAgent,
    DateTime DateDebut,
    DateTime DateFin,
    string Statut,
    string? Motif);

/// <summary>Request payload for creating a reservation.</summary>
public sealed record CreateReservationRequest(
    int BureauId,
    DateTime DateDebut,
    DateTime DateFin,
    string? Motif);

/// <summary>Request payload for updating date/motif on a pending reservation.</summary>
public sealed record UpdateReservationRequest(
    string ConcurrencyToken,
    DateTime DateDebut,
    DateTime DateFin,
    string? Motif);

/// <summary>Request to search reservations by bureau and/or date range.</summary>
public sealed record SearchReservationsRequest(
    int? BureauId,
    int? AgentId,
    DateTime? From,
    DateTime? To,
    StatutReservation? Statut,
    int PageNumber,
    int PageSize);
