using FluentValidation;
using TaskApp.Application.DTOs;

namespace TaskApp.Application.Validators;

public class TaskUpdateRequestValidator : AbstractValidator<TaskUpdateRequest>
{
    public TaskUpdateRequestValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title is required.")
            .MaximumLength(50).WithMessage("Title must not exceed 50 characters.");

        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("Description is required.")
            .MaximumLength(250).WithMessage("Description must not exceed 250 characters.");

        RuleFor(x => x.DueDateUtc)
            .NotEmpty().WithMessage("Due date is required.")
            .Must(BeInFuture).WithMessage("Due date must not be in the past (UTC).");

        RuleFor(x => x.Priority)
            .IsInEnum().WithMessage("Priority must be a valid value.");

        RuleFor(x => x.Status)
            .IsInEnum().WithMessage("Status must be a valid value.");
    }

    private static bool BeInFuture(DateTime dueDate)
    {
        return dueDate >= DateTime.UtcNow;
    }
}

