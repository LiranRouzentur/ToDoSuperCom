import type { Task, UserRef } from "../features/tasks/types";

// Use relative URL when in Docker (via Vite proxy) or absolute URL when VITE_API_URL is set
// In Docker, Vite proxy forwards /api/* to the API container
// When VITE_API_URL is set, use it directly (for local dev outside Docker)
const API_BASE_URL = import.meta.env.VITE_API_URL || "";
const API_V1 = API_BASE_URL 
  ? `${API_BASE_URL}/api/v1` 
  : "/api/v1"; // Relative URL uses Vite proxy

// Retry helper for API calls
const pendingRequests = new Map<string, Promise<any>>();

// Retry helper for API calls with Deduplication
async function fetchWithRetry(
  url: string,
  options: RequestInit = {},
  maxRetries = 5,
  delayMs = 1000
): Promise<Response> {
  // Deduplication: Return existing promise if request is already in flight
  // Use a key that includes method and body to be safe (though for GET it's just URL)
  const reqKey = `${options.method || 'GET'}:${url}:${options.body || ''}`;
  
  if (!pendingRequests.has(reqKey)) {
    const promise = (async () => {
      let lastError: Error | null = null;
      
      for (let attempt = 0; attempt < maxRetries; attempt++) {
        try {
          const response = await fetch(url, options);
          // If we get a response (even error status), API is up
          if (response.status !== 0) {
            return response;
          }
        } catch (error) {
          lastError = error instanceof Error ? error : new Error(String(error));
          // Network errors - API might not be ready yet
          if (attempt < maxRetries - 1) {
            await new Promise(resolve => setTimeout(resolve, delayMs * (attempt + 1)));
            continue;
          }
        }
      }
      
      throw lastError || new Error("Failed to fetch after retries");
    })();

    pendingRequests.set(reqKey, promise);
    
    // Clean up when the promise settles (success or fail)
    // We do this here so it clears for everyone once the network part is done
    promise.finally(() => {
        pendingRequests.delete(reqKey);
    });
  }

  // All callers (including the first one) wait for the shared promise
  const response = await pendingRequests.get(reqKey)!;
  
  // Return a CLONE to every caller so they can read the body independently
  return response.clone();
}

// Check if API is ready - more aggressive waiting for UI initialization
let isApiReady = false;
let apiReadyPromise: Promise<boolean> | null = null;

export function waitForApiReady(maxWaitMs = 60000): Promise<boolean> {
  if (isApiReady) return Promise.resolve(true);
  if (apiReadyPromise) return apiReadyPromise;

  apiReadyPromise = (async () => {
    const startTime = Date.now();
    const checkInterval = 200; // Check every 200ms for faster response
    const healthUrl = API_BASE_URL ? `${API_BASE_URL}/health` : "/health";
    
    console.log('[API] Waiting for API to be ready...');
    
    while (Date.now() - startTime < maxWaitMs) {
      if (isApiReady) return true; // Check if ready flag was set elsewhere

      try {
        // Create timeout manually for browser compatibility
        const controller = new AbortController();
        const timeoutId = setTimeout(() => controller.abort(), 1000); // 1s timeout per check
        
        const response = await fetch(healthUrl, { 
          method: 'GET',
          signal: controller.signal,
          cache: 'no-cache'
        });
        
        clearTimeout(timeoutId);
        
        if (response.ok) {
          console.log('[API] API is ready!');
          isApiReady = true;
          apiReadyChecked = true; // Also set the service-level flag
          return true;
        }
      } catch (error) {
        // API not ready yet - continue waiting
        const elapsed = Math.round((Date.now() - startTime) / 1000);
        if (elapsed % 2 === 0) { // Log every 2 seconds to avoid spam
          console.log(`[API] Still waiting for API... (${elapsed}s)`);
        }
      }
      await new Promise(resolve => setTimeout(resolve, checkInterval));
    }
    
    console.warn('[API] API readiness check timed out');
    return false;
  })();

  return apiReadyPromise;
}

export interface TaskListQuery {
  scope?: "my" | "assigned" | "all";
  ownerUserId?: string;
  assignedUserId?: string;
  status?: string;
  priority?: string;
  overdueOnly?: boolean;
  search?: string;
  page?: number;
  pageSize?: number;
  sortBy?: string;
  sortDir?: "asc" | "desc";
}

export interface PagedResponse<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalItems: number;
  totalPages: number;
}

export interface CreateTaskDto {
  title: string;
  description: string;
  dueDateUtc: string;
  priority: string;
  status?: string;
  assignedUserId?: string;
  ownerUserId: string;
  assignee?: {
    fullName: string;
    email: string;
    telephone: string;
  };
  owner: {
    fullName: string;
    email: string;
    telephone: string;
  };
}

export interface UpdateTaskDto {
  title: string;
  description: string;
  dueDateUtc: string;
  priority: string;
  status: string;
  assignedUserId?: string;
  ownerUserId?: string;
}

// Initialize: wait for API to be ready on first call
// Note: ApiReadyGate component handles initial readiness check, so this is just a safety net
let apiReadyChecked = false;

export const apiService = {
  // Tasks
  async listTasks(params: TaskListQuery): Promise<PagedResponse<Task>> {
    // Safety check: if ApiReadyGate didn't wait, wait here (shouldn't happen)
    if (!apiReadyChecked) {
      await waitForApiReady(5000); // Shorter timeout since ApiReadyGate already waited
      apiReadyChecked = true;
    }
    
    const query = new URLSearchParams();
    Object.entries(params).forEach(([key, value]) => {
      if (value !== undefined) query.append(key, value.toString());
    });
    const response = await fetchWithRetry(`${API_V1}/tasks?${query.toString()}`);
    if (!response.ok) throw new Error("Failed to fetch tasks");
    return response.json();
  },

  async getTask(id: string): Promise<Task> {
    const response = await fetchWithRetry(`${API_V1}/tasks/${id}`);
    if (!response.ok) throw new Error("Failed to fetch task");
    return response.json();
  },

  async createTask(task: any): Promise<Task> {
    const response = await fetch(`${API_V1}/tasks`, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(task),
    });
    if (!response.ok) throw new Error("Failed to create task");
    return response.json();
  },

  async updateTask(id: string, task: any, rowVersion: string): Promise<Task> {
    const response = await fetch(`${API_V1}/tasks/${id}`, {
      method: "PUT",
      headers: { 
        "Content-Type": "application/json",
        "If-Match": rowVersion 
      },
      body: JSON.stringify(task),
    });
    if (response.status === 409) {
      const errorData = await response.json();
      throw { status: 409, data: errorData };
    }
    if (!response.ok) {
      let errorMessage = "Failed to update task";
      try {
        const text = await response.text();
        try {
          const errorData = JSON.parse(text);
          
          // Handle Custom ErrorResponse format: { error: { code, message, details: [] } }
          if (errorData.error) {
            if (errorData.error.details && Array.isArray(errorData.error.details) && errorData.error.details.length > 0) {
              // Validation error: take the first field error
              errorMessage = errorData.error.details[0].message;
            } else if (errorData.error.message) {
              // General error message (e.g. InvalidOperation)
              errorMessage = errorData.error.message;
            }
          }
          // Fallback: Handle Standard ProblemDetails or other formats
          else if (errorData.errors) {
            const firstError = Object.values(errorData.errors)[0];
            errorMessage = Array.isArray(firstError) ? String(firstError[0]) : errorMessage;
          } else if (errorData.detail) {
            errorMessage = errorData.detail;
          } else if (errorData.title) {
            errorMessage = errorData.title;
          }
        } catch {
          if (text && text.length < 500) {
            errorMessage = text;
          }
        }
      } catch (e) {
        // Error parsing response - use default message
      }
      throw new Error(errorMessage);
    }
    return response.json();
  },

  async deleteTask(id: string): Promise<void> {
    const response = await fetch(`${API_V1}/tasks/${id}`, {
      method: "DELETE",
    });
    if (!response.ok) throw new Error("Failed to delete task");
  },

  // Users
  async listUsers(): Promise<PagedResponse<UserRef>> {
    // Safety check: if ApiReadyGate didn't wait, wait here (shouldn't happen)
    if (!apiReadyChecked) {
      await waitForApiReady(5000); // Shorter timeout since ApiReadyGate already waited
      apiReadyChecked = true;
    }
    
    const response = await fetchWithRetry(`${API_V1}/users`);
    if (!response.ok) throw new Error("Failed to fetch users");
    return response.json();
  },

  async createUser(user: any): Promise<UserRef> {
    const response = await fetch(`${API_V1}/users`, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(user),
    });
    if (!response.ok) throw new Error("Failed to create user");
    return response.json();
  }
};
