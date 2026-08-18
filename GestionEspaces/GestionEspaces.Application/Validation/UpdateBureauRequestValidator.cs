using FluentValidation;
using GestionEspaces.Application.DTOs.Bureaux;

namespace GestionEspaces.Application.Validation;

public sealed class UpdateBureauRequestValidator : AbstractValidator<UpdateBureauRequest>
{
    public UpdateBureauRequestValidator()
    {
        RuleFor(x => x.ConcurrencyToken).NotEmpty();
        RuleFor(x => x.Numero).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Capacite).GreaterThan(0);
        RuleFor(x => x.IdBatiment).GreaterThan(0);
        RuleFor(x => x.Image).MaximumLength(500).When(x => !string.IsNullOrWhiteSpace(x.Image));
    }
}