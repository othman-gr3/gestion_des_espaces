using FluentValidation;
using GestionEspaces.Application.DTOs.Actifs;

namespace GestionEspaces.Application.Validation;

public sealed class UpdateActifRequestValidator : AbstractValidator<UpdateActifRequest>
{
    public UpdateActifRequestValidator()
    {
        RuleFor(x => x.ConcurrencyToken).NotEmpty();
        RuleFor(x => x.Nom).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Type).MaximumLength(100).When(x => !string.IsNullOrWhiteSpace(x.Type));
        RuleFor(x => x.Marque).MaximumLength(100).When(x => !string.IsNullOrWhiteSpace(x.Marque));
        RuleFor(x => x.Modele).MaximumLength(100).When(x => !string.IsNullOrWhiteSpace(x.Modele));
        RuleFor(x => x.NumeroSerie).MaximumLength(150).When(x => !string.IsNullOrWhiteSpace(x.NumeroSerie));
        RuleFor(x => x.Image).MaximumLength(500).When(x => !string.IsNullOrWhiteSpace(x.Image));
    }
}