using GestionEspaces.Application.DTOs.Assignments;
using FluentValidation;

namespace GestionEspaces.Application.Validation;

/// <summary>
/// Validates asset assignment requests.
/// </summary>
public sealed class AssignAssetToAgentRequestValidator : AbstractValidator<AssignAssetToAgentRequest>
{
    public AssignAssetToAgentRequestValidator()
    {
        RuleFor(request => request.AgentId)
            .GreaterThan(0);

        RuleFor(request => request.ActifId)
            .GreaterThan(0);

        RuleFor(request => request.DateAffectation)
            .NotEmpty();
    }
}