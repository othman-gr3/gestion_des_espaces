using FluentValidation;
using GestionEspaces.Application.Common;
using GestionEspaces.Application.DTOs.Actifs;
using GestionEspaces.Application.Interfaces;
using GestionEspaces.Application.Interfaces.Repositories;

namespace GestionEspaces.Application.UseCases;

public sealed class ActifUseCases
{
    private readonly IActifRepository _actifRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IValidator<CreateActifRequest> _createValidator;
    private readonly IValidator<UpdateActifRequest> _updateValidator;
    private readonly IValidator<SearchActifsRequest> _searchValidator;

    public ActifUseCases(
        IActifRepository actifRepository,
        IUnitOfWork unitOfWork,
        IValidator<CreateActifRequest> createValidator,
        IValidator<UpdateActifRequest> updateValidator,
        IValidator<SearchActifsRequest> searchValidator)
    {
        _actifRepository = actifRepository;
        _unitOfWork = unitOfWork;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
        _searchValidator = searchValidator;
    }

    public async Task<Result<ActifDto>> CreateAsync(CreateActifRequest request, CancellationToken cancellationToken)
    {
        var validation = await _createValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return Result<ActifDto>.Failure(validation.Errors.Select(e => new ErrorDetail("ValidationError", e.ErrorMessage, e.PropertyName)));
        }

        if (!string.IsNullOrWhiteSpace(request.NumeroSerie)
            && await _actifRepository.ExistsByNumeroSerieAsync(request.NumeroSerie, null, cancellationToken))
        {
            return Result<ActifDto>.Failure(new ErrorDetail("DuplicateNumeroSerie", $"Le numéro de série '{request.NumeroSerie}' existe déjà."));
        }

        var actif = new Domain.Entities.Actif(
            request.Nom,
            request.Type,
            request.Marque,
            request.Modele,
            request.NumeroSerie,
            request.DateAchat,
            request.Image);

        await _actifRepository.AddAsync(actif, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<ActifDto>.Success(actif.ToDto());
    }

    public async Task<Result<ActifDto>> UpdateAsync(int idActif, UpdateActifRequest request, CancellationToken cancellationToken)
    {
        var validation = await _updateValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return Result<ActifDto>.Failure(validation.Errors.Select(e => new ErrorDetail("ValidationError", e.ErrorMessage, e.PropertyName)));
        }

        byte[] tokenBytes;
        try
        {
            tokenBytes = Convert.FromBase64String(request.ConcurrencyToken);
        }
        catch (FormatException)
        {
            return Result<ActifDto>.Failure(new ErrorDetail("ValidationError", "Le jeton de concurrence est invalide.", nameof(request.ConcurrencyToken)));
        }

        var actif = await _actifRepository.GetByIdAsync(idActif, cancellationToken);
        if (actif is null)
        {
            return Result<ActifDto>.Failure(new ErrorDetail("ActifNotFound", $"Actif {idActif} introuvable."));
        }

        if (!string.IsNullOrWhiteSpace(request.NumeroSerie)
            && await _actifRepository.ExistsByNumeroSerieAsync(request.NumeroSerie, idActif, cancellationToken))
        {
            return Result<ActifDto>.Failure(new ErrorDetail("DuplicateNumeroSerie", $"Le numéro de série '{request.NumeroSerie}' existe déjà."));
        }

        _actifRepository.SetOriginalVersion(actif, tokenBytes);

        actif.MettreAJour(
            request.Nom,
            request.Type,
            request.Marque,
            request.Modele,
            request.NumeroSerie,
            request.DateAchat,
            request.Image,
            request.Etat);

        _actifRepository.Update(actif);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result<ActifDto>.Success(actif.ToDto());
    }

    public async Task<Result<ActifDto>> GetByIdAsync(int idActif, CancellationToken cancellationToken)
    {
        var actif = await _actifRepository.GetByIdAsync(idActif, cancellationToken);
        if (actif is null)
        {
            return Result<ActifDto>.Failure(new ErrorDetail("ActifNotFound", $"Actif {idActif} introuvable."));
        }

        return Result<ActifDto>.Success(actif.ToDto());
    }

    public async Task<Result<PagedResult<ActifDto>>> SearchAsync(SearchActifsRequest request, CancellationToken cancellationToken)
    {
        var validation = await _searchValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return Result<PagedResult<ActifDto>>.Failure(validation.Errors.Select(e => new ErrorDetail("ValidationError", e.ErrorMessage, e.PropertyName)));
        }

        var items = await _actifRepository.SearchAsync(request.SearchText, request.Etat, request.PageNumber, request.PageSize, cancellationToken);
        var totalCount = await _actifRepository.CountAsync(request.SearchText, request.Etat, cancellationToken);

        return Result<PagedResult<ActifDto>>.Success(new PagedResult<ActifDto>(
            items.Select(item => item.ToDto()).ToArray(),
            request.PageNumber,
            request.PageSize,
            totalCount));
    }

    public async Task<Result> DeleteAsync(int idActif, CancellationToken cancellationToken)
    {
        var actif = await _actifRepository.GetByIdAsync(idActif, cancellationToken);
        if (actif is null)
        {
            return Result.Failure(new ErrorDetail("ActifNotFound", $"Actif {idActif} introuvable."));
        }

        _actifRepository.Remove(actif);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}