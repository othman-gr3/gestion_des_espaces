using GestionEspaces.Application.DTOs.Agents;
using FluentValidation;

namespace GestionEspaces.Application.Validation;

/// <summary>
/// Validates agent creation requests.
/// </summary>
public sealed class CreateAgentRequestValidator : AbstractValidator<CreateAgentRequest>
{
    public CreateAgentRequestValidator()
    {
        RuleFor(request => request.Nom)
            .NotEmpty()
            .MaximumLength(150);

        RuleFor(request => request.Prenom)
            .NotEmpty()
            .MaximumLength(150);

        RuleFor(request => request.Matricule)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(request => request.Email)
            .EmailAddress()
            .When(request => !string.IsNullOrWhiteSpace(request.Email));

        RuleFor(request => request.Telephone)
            .MaximumLength(30)
            .When(request => !string.IsNullOrWhiteSpace(request.Telephone));

        RuleFor(request => request.Fonction)
            .MaximumLength(150)
            .When(request => !string.IsNullOrWhiteSpace(request.Fonction));

        RuleFor(request => request.Departement)
            .MaximumLength(150)
            .When(request => !string.IsNullOrWhiteSpace(request.Departement));

        RuleFor(request => request.Image)
            .MaximumLength(500)
            .When(request => !string.IsNullOrWhiteSpace(request.Image));
    }
}