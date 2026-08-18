using FluentValidation;
using GestionEspaces.Application.Common;
using GestionEspaces.Application.DTOs.Bureaux;
using GestionEspaces.Application.Interfaces;
using GestionEspaces.Application.Interfaces.Repositories;

namespace GestionEspaces.Application.UseCases;

public sealed class BureauUseCases
{
    private readonly IBureauRepository _bureauRepository;
    private readonly IBatimentRepository _batimentRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IValidator<CreateBureauRequest> _createValidator;
    private readonly IValidator<UpdateBureauRequest> _updateValidator;
    private readonly IValidator<SearchBureauxRequest> _searchValidator;

    public BureauUseCases(
        IBureauRepository bureauRepository,
        IBatimentRepository batimentRepository,
        IUnitOfWork unitOfWork,
        IValidator<CreateBureauRequest> createValidator,
        IValidator<UpdateBureauRequest> updateValidator,
        IValidator<SearchBureauxRequest> searchValidator)
    {
        _bureauRepository = bureauRepository;
        _batimentRepository = batimentRepository;
        _unitOfWork = unitOfWork;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
        _searchValidator = searchValidator;
    }

    public async Task<Result<BureauDto>> CreateAsync(CreateBureauRequest request, CancellationToken cancellationToken)
    {
        var validation = await _createValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return Result<BureauDto>.Failure(validation.Errors.Select(e => new ErrorDetail("ValidationError", e.ErrorMessage, e.PropertyName)));
        }

        if (await _batimentRepository.GetByIdAsync(request.IdBatiment, cancellationToken) is null)
        {
            return Result<BureauDto>.Failure(new ErrorDetail("BatimentNotFound", $"Bâtiment {request.IdBatiment} introuvable."));
        }

        if (await _bureauRepository.ExistsByNumeroAsync(request.IdBatiment, request.Numero, null, cancellationToken))
        {
            return Result<BureauDto>.Failure(new ErrorDetail("DuplicateBureauNumber", $"Le numéro '{request.Numero}' existe déjà pour ce bâtiment."));
        }

        var bureau = new Domain.Entities.Bureau(
            request.Numero,
            request.Type,
            request.Capacite,
            request.Superficie,
            request.Etage,
            request.Image,
            request.IdBatiment);

        await _bureauRepository.AddAsync(bureau, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<BureauDto>.Success(bureau.ToDto());
    }

    public async Task<Result<BureauDto>> UpdateAsync(int idBureau, UpdateBureauRequest request, CancellationToken cancellationToken)
    {
        var validation = await _updateValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return Result<BureauDto>.Failure(validation.Errors.Select(e => new ErrorDetail("ValidationError", e.ErrorMessage, e.PropertyName)));
        }

        byte[] tokenBytes;
        try
        {
            tokenBytes = Convert.FromBase64String(request.ConcurrencyToken);
        }
        catch (FormatException)
        {
            return Result<BureauDto>.Failure(new ErrorDetail("ValidationError", "Le jeton de concurrence est invalide.", nameof(request.ConcurrencyToken)));
        }

        var bureau = await _bureauRepository.GetByIdAsync(idBureau, cancellationToken);
        if (bureau is null)
        {
            return Result<BureauDto>.Failure(new ErrorDetail("BureauNotFound", $"Bureau {idBureau} introuvable."));
        }

        if (await _batimentRepository.GetByIdAsync(request.IdBatiment, cancellationToken) is null)
        {
            return Result<BureauDto>.Failure(new ErrorDetail("BatimentNotFound", $"Bâtiment {request.IdBatiment} introuvable."));
        }

        if (await _bureauRepository.ExistsByNumeroAsync(request.IdBatiment, request.Numero, idBureau, cancellationToken))
        {
            return Result<BureauDto>.Failure(new ErrorDetail("DuplicateBureauNumber", $"Le numéro '{request.Numero}' existe déjà pour ce bâtiment."));
        }

        _bureauRepository.SetOriginalVersion(bureau, tokenBytes);

        bureau.MettreAJour(
            request.Numero,
            request.Type,
            request.Capacite,
            request.Superficie,
            request.Etage,
            request.Image,
            request.IdBatiment);

        switch (request.Statut)
        {
            case Domain.Entities.StatutBureau.Disponible:
                bureau.RemettreEnService();
                break;
            case Domain.Entities.StatutBureau.EnMaintenance:
                bureau.MettreEnMaintenance();
                break;
            // Occupe is derived automatically from an active AffectationPoste
            // (see Agent.AffecterAuBureau / CloseAffectationPosteUseCase) and is not
            // directly settable through the referentiel edit form.
        }

        _bureauRepository.Update(bureau);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result<BureauDto>.Success(bureau.ToDto());
    }

    public async Task<Result<BureauDto>> GetByIdAsync(int idBureau, CancellationToken cancellationToken)
    {
        var bureau = await _bureauRepository.GetByIdAsync(idBureau, cancellationToken);
        if (bureau is null)
        {
            return Result<BureauDto>.Failure(new ErrorDetail("BureauNotFound", $"Bureau {idBureau} introuvable."));
        }

        return Result<BureauDto>.Success(bureau.ToDto());
    }

    public async Task<Result<PagedResult<BureauDto>>> SearchAsync(SearchBureauxRequest request, CancellationToken cancellationToken)
    {
        var validation = await _searchValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return Result<PagedResult<BureauDto>>.Failure(validation.Errors.Select(e => new ErrorDetail("ValidationError", e.ErrorMessage, e.PropertyName)));
        }

        var items = await _bureauRepository.SearchAsync(request.IdBatiment, request.SearchText, request.Statut, request.PageNumber, request.PageSize, cancellationToken);
        var totalCount = await _bureauRepository.CountAsync(request.IdBatiment, request.SearchText, request.Statut, cancellationToken);

        return Result<PagedResult<BureauDto>>.Success(new PagedResult<BureauDto>(
            items.Select(item => item.ToDto()).ToArray(),
            request.PageNumber,
            request.PageSize,
            totalCount));
    }

    public async Task<Result> DeleteAsync(int idBureau, CancellationToken cancellationToken)
    {
        var bureau = await _bureauRepository.GetByIdAsync(idBureau, cancellationToken);
        if (bureau is null)
        {
            return Result.Failure(new ErrorDetail("BureauNotFound", $"Bureau {idBureau} introuvable."));
        }

        _bureauRepository.SetOriginalVersion(bureau, bureau.Version);
        _bureauRepository.Remove(bureau);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}