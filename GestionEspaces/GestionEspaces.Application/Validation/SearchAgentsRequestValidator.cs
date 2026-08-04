using FluentValidation;
using GestionEspaces.Application.DTOs.Agents;

namespace GestionEspaces.Application.Validation;

/// <summary>
/// Validates agent search/pagination requests.
/// </summary>
public sealed class SearchAgentsRequestValidator : AbstractValidator<SearchAgentsRequest>
{
    public SearchAgentsRequestValidator()
    {
        RuleFor(x => x.PageNumber).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
    }
}
