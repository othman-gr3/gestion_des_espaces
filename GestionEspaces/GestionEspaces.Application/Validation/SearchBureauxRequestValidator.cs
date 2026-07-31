using FluentValidation;
using GestionEspaces.Application.DTOs.Bureaux;

namespace GestionEspaces.Application.Validation;

public sealed class SearchBureauxRequestValidator : AbstractValidator<SearchBureauxRequest>
{
    public SearchBureauxRequestValidator()
    {
        RuleFor(x => x.PageNumber).GreaterThan(0);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 200);
        RuleFor(x => x.IdBatiment).GreaterThan(0).When(x => x.IdBatiment.HasValue);
    }
}