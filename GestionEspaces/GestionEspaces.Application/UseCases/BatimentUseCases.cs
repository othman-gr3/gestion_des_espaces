using FluentValidation;
using GestionEspaces.Application.Common;
using GestionEspaces.Application.DTOs.Batiments;
using GestionEspaces.Application.Interfaces;
using GestionEspaces.Application.Interfaces.Repositories;

namespace GestionEspaces.Application.UseCases;

public sealed class BatimentUseCases
{
    private readonly IBatimentRepository _batimentRepository;
    private readonly ISiteRepository _siteRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IValidator<CreateBatimentRequest> _createValidator;
    private readonly IValidator<UpdateBatimentRequest> _updateValidator;
    private readonly IValidator<SearchBatimentsRequest> _searchValidator;

    public BatimentUseCases(
        IBatimentRepository batimentRepository,
        ISiteRepository siteRepository,
        IUnitOfWork unitOfWork,
        IValidator<CreateBatimentRequest> createValidator,
        IValidator<UpdateBatimentRequest> updateValidator,
        IValidator<SearchBatimentsRequest> searchValidator)
    {
        _batimentRepository = batimentRepository;
        _siteRepository = siteRepository;
        _unitOfWork = unitOfWork;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
        _searchValidator = searchValidator;
    }

    public async Task<Result<BatimentDto>> CreateAsync(CreateBatimentRequest request, CancellationToken cancellationToken)
    {
        var validation = await _createValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return Result<BatimentDto>.Failure(validation.Errors.Select(e => new ErrorDetail("ValidationError", e.ErrorMessage, e.PropertyName)));
        }

        if (await _siteRepository.GetByIdAsync(request.IdSite, cancellationToken) is null)
        {
            return Result<BatimentDto>.Failure(new ErrorDetail("SiteNotFound", $"Site {request.IdSite} introuvable."));
        }

        if (await _batimentRepository.ExistsByNameForSiteAsync(request.IdSite, request.Nom, null, cancellationToken))
        {
            return Result<BatimentDto>.Failure(new ErrorDetail("DuplicateBatimentName", $"Un bâtiment nommé '{request.Nom}' existe déjà pour ce site."));
        }

        var batiment = new Domain.Entities.Batiment(
            request.Nom,
            request.NombreEtages,
            request.Superficie,
            request.Image,
            request.IdSite);

        await _batimentRepository.AddAsync(batiment, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<BatimentDto>.Success(batiment.ToDto());
    }

    public async Task<Result<BatimentDto>> UpdateAsync(int idBatiment, UpdateBatimentRequest request, CancellationToken cancellationToken)
    {
        var validation = await _updateValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return Result<BatimentDto>.Failure(validation.Errors.Select(e => new ErrorDetail("ValidationError", e.ErrorMessage, e.PropertyName)));
        }

        byte[] tokenBytes;
        try
        {
            tokenBytes = Convert.FromBase64String(request.ConcurrencyToken);
        }
        catch (FormatException)
        {
            return Result<BatimentDto>.Failure(new ErrorDetail("ValidationError", "Le jeton de concurrence est invalide.", nameof(request.ConcurrencyToken)));
        }

        var batiment = await _batimentRepository.GetByIdAsync(idBatiment, cancellationToken);
        if (batiment is null)
        {
            return Result<BatimentDto>.Failure(new ErrorDetail("BatimentNotFound", $"Bâtiment {idBatiment} introuvable."));
        }

        if (await _siteRepository.GetByIdAsync(request.IdSite, cancellationToken) is null)
        {
            return Result<BatimentDto>.Failure(new ErrorDetail("SiteNotFound", $"Site {request.IdSite} introuvable."));
        }

        if (await _batimentRepository.ExistsByNameForSiteAsync(request.IdSite, request.Nom, idBatiment, cancellationToken))
        {
            return Result<BatimentDto>.Failure(new ErrorDetail("DuplicateBatimentName", $"Un bâtiment nommé '{request.Nom}' existe déjà pour ce site."));
        }

        _batimentRepository.SetOriginalVersion(batiment, tokenBytes);

        batiment.MettreAJour(request.Nom, request.NombreEtages, request.Superficie, request.Image, request.IdSite);
        _batimentRepository.Update(batiment);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<BatimentDto>.Success(batiment.ToDto());
    }

    public async Task<Result<BatimentDto>> GetByIdAsync(int idBatiment, CancellationToken cancellationToken)
    {
        var batiment = await _batimentRepository.GetByIdAsync(idBatiment, cancellationToken);
        if (batiment is null)
        {
            return Result<BatimentDto>.Failure(new ErrorDetail("BatimentNotFound", $"Bâtiment {idBatiment} introuvable."));
        }

        return Result<BatimentDto>.Success(batiment.ToDto());
    }

    public async Task<Result<PagedResult<BatimentDto>>> SearchAsync(SearchBatimentsRequest request, CancellationToken cancellationToken)
    {
        var validation = await _searchValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return Result<PagedResult<BatimentDto>>.Failure(validation.Errors.Select(e => new ErrorDetail("ValidationError", e.ErrorMessage, e.PropertyName)));
        }

        var items = await _batimentRepository.SearchAsync(request.IdSite, request.SearchText, request.PageNumber, request.PageSize, cancellationToken);
        var totalCount = await _batimentRepository.CountAsync(request.IdSite, request.SearchText, cancellationToken);

        return Result<PagedResult<BatimentDto>>.Success(new PagedResult<BatimentDto>(
            items.Select(item => item.ToDto()).ToArray(),
            request.PageNumber,
            request.PageSize,
            totalCount));
    }

    public async Task<Result> DeleteAsync(int idBatiment, CancellationToken cancellationToken)
    {
        var batiment = await _batimentRepository.GetByIdAsync(idBatiment, cancellationToken);
        if (batiment is null)
        {
            return Result.Failure(new ErrorDetail("BatimentNotFound", $"Bâtiment {idBatiment} introuvable."));
        }

        _batimentRepository.SetOriginalVersion(batiment, batiment.Version);
        _batimentRepository.Remove(batiment);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}