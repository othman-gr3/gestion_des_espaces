using FluentValidation;
using GestionEspaces.Application.Common;
using GestionEspaces.Application.DTOs.Demandes;
using GestionEspaces.Application.Interfaces;
using GestionEspaces.Application.Interfaces.Repositories;
using GestionEspaces.Domain.Entities;

namespace GestionEspaces.Application.UseCases;

/// <summary>
/// Administrateur/Gestionnaire side of the agent-request workflow: list requests and
/// move them through EnAttente → EnCours → Resolue/Rejetee.
/// </summary>
public sealed class DemandeUseCases
{
    private readonly IDemandeRepository _demandeRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IValidator<ResolveDemandeRequest> _resolveValidator;
    private readonly IValidator<RejectDemandeRequest> _rejectValidator;

    public DemandeUseCases(
        IDemandeRepository demandeRepository,
        IUnitOfWork unitOfWork,
        IValidator<ResolveDemandeRequest> resolveValidator,
        IValidator<RejectDemandeRequest> rejectValidator)
    {
        _demandeRepository = demandeRepository;
        _unitOfWork = unitOfWork;
        _resolveValidator = resolveValidator;
        _rejectValidator = rejectValidator;
    }

    public async Task<Result<PagedResult<DemandeDto>>> SearchAsync(SearchDemandesRequest request, CancellationToken cancellationToken)
    {
        var items = await _demandeRepository.SearchAsync(request.Statut, request.PageNumber, request.PageSize, cancellationToken);
        var totalCount = await _demandeRepository.CountAsync(request.Statut, cancellationToken);

        return Result<PagedResult<DemandeDto>>.Success(new PagedResult<DemandeDto>(
            items.Select(demande => demande.ToDto()).ToArray(),
            request.PageNumber,
            request.PageSize,
            totalCount));
    }

    public async Task<Result<DemandeDto>> PrendreEnChargeAsync(int idDemande, TakeChargeDemandeRequest request, CancellationToken cancellationToken)
    {
        byte[] tokenBytes;
        try
        {
            tokenBytes = Convert.FromBase64String(request.ConcurrencyToken);
        }
        catch (FormatException)
        {
            return Result<DemandeDto>.Failure(new ErrorDetail("ValidationError", "Le jeton de concurrence est invalide.", nameof(request.ConcurrencyToken)));
        }

        var demande = await _demandeRepository.GetByIdAsync(idDemande, cancellationToken);
        if (demande is null)
        {
            return Result<DemandeDto>.Failure(new ErrorDetail("DemandeNotFound", $"Demande {idDemande} introuvable."));
        }

        _demandeRepository.SetOriginalVersion(demande, tokenBytes);
        demande.PrendreEnCharge();

        _demandeRepository.Update(demande);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result<DemandeDto>.Success(demande.ToDto());
    }

    public async Task<Result<DemandeDto>> ResoudreAsync(int idDemande, ResolveDemandeRequest request, CancellationToken cancellationToken)
    {
        var validation = await _resolveValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return Result<DemandeDto>.Failure(validation.Errors.Select(e => new ErrorDetail("ValidationError", e.ErrorMessage, e.PropertyName)));
        }

        byte[] tokenBytes;
        try
        {
            tokenBytes = Convert.FromBase64String(request.ConcurrencyToken);
        }
        catch (FormatException)
        {
            return Result<DemandeDto>.Failure(new ErrorDetail("ValidationError", "Le jeton de concurrence est invalide.", nameof(request.ConcurrencyToken)));
        }

        var demande = await _demandeRepository.GetByIdAsync(idDemande, cancellationToken);
        if (demande is null)
        {
            return Result<DemandeDto>.Failure(new ErrorDetail("DemandeNotFound", $"Demande {idDemande} introuvable."));
        }

        _demandeRepository.SetOriginalVersion(demande, tokenBytes);
        demande.Resoudre(request.Reponse, DateTime.UtcNow);

        _demandeRepository.Update(demande);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result<DemandeDto>.Success(demande.ToDto());
    }

    public async Task<Result<DemandeDto>> RejeterAsync(int idDemande, RejectDemandeRequest request, CancellationToken cancellationToken)
    {
        var validation = await _rejectValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return Result<DemandeDto>.Failure(validation.Errors.Select(e => new ErrorDetail("ValidationError", e.ErrorMessage, e.PropertyName)));
        }

        byte[] tokenBytes;
        try
        {
            tokenBytes = Convert.FromBase64String(request.ConcurrencyToken);
        }
        catch (FormatException)
        {
            return Result<DemandeDto>.Failure(new ErrorDetail("ValidationError", "Le jeton de concurrence est invalide.", nameof(request.ConcurrencyToken)));
        }

        var demande = await _demandeRepository.GetByIdAsync(idDemande, cancellationToken);
        if (demande is null)
        {
            return Result<DemandeDto>.Failure(new ErrorDetail("DemandeNotFound", $"Demande {idDemande} introuvable."));
        }

        _demandeRepository.SetOriginalVersion(demande, tokenBytes);
        demande.Rejeter(request.Reponse, DateTime.UtcNow);

        _demandeRepository.Update(demande);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result<DemandeDto>.Success(demande.ToDto());
    }
}
