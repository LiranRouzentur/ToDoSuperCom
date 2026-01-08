import type { UserRef } from '../features/tasks/types';

// The single active user of the system
// This will be populated from the API on first load
export const ACTIVE_USER_EMAIL = 'liran@example.com';

// Placeholder - will be replaced with actual user data from API
export const ACTIVE_USER_PLACEHOLDER: UserRef = {
  id: '',
  fullName: 'Liran',
  email: ACTIVE_USER_EMAIL,
  telephone: '050-1234567',
};
