import { useEffect, useCallback } from "react";
import {
  Box,
  Typography,
  Paper,
  Tabs,
  Tab,
  FormControl,
  InputLabel,
  Select,
  MenuItem,
  Checkbox,
  CircularProgress,
  Alert,
} from "@mui/material";
import { useActiveUser } from "../../../hooks/useActiveUser";
import { useAppSelector, useAppDispatch } from "../../../store/hooks";
import { fetchTasks, setScope, setPriorityFilter, setConflictTask, deleteTask as deleteTaskAction, type TaskScope } from "../../../store/slices/tasksSlice";
import { openTaskForm } from "../../../store/slices/uiSlice";
import { showSnackbar } from "../../../store/slices/uiSlice";
import { KanbanBoard } from "./KanbanBoard";
import { EmptyState } from "./EmptyState";
import { ConcurrencyConflictModal } from "./ConcurrencyConflictModal";
import type { TaskStatus } from "../types";

export function TaskList() {
  const dispatch = useAppDispatch();
  const { activeUserId } = useActiveUser();
  
  // Redux state
  const tasks = useAppSelector(state => state.tasks.items);
  const isLoading = useAppSelector(state => state.tasks.loading);
  const error = useAppSelector(state => state.tasks.error);
  const scope = useAppSelector(state => state.tasks.filters.scope);
  const priority = useAppSelector(state => state.tasks.filters.priority);
  const conflictTask = useAppSelector(state => state.tasks.conflictTask);

  // Load tasks when filters or activeUserId change
  useEffect(() => {
    // Only pass ownership filters if relevant to the scope
    const currentOwnerId = scope === "my" ? activeUserId : undefined;
    const currentAssignedId = scope === "assigned" ? activeUserId : undefined;
    
    // Prevent fetching if we need a user ID but don't have it yet
    if ((scope === "my" || scope === "assigned") && !activeUserId) return;

    const query: any = {
      scope,
      ownerUserId: currentOwnerId,
      assignedUserId: currentAssignedId,
      priority: priority.length > 0 ? priority.join(",") : undefined,
      page: 1,
      pageSize: 100,
      sortBy: "dueDateUtc",
      sortDir: "asc",
    };
    
    const promise = dispatch(fetchTasks(query));
    
    return () => {
      promise.abort();
    };
    // Optimization: Only depend on activeUserId if scope relies on it.
    // However, hooks rules enforce listing all dependencies.
    // We rely on the logic inside to short-circuit or produce identical query objects.
    // Note: If scope is 'all', activeUserId changes won't change the query object content,
    // but a new object reference is created, triggering the thunk.
    // Ideally we should memoize the query object.
  }, [dispatch, scope === "all" ? undefined : activeUserId, scope, priority]);

  const handleScopeChange = (_: React.SyntheticEvent, value: TaskScope) => {
    dispatch(setScope(value));
  };

  const handlePriorityChange = (value: string) => {
    const newPriority = priority.includes(value) 
      ? priority.filter(p => p !== value) 
      : [...priority, value];
    dispatch(setPriorityFilter(newPriority));
  };

  const handleDelete = useCallback(async (id: string) => {
    try {
      await dispatch(deleteTaskAction(id)).unwrap();
      dispatch(showSnackbar({ message: 'Task deleted successfully', severity: 'success' }));
    } catch (err: any) {
      dispatch(showSnackbar({ message: err.message || 'Failed to delete task', severity: 'error' }));
    }
  }, [dispatch]);

  const handleAddTask = useCallback((status: TaskStatus) => {
    dispatch(openTaskForm({ initialStatus: status }));
  }, [dispatch]);

  if (!activeUserId && scope !== "all") {
    return (
      <Alert severity="info">
        Please select an active user to view tasks.
      </Alert>
    );
  }

  return (
    <Box>
      {/* Header & Priority Filter */}
      <Paper 
        elevation={0} 
        sx={{ 
          p: 0, 
          mb: 4, 
          borderRadius: 3, 
          overflow: 'hidden',
          border: '1px solid',
          borderColor: 'divider',
          bgcolor: 'background.paper'
        }}
      >
        <Box sx={{ 
          borderBottom: 1, 
          borderColor: 'divider', 
          px: 2, 
          py: 2, // Balanced padding
          display: 'flex',
          alignItems: 'center',
          justifyContent: 'space-between',
          flexWrap: 'wrap',
          gap: 2
        }}>
          <Tabs 
            value={scope} 
            onChange={handleScopeChange} 
            TabIndicatorProps={{ sx: { display: 'none' } }} // Remove default underline
            sx={{
              minHeight: 'auto',
              '& .MuiTabs-flexContainer': { gap: 1 },
              '& .MuiTab-root': {
                outline: 'none', // Remove native focus outline
                textTransform: 'none',
                fontWeight: 600,
                minWidth: 'auto',
                minHeight: 40,
                px: 2.5,
                borderRadius: 2,
                fontSize: '0.9rem',
                color: 'text.secondary',
                transition: 'all 0.2s',
                '&:hover': {
                  bgcolor: 'action.hover',
                  color: 'text.primary',
                },
                '&.Mui-selected': {
                  color: 'primary.main',
                  bgcolor: 'rgba(25, 118, 210, 0.08)', // Soft primary background
                }
              }
            }}
          >
            <Tab value="all" label="All Tasks" />
            <Tab value="assigned" label="Assigned To Me" />
            <Tab value="my" label="Created By Me" />
          </Tabs>

          {/* Priority Filter Moved Here */}
          <Box>
            <FormControl variant="outlined" size="small" sx={{ minWidth: 140 }}>
              <InputLabel id="priority-filter-label">Priority</InputLabel>
              <Select
                labelId="priority-filter-label"
                multiple
                value={priority}
                label="Priority"
                sx={{ borderRadius: 2 }}
                renderValue={(selected) => (
                  <Typography variant="body2" noWrap>{selected.length ? `${selected.length} Selected` : 'Priority'}</Typography>
                )}
              >
                {["Low", "Medium", "High"].map((p) => (
                  <MenuItem key={p} value={p} onClick={() => handlePriorityChange(p)}>
                    <Checkbox size="small" checked={priority.includes(p)} />
                    <Typography variant="body2">{p}</Typography>
                  </MenuItem>
                ))}
              </Select>
            </FormControl>
          </Box>
        </Box>
        {/* Secondary Filter Bar Removed */}
      </Paper>

      {/* Task List / Kanban Content */}
      {isLoading && (!tasks || tasks.length === 0) ? (
        <Box sx={{ display: "flex", justifyContent: "center", alignItems: 'center', minHeight: 400 }}>
          <CircularProgress />
        </Box>
      ) : error && (!tasks || tasks.length === 0) ? (
        <Alert severity="error" sx={{ borderRadius: 2 }}>
          {error}
        </Alert>
      ) : !tasks || tasks.length === 0 ? (
        <EmptyState onCreateClick={() => dispatch(openTaskForm({}))} />
      ) : (
        <KanbanBoard
          tasks={tasks}
          onDelete={handleDelete}
          onConflict={(task) => dispatch(setConflictTask(task))}
          onUpdateSuccess={() => dispatch(fetchTasks({ scope, priority: priority.join(',') || undefined }))} // Only called on 404 errors for state sync
          onAddTask={handleAddTask}
        />
      )}

      {/* Concurrency Conflict Modal */}
      {conflictTask && (
        <ConcurrencyConflictModal
          task={conflictTask}
          open={!!conflictTask}
          onClose={() => dispatch(setConflictTask(null))}
          onReload={() => {
            dispatch(setConflictTask(null));
            dispatch(fetchTasks({ scope, priority: priority.join(',') || undefined }));
          }}
        />
      )}
    </Box>
  );
}
