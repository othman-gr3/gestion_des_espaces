using FluentValidation;
using GestionEspaces.Application.DTOs.Agents;

namespace GestionEspaces.Application.Validation;

/// <summary>
/// Validates agent update requests.
/// </summary>
public sealed class UpdateAgentRequestValidator : AbstractValidator<UpdateAgentRequest>
{
    public UpdateAgentRequestValidator()
    {
        RuleFor(x => x.ConcurrencyToken).NotEmpty();

        RuleFor(x => x.Nom)
            .NotEmpty()
            .MaximumLength(150);

        RuleFor(x => x.Prenom)
            .NotEmpty()
            .MaximumLength(150);

        RuleFor(x => x.Matricule)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(x => x.Email)
            .EmailAddress()
            .When(x => !string.IsNullOrWhiteSpace(x.Email));

        RuleFor(x => x.Telephone)
            .MaximumLength(30)
            .When(x => !string.IsNullOrWhiteSpace(x.Telephone));

        RuleFor(x => x.Fonction)
            .MaximumLength(150)
            .When(x => !string.IsNullOrWhiteSpace(x.Fonction));

        RuleFor(x => x.Departement)
            .MaximumLength(150)
            .When(x => !string.IsNullOrWhiteSpace(x.Departement));

        RuleFor(x => x.Image)
            .MaximumLength(500)
            .When(x => !string.IsNullOrWhiteSpace(x.Image));
    }
}
