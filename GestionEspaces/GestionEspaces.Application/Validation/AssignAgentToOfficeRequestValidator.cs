using GestionEspaces.Application.DTOs.Assignments;
using FluentValidation;

namespace GestionEspaces.Application.Validation;

/// <summary>
/// Validates office assignment requests.
/// </summary>
public sealed class AssignAgentToOfficeRequestValidator : AbstractValidator<AssignAgentToOfficeRequest>
{
    public AssignAgentToOfficeRequestValidator()
    {
        RuleFor(request => request.AgentId)
            .GreaterThan(0);

        RuleFor(request => request.BureauId)
            .GreaterThan(0);

        RuleFor(request => request.DateAffectation)
            .NotEmpty();

        RuleFor(request => request.Motif)
            .MaximumLength(100)
            .When(request => !string.IsNullOrWhiteSpace(request.Motif));
    }
}