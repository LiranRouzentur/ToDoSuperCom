import { createSlice, type PayloadAction } from '@reduxjs/toolkit';
import type { TaskStatus } from '../../features/tasks/types';

interface UiState {
  taskForm: {
    isOpen: boolean;
    editingTaskId: string | null;
    initialStatus: TaskStatus | null;
  };
  snackbar: {
    open: boolean;
    message: string;
    severity: 'success' | 'error' | 'info' | 'warning';
  };
}

const initialState: UiState = {
  taskForm: {
    isOpen: false,
    editingTaskId: null,
    initialStatus: null,
  },
  snackbar: {
    open: false,
    message: '',
    severity: 'info',
  },
};

const uiSlice = createSlice({
  name: 'ui',
  initialState,
  reducers: {
    openTaskForm(state, action: PayloadAction<{ taskId?: string; initialStatus?: TaskStatus }>) {
      state.taskForm.isOpen = true;
      state.taskForm.editingTaskId = action.payload.taskId || null;
      state.taskForm.initialStatus = action.payload.initialStatus || null;
    },
    closeTaskForm(state) {
      state.taskForm.isOpen = false;
      state.taskForm.editingTaskId = null;
      state.taskForm.initialStatus = null;
    },
    showSnackbar(state, action: PayloadAction<{ message: string; severity: 'success' | 'error' | 'info' | 'warning' }>) {
      state.snackbar.open = true;
      state.snackbar.message = action.payload.message;
      state.snackbar.severity = action.payload.severity;
    },
    hideSnackbar(state) {
      state.snackbar.open = false;
    },
  },
});

export const { openTaskForm, closeTaskForm, showSnackbar, hideSnackbar } = uiSlice.actions;
export default uiSlice.reducer;
