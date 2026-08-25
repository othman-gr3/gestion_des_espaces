using FluentValidation;
using GestionEspaces.Application.DTOs.Demandes;

namespace GestionEspaces.Application.Validation;

public sealed class RejectDemandeRequestValidator : AbstractValidator<RejectDemandeRequest>
{
    public RejectDemandeRequestValidator()
    {
        RuleFor(request => request.ConcurrencyToken)
            .NotEmpty();

        RuleFor(request => request.Reponse)
            .NotEmpty()
            .MaximumLength(1000);
    }
}
