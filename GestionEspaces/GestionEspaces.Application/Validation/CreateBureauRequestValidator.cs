using FluentValidation;
using GestionEspaces.Application.DTOs.Bureaux;

namespace GestionEspaces.Application.Validation;

public sealed class CreateBureauRequestValidator : AbstractValidator<CreateBureauRequest>
{
    public CreateBureauRequestValidator()
    {
        RuleFor(x => x.Numero).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Capacite).GreaterThan(0);
        RuleFor(x => x.IdBatiment).GreaterThan(0);
        RuleFor(x => x.Image).MaximumLength(500).When(x => !string.IsNullOrWhiteSpace(x.Image));
    }
}