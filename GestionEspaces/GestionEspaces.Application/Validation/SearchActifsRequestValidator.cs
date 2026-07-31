using FluentValidation;
using GestionEspaces.Application.DTOs.Actifs;

namespace GestionEspaces.Application.Validation;

public sealed class SearchActifsRequestValidator : AbstractValidator<SearchActifsRequest>
{
    public SearchActifsRequestValidator()
    {
        RuleFor(x => x.PageNumber).GreaterThan(0);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 200);
    }
}