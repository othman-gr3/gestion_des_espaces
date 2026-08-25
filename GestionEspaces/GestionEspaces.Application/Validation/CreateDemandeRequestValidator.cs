using FluentValidation;
using GestionEspaces.Application.DTOs.Demandes;

namespace GestionEspaces.Application.Validation;

public sealed class CreateDemandeRequestValidator : AbstractValidator<CreateDemandeRequest>
{
    public CreateDemandeRequestValidator()
    {
        RuleFor(request => request.Type)
            .IsInEnum();

        RuleFor(request => request.Description)
            .NotEmpty()
            .MaximumLength(1000);
    }
}
