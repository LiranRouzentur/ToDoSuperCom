using FluentValidation;
using TaskApp.Application.DTOs;

namespace TaskApp.Application.Validators;

public class TaskAssigneeUpdateRequestValidator : AbstractValidator<TaskAssigneeUpdateRequest>
{
    public TaskAssigneeUpdateRequestValidator()
    {
        // AssignedUserId can be null (to unassign)
        // No additional validation needed
    }
}

