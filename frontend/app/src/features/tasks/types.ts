export type TaskPriority = "Low" | "Medium" | "High";
export type TaskStatus = "Draft" | "Open" | "InProgress" | "Completed" | "Overdue" | "Cancelled";

export interface UserRef {
  id: string;
  fullName: string;
  email: string;
  telephone: string;
}

export interface Task {
  id: string;
  title: string;
  description: string;
  dueDateUtc: string;
  priority: TaskPriority;
  status: TaskStatus;
  reminderSent: boolean;
  owner: UserRef;
  assignee: UserRef;
  createdAtUtc: string;
  updatedAtUtc: string;
  rowVersion: string;
}

export interface PagedResponse<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalItems: number;
  totalPages: number;
}

