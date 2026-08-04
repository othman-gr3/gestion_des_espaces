using FluentValidation;
using GestionEspaces.Application.Common;
using GestionEspaces.Application.DTOs.Reservations;
using GestionEspaces.Application.Interfaces;
using GestionEspaces.Application.Interfaces.Repositories;
using GestionEspaces.Domain.Entities;
using GestionEspaces.Domain.Exceptions;

namespace GestionEspaces.Application.UseCases;

/// <summary>
/// Full CRUD + workflow use cases for reservations.
/// </summary>
public sealed class ReservationUseCases
{
    private readonly IReservationRepository _reservationRepository;
    private readonly IBureauRepository _bureauRepository;
    private readonly IAgentRepository _agentRepository;
    private readonly IUnitOfWork _unitOfWork;

    public ReservationUseCases(
        IReservationRepository reservationRepository,
        IBureauRepository bureauRepository,
        IAgentRepository agentRepository,
        IUnitOfWork unitOfWork)
    {
        _reservationRepository = reservationRepository;
        _bureauRepository = bureauRepository;
        _agentRepository = agentRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<ReservationDto>> CreateAsync(int agentId, CreateReservationRequest request, CancellationToken cancellationToken)
    {
        if (request.DateFin <= request.DateDebut)
            return Fail("ValidationError", "La date de fin doit être postérieure à la date de début.");

        if (await _agentRepository.GetByIdAsync(agentId, cancellationToken) is null)
            return Fail("AgentNotFound", $"Agent {agentId} introuvable.");

        if (await _bureauRepository.GetByIdAsync(request.BureauId, cancellationToken) is null)
            return Fail("BureauNotFound", $"Bureau {request.BureauId} introuvable.");

        if (await _reservationRepository.HasOverlapAsync(request.BureauId, request.DateDebut, request.DateFin, null, cancellationToken))
            return Fail("ReservationConflict", "Ce bureau a déjà une réservation active sur ce créneau.");

        var reservation = new Reservation(request.BureauId, agentId, request.DateDebut, request.DateFin, request.Motif);
        await _reservationRepository.AddAsync(reservation, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<ReservationDto>.Success(reservation.ToDto());
    }

    public async Task<Result<ReservationDto>> UpdateAsync(int idReservation, UpdateReservationRequest request, CancellationToken cancellationToken)
    {
        if (request.DateFin <= request.DateDebut)
            return Fail("ValidationError", "La date de fin doit être postérieure à la date de début.");

        byte[] tokenBytes;
        try { tokenBytes = Convert.FromBase64String(request.ConcurrencyToken); }
        catch (FormatException) { return Fail("ValidationError", "Le jeton de concurrence est invalide."); }

        var reservation = await _reservationRepository.GetByIdAsync(idReservation, cancellationToken);
        if (reservation is null) return Fail("ReservationNotFound", $"Réservation {idReservation} introuvable.");

        if (await _reservationRepository.HasOverlapAsync(reservation.IdBureau, request.DateDebut, request.DateFin, idReservation, cancellationToken))
            return Fail("ReservationConflict", "Ce bureau a déjà une réservation active sur ce créneau.");

        _reservationRepository.SetOriginalVersion(reservation, tokenBytes);

        try { reservation.MettreAJour(request.DateDebut, request.DateFin, request.Motif); }
        catch (BusinessRuleViolationException ex) { return Fail("BusinessRuleViolation", ex.Message); }

        _reservationRepository.Update(reservation);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result<ReservationDto>.Success(reservation.ToDto());
    }

    public async Task<Result<ReservationDto>> GetByIdAsync(int idReservation, CancellationToken cancellationToken)
    {
        var reservation = await _reservationRepository.GetByIdAsync(idReservation, cancellationToken);
        if (reservation is null) return Fail("ReservationNotFound", $"Réservation {idReservation} introuvable.");
        return Result<ReservationDto>.Success(reservation.ToDto());
    }

    public async Task<Result<PagedResult<ReservationDto>>> SearchAsync(SearchReservationsRequest request, CancellationToken cancellationToken)
    {
        var items = await _reservationRepository.SearchAsync(
            request.BureauId, request.AgentId, request.From, request.To, request.Statut,
            request.PageNumber, request.PageSize, cancellationToken);
        var totalCount = await _reservationRepository.CountAsync(
            request.BureauId, request.AgentId, request.From, request.To, request.Statut, cancellationToken);

        return Result<PagedResult<ReservationDto>>.Success(new PagedResult<ReservationDto>(
            items.Select(r => r.ToDto()).ToArray(), request.PageNumber, request.PageSize, totalCount));
    }

    public async Task<Result<ReservationDto>> ConfirmerAsync(int idReservation, string concurrencyToken, CancellationToken cancellationToken)
        => await TransitionAsync(idReservation, concurrencyToken, r => r.Confirmer(), cancellationToken);

    public async Task<Result<ReservationDto>> AnnulerAsync(int idReservation, string concurrencyToken, CancellationToken cancellationToken)
        => await TransitionAsync(idReservation, concurrencyToken, r => r.Annuler(), cancellationToken);

    public async Task<Result<ReservationDto>> RejeterAsync(int idReservation, string concurrencyToken, CancellationToken cancellationToken)
        => await TransitionAsync(idReservation, concurrencyToken, r => r.Rejeter(), cancellationToken);

    // ── Private ────────────────────────────────────────────────────────────────

    private async Task<Result<ReservationDto>> TransitionAsync(
        int idReservation, string concurrencyToken,
        Action<Reservation> transition, CancellationToken cancellationToken)
    {
        byte[] tokenBytes;
        try { tokenBytes = Convert.FromBase64String(concurrencyToken); }
        catch (FormatException) { return Fail("ValidationError", "Le jeton de concurrence est invalide."); }

        var reservation = await _reservationRepository.GetByIdAsync(idReservation, cancellationToken);
        if (reservation is null) return Fail("ReservationNotFound", $"Réservation {idReservation} introuvable.");

        _reservationRepository.SetOriginalVersion(reservation, tokenBytes);

        try { transition(reservation); }
        catch (BusinessRuleViolationException ex) { return Fail("BusinessRuleViolation", ex.Message); }

        _reservationRepository.Update(reservation);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result<ReservationDto>.Success(reservation.ToDto());
    }

    private static Result<ReservationDto> Fail(string code, string message)
        => Result<ReservationDto>.Failure(new ErrorDetail(code, message));
}
