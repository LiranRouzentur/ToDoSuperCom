using FluentValidation;
using TaskApp.Application.DTOs;
using TaskApp.Application.Helpers;

namespace TaskApp.Application.Validators;

public class UserRefDtoValidator : AbstractValidator<UserRefDto>
{
    public UserRefDtoValidator()
    {
        RuleFor(x => x.FullName)
            .NotEmpty().WithMessage("Full name is required.")
            .MaximumLength(100).WithMessage("Full name must not exceed 100 characters.");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("Email must be a valid email address.")
            .MaximumLength(150).WithMessage("Email must not exceed 150 characters.");

        RuleFor(x => x.Telephone)
            .NotEmpty().WithMessage("Telephone is required.")
            .Must(PhoneNumberHelper.IsValidIsraeliPhone)
            .WithMessage("Telephone must be a valid Israeli phone number.")
            .MaximumLength(20).WithMessage("Telephone must not exceed 20 characters.");
    }
}

