import type { Task, TaskStatus } from "../types";

export interface StatusValidationResult {
  isValid: boolean;
  error?: string;
}

/**
 * Validates whether a task can transition to a new status
 */
export function validateStatusChange(
  task: Task,
  newStatus: TaskStatus
): StatusValidationResult {
  // Prevent manual setting of Overdue - it's auto-calculated
  if (newStatus === "Overdue") {
    return {
      isValid: false,
      error: "Cannot manually set status to Overdue. It is auto-calculated based on due date.",
    };
  }

  // Prevent dragging FROM Overdue - task must be edited to update due date first
  if (task.status === "Overdue") {
    return {
      isValid: false,
      error: "Cannot change status of overdue tasks via drag-and-drop. Please edit the task to update the due date first.",
    };
  }

  // All other status transitions are allowed
  return { isValid: true };
}

/**
 * Returns list of valid status options for a given task
 */
export function getAvailableStatuses(task: Task): TaskStatus[] {
  const allStatuses: TaskStatus[] = ["Draft", "Open", "InProgress", "Completed", "Cancelled"];
  
  return allStatuses.filter((status) => {
    const validation = validateStatusChange(task, status);
    return validation.isValid;
  });
}
