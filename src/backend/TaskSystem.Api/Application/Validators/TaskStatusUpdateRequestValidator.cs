using FluentValidation;
using TaskApp.Application.DTOs;

namespace TaskApp.Application.Validators;

public class TaskStatusUpdateRequestValidator : AbstractValidator<TaskStatusUpdateRequest>
{
    public TaskStatusUpdateRequestValidator()
    {
        RuleFor(x => x.Status)
            .IsInEnum().WithMessage("Status must be a valid value.");
    }
}

