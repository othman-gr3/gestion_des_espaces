using FluentValidation;
using GestionEspaces.Application.Common;
using GestionEspaces.Application.DTOs.Sites;
using GestionEspaces.Application.Interfaces;
using GestionEspaces.Application.Interfaces.Repositories;
using GestionEspaces.Domain.ValueObjects;

namespace GestionEspaces.Application.UseCases;

public sealed class SiteUseCases
{
    private readonly ISiteRepository _siteRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IValidator<CreateSiteRequest> _createValidator;
    private readonly IValidator<UpdateSiteRequest> _updateValidator;
    private readonly IValidator<SearchSitesRequest> _searchValidator;

    public SiteUseCases(
        ISiteRepository siteRepository,
        IUnitOfWork unitOfWork,
        IValidator<CreateSiteRequest> createValidator,
        IValidator<UpdateSiteRequest> updateValidator,
        IValidator<SearchSitesRequest> searchValidator)
    {
        _siteRepository = siteRepository;
        _unitOfWork = unitOfWork;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
        _searchValidator = searchValidator;
    }

    public async Task<Result<SiteDto>> CreateAsync(CreateSiteRequest request, CancellationToken cancellationToken)
    {
        var validation = await _createValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return Result<SiteDto>.Failure(validation.Errors.Select(e => new ErrorDetail("ValidationError", e.ErrorMessage, e.PropertyName)));
        }

        if (await _siteRepository.ExistsByCodeAsync(request.Code, null, cancellationToken))
        {
            return Result<SiteDto>.Failure(new ErrorDetail("DuplicateCode", $"Un site avec le code '{request.Code}' existe déjà.", nameof(request.Code)));
        }

        var site = new Domain.Entities.Site(
            request.Nom,
            request.Code,
            new AdresseSite(request.Rue, request.Ville, request.CodePostal, request.Pays),
            request.Telephone,
            request.Email,
            request.Image);

        await _siteRepository.AddAsync(site, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<SiteDto>.Success(site.ToDto());
    }

    public async Task<Result<SiteDto>> UpdateAsync(int idSite, UpdateSiteRequest request, CancellationToken cancellationToken)
    {
        var validation = await _updateValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return Result<SiteDto>.Failure(validation.Errors.Select(e => new ErrorDetail("ValidationError", e.ErrorMessage, e.PropertyName)));
        }

        byte[] tokenBytes;
        try
        {
            tokenBytes = Convert.FromBase64String(request.ConcurrencyToken);
        }
        catch (FormatException)
        {
            return Result<SiteDto>.Failure(new ErrorDetail("ValidationError", "Le jeton de concurrence est invalide.", nameof(request.ConcurrencyToken)));
        }

        var site = await _siteRepository.GetByIdAsync(idSite, cancellationToken);
        if (site is null)
        {
            return Result<SiteDto>.Failure(new ErrorDetail("SiteNotFound", $"Site {idSite} introuvable."));
        }

        if (await _siteRepository.ExistsByCodeAsync(request.Code, idSite, cancellationToken))
        {
            return Result<SiteDto>.Failure(new ErrorDetail("DuplicateCode", $"Un site avec le code '{request.Code}' existe déjà.", nameof(request.Code)));
        }

        _siteRepository.SetOriginalVersion(site, tokenBytes);

        site.MettreAJour(
            request.Nom,
            request.Code,
            new AdresseSite(request.Rue, request.Ville, request.CodePostal, request.Pays),
            request.Telephone,
            request.Email,
            request.Image);

        _siteRepository.Update(site);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<SiteDto>.Success(site.ToDto());
    }

    public async Task<Result<SiteDto>> GetByIdAsync(int idSite, CancellationToken cancellationToken)
    {
        var site = await _siteRepository.GetByIdAsync(idSite, cancellationToken);
        if (site is null)
        {
            return Result<SiteDto>.Failure(new ErrorDetail("SiteNotFound", $"Site {idSite} introuvable."));
        }

        return Result<SiteDto>.Success(site.ToDto());
    }

    public async Task<Result<PagedResult<SiteDto>>> SearchAsync(SearchSitesRequest request, CancellationToken cancellationToken)
    {
        var validation = await _searchValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return Result<PagedResult<SiteDto>>.Failure(validation.Errors.Select(e => new ErrorDetail("ValidationError", e.ErrorMessage, e.PropertyName)));
        }

        var items = await _siteRepository.SearchAsync(request.SearchText, request.PageNumber, request.PageSize, cancellationToken);
        var totalCount = await _siteRepository.CountAsync(request.SearchText, cancellationToken);

        return Result<PagedResult<SiteDto>>.Success(new PagedResult<SiteDto>(
            items.Select(item => item.ToDto()).ToArray(),
            request.PageNumber,
            request.PageSize,
            totalCount));
    }

    public async Task<Result> DeleteAsync(int idSite, CancellationToken cancellationToken)
    {
        var site = await _siteRepository.GetByIdAsync(idSite, cancellationToken);
        if (site is null)
        {
            return Result.Failure(new ErrorDetail("SiteNotFound", $"Site {idSite} introuvable."));
        }

        // Apply the current rowversion so EF detects a concurrent delete conflict.
        _siteRepository.SetOriginalVersion(site, site.Version);
        _siteRepository.Remove(site);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}