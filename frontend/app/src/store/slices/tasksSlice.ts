import { createSlice, createAsyncThunk, type PayloadAction } from '@reduxjs/toolkit';
import type { Task, TaskStatus } from '../../features/tasks/types';
import type { TaskListQuery, CreateTaskDto, UpdateTaskDto } from '../../services/apiService';
import { apiService } from '../../services/apiService';

export type TaskScope = 'my' | 'assigned' | 'all';

interface TasksState {
  items: Task[];
  loading: boolean;
  error: string | null;
  filters: {
    scope: TaskScope;
    priority: string[];
    page: number;
    pageSize: number;
  };
  totalItems: number;
  totalPages: number;
  conflictTask: Task | null;
  currentRequestId: string | null;
}

const initialState: TasksState = {
  items: [],
  loading: false,
  error: null,
  filters: {
    scope: 'all',
    priority: [],
    page: 1,
    pageSize: 100,
  },
  totalItems: 0,
  totalPages: 0,
  conflictTask: null,
  currentRequestId: null,
};

// Async thunks
export const fetchTasks = createAsyncThunk(
  'tasks/fetchTasks',
  async (params: TaskListQuery) => {
    const response = await apiService.listTasks(params);
    return response;
  }
);

export const createTask = createAsyncThunk(
  'tasks/createTask',
  async (taskData: CreateTaskDto) => {
    const response = await apiService.createTask(taskData);
    return response;
  }
);

export const updateTask = createAsyncThunk(
  'tasks/updateTask',
  async ({ id, data, rowVersion }: { id: string; data: UpdateTaskDto; rowVersion: string }) => {
    const response = await apiService.updateTask(id, data, rowVersion);
    return response;
  }
);

export const deleteTask = createAsyncThunk(
  'tasks/deleteTask',
  async (id: string) => {
    await apiService.deleteTask(id);
    return id;
  }
);

const tasksSlice = createSlice({
  name: 'tasks',
  initialState,
  reducers: {
    setScope(state, action: PayloadAction<TaskScope>) {
      state.filters.scope = action.payload;
    },
    setPriorityFilter(state, action: PayloadAction<string[]>) {
      state.filters.priority = action.payload;
    },
    setPage(state, action: PayloadAction<number>) {
      state.filters.page = action.payload;
    },
    setConflictTask(state, action: PayloadAction<Task | null>) {
      state.conflictTask = action.payload;
    },
    clearError(state) {
      state.error = null;
    },
    // Optimistic update for drag-and-drop
    updateTaskStatusOptimistic(state, action: PayloadAction<{ taskId: string; newStatus: TaskStatus }>) {
      const task = state.items.find(t => t.id === action.payload.taskId);
      if (task) {
        task.status = action.payload.newStatus;
      }
    },
  },
  extraReducers: (builder) => {
    builder
      // Fetch tasks
      .addCase(fetchTasks.pending, (state, action) => {
        state.loading = true;
        state.error = null;
        state.currentRequestId = action.meta.requestId;
      })
      .addCase(fetchTasks.fulfilled, (state, action) => {
        if (state.currentRequestId === action.meta.requestId) {
          state.loading = false;
          state.currentRequestId = null;
          state.items = action.payload.items;
          state.totalItems = action.payload.totalItems;
          state.totalPages = action.payload.totalPages;
        }
      })
      .addCase(fetchTasks.rejected, (state, action) => {
        if (state.currentRequestId === action.meta.requestId) {
          state.loading = false;
          state.currentRequestId = null;
          state.error = action.error.message || 'Failed to load tasks';
        }
      })
      // Create task
      .addCase(createTask.fulfilled, (state, action) => {
        state.items.push(action.payload);
        state.totalItems += 1;
      })
      // Update task
      .addCase(updateTask.fulfilled, (state, action) => {
        const index = state.items.findIndex(t => t.id === action.payload.id);
        if (index !== -1) {
          state.items[index] = action.payload;
        }
      })
      .addCase(updateTask.rejected, (state, action) => {
        state.error = action.error.message || 'Failed to update task';
      })
      // Delete task
      .addCase(deleteTask.fulfilled, (state, action) => {
        state.items = state.items.filter(t => t.id !== action.payload);
        state.totalItems -= 1;
      });
  },
});

export const { 
  setScope, 
  setPriorityFilter, 
  setPage, 
  setConflictTask, 
  clearError,
  updateTaskStatusOptimistic 
} = tasksSlice.actions;

export default tasksSlice.reducer;
