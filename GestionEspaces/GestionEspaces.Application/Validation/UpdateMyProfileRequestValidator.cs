using FluentValidation;
using GestionEspaces.Application.DTOs.SelfService;

namespace GestionEspaces.Application.Validation;

public sealed class UpdateMyProfileRequestValidator : AbstractValidator<UpdateMyProfileRequest>
{
    public UpdateMyProfileRequestValidator()
    {
        RuleFor(request => request.ConcurrencyToken)
            .NotEmpty();

        RuleFor(request => request.Telephone)
            .MaximumLength(30)
            .When(request => !string.IsNullOrWhiteSpace(request.Telephone));
    }
}
