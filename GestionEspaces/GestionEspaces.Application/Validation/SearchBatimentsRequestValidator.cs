using FluentValidation;
using GestionEspaces.Application.DTOs.Batiments;

namespace GestionEspaces.Application.Validation;

public sealed class SearchBatimentsRequestValidator : AbstractValidator<SearchBatimentsRequest>
{
    public SearchBatimentsRequestValidator()
    {
        RuleFor(x => x.PageNumber).GreaterThan(0);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 200);
        RuleFor(x => x.IdSite).GreaterThan(0).When(x => x.IdSite.HasValue);
    }
}