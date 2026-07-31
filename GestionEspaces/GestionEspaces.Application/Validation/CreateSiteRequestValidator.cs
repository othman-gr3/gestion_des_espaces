using FluentValidation;
using GestionEspaces.Application.DTOs.Sites;

namespace GestionEspaces.Application.Validation;

public sealed class CreateSiteRequestValidator : AbstractValidator<CreateSiteRequest>
{
    public CreateSiteRequestValidator()
    {
        RuleFor(x => x.Nom).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Code).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Rue).NotEmpty().MaximumLength(250);
        RuleFor(x => x.Ville).NotEmpty().MaximumLength(150);
        RuleFor(x => x.CodePostal).NotEmpty().MaximumLength(20);
        RuleFor(x => x.Pays).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Image).MaximumLength(500).When(x => !string.IsNullOrWhiteSpace(x.Image));
    }
}