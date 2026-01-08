import { useAppSelector } from '../store/hooks';
import { ACTIVE_USER_EMAIL } from '../constants/user';

/**
 * Hook to get the active user and users list.
 * Since this is a single-user system, the active user is always liran@example.com.
 * Other users in the list are just for assignment options.
 */
export function useActiveUser() {
  const users = useAppSelector(state => state.users.items);
  
  // Find the active user from the users list
  const activeUser = users.find(u => u.email === ACTIVE_USER_EMAIL) || null;
  
  return {
    activeUser,
    activeUserId: activeUser?.id || null,
    users, // All users for assignment dropdown
  };
}
