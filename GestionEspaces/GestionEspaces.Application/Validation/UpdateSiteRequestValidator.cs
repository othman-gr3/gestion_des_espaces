using FluentValidation;
using GestionEspaces.Application.DTOs.Sites;

namespace GestionEspaces.Application.Validation;

public sealed class UpdateSiteRequestValidator : AbstractValidator<UpdateSiteRequest>
{
    public UpdateSiteRequestValidator()
    {
        RuleFor(x => x.ConcurrencyToken).NotEmpty();
        RuleFor(x => x.Nom).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Code).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Rue).NotEmpty().MaximumLength(250);
        RuleFor(x => x.Ville).NotEmpty().MaximumLength(150);
        RuleFor(x => x.CodePostal).NotEmpty().MaximumLength(20);
        RuleFor(x => x.Pays).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Telephone).MaximumLength(20).When(x => !string.IsNullOrWhiteSpace(x.Telephone));
        RuleFor(x => x.Email).MaximumLength(100).EmailAddress().When(x => !string.IsNullOrWhiteSpace(x.Email));
        RuleFor(x => x.Image).MaximumLength(500).When(x => !string.IsNullOrWhiteSpace(x.Image));
    }
}