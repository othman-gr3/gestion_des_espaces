using FluentValidation;
using GestionEspaces.Application.DTOs.Batiments;

namespace GestionEspaces.Application.Validation;

public sealed class CreateBatimentRequestValidator : AbstractValidator<CreateBatimentRequest>
{
    public CreateBatimentRequestValidator()
    {
        RuleFor(x => x.Nom).NotEmpty().MaximumLength(200);
        RuleFor(x => x.NombreEtages).GreaterThanOrEqualTo(0);
        RuleFor(x => x.IdSite).GreaterThan(0);
        RuleFor(x => x.Image).MaximumLength(500).When(x => !string.IsNullOrWhiteSpace(x.Image));
    }
}