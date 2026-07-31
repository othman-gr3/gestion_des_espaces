using FluentValidation;
using GestionEspaces.Application.DTOs.Sites;

namespace GestionEspaces.Application.Validation;

public sealed class SearchSitesRequestValidator : AbstractValidator<SearchSitesRequest>
{
    public SearchSitesRequestValidator()
    {
        RuleFor(x => x.PageNumber).GreaterThan(0);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 200);
    }
}