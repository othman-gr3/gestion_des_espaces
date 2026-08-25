using FluentValidation;
using GestionEspaces.Application.DTOs.Demandes;

namespace GestionEspaces.Application.Validation;

public sealed class ResolveDemandeRequestValidator : AbstractValidator<ResolveDemandeRequest>
{
    public ResolveDemandeRequestValidator()
    {
        RuleFor(request => request.ConcurrencyToken)
            .NotEmpty();

        RuleFor(request => request.Reponse)
            .NotEmpty()
            .MaximumLength(1000);
    }
}
