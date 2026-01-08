using FluentValidation;
using TaskApp.Application.DTOs;
using TaskApp.Application.Validators;

namespace TaskApp.Application.Validators;

public class TaskCreateRequestValidator : AbstractValidator<TaskCreateRequest>
{
    public TaskCreateRequestValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title is required.")
            .MaximumLength(50).WithMessage("Title must not exceed 50 characters.");

        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("Description is required.")
            .MaximumLength(250).WithMessage("Description must not exceed 250 characters.");

        RuleFor(x => x.DueDateUtc)
            .NotEmpty().WithMessage("Due date is required.")
            .Must(BeInFuture).WithMessage("Due date must be in the future (UTC).");

        RuleFor(x => x.Priority)
            .IsInEnum().WithMessage("Priority must be a valid value.");

        RuleFor(x => x.Owner)
            .NotNull().WithMessage("Owner is required.")
            .SetValidator(new UserRefDtoValidator());

        RuleFor(x => x.Assignee)
            .SetValidator(new UserRefDtoValidator()!)
            .When(x => x.Assignee != null);
    }

    private static bool BeInFuture(DateTime dueDate)
    {
        return dueDate > DateTime.UtcNow;
    }
}

