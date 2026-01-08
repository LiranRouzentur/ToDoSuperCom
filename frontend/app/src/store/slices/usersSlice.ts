import { createSlice, createAsyncThunk } from '@reduxjs/toolkit';
import type { UserRef } from '../../features/tasks/types';
import { apiService } from '../../services/apiService';

interface UsersState {
  items: UserRef[];
  loading: boolean;
  error: string | null;
}

const initialState: UsersState = {
  items: [],
  loading: false,
  error: null,
};

// Async thunks
export const fetchUsers = createAsyncThunk(
  'users/fetchUsers',
  async () => {
    const response = await apiService.listUsers();
    return response.items;
  },
  {
    condition: (_, { getState }) => {
      const { users } = getState() as { users: UsersState };
      if (users.loading || users.items.length > 0) {
        return false;
      }
    },
  }
);

export const createUser = createAsyncThunk(
  'users/createUser',
  async (userData: { fullName: string; email: string; telephone: string }) => {
    const response = await apiService.createUser(userData);
    return response;
  }
);

const usersSlice = createSlice({
  name: 'users',
  initialState,
  reducers: {},
  extraReducers: (builder) => {
    builder
      // Fetch users
      .addCase(fetchUsers.pending, (state) => {
        state.loading = true;
        state.error = null;
      })
      .addCase(fetchUsers.fulfilled, (state, action) => {
        state.loading = false;
        state.items = action.payload;
      })
      .addCase(fetchUsers.rejected, (state, action) => {
        state.loading = false;
        state.error = action.error.message || 'Failed to load users';
      })
      // Create user
      .addCase(createUser.fulfilled, (state, action) => {
        state.items.push(action.payload);
      });
  },
});

export default usersSlice.reducer;
