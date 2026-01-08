import { type ReactNode, useEffect } from "react";
import {
  Box,
  AppBar,
  Toolbar,
  Typography,
  Container,
  CssBaseline,
  Button,
  Snackbar,
  Alert,
} from "@mui/material";
import { Add as AddIcon } from "@mui/icons-material";
import { useAppSelector, useAppDispatch } from "../store/hooks";
import { openTaskForm, closeTaskForm, hideSnackbar } from "../store/slices/uiSlice";
import { fetchUsers } from "../store/slices/usersSlice";
import { TaskForm } from "../features/tasks/components/TaskForm";

interface LayoutProps {
  children: ReactNode;
}

export function Layout({ children }: LayoutProps) {
  const dispatch = useAppDispatch();
  const isFormOpen = useAppSelector(state => state.ui.taskForm.isOpen);
  const editingTaskId = useAppSelector(state => state.ui.taskForm.editingTaskId);
  const initialStatus = useAppSelector(state => state.ui.taskForm.initialStatus);
  const snackbar = useAppSelector(state => state.ui.snackbar);
  const tasks = useAppSelector(state => state.tasks.items);

  // Load users on mount to ensure Current User is always available
  useEffect(() => {
    dispatch(fetchUsers());
  }, [dispatch]);

  const handleOpenForm = () => {
    dispatch(openTaskForm({}));
  };

  const handleCloseForm = () => {
    dispatch(closeTaskForm());
  };

  const handleSnackbarClose = () => {
    dispatch(hideSnackbar());
  };

  const editingTask = editingTaskId ? tasks.find(t => t.id === editingTaskId) : undefined;

  return (
    <Box sx={{ display: "flex", flexDirection: "column", minHeight: "100vh" }}>
      <CssBaseline />
      <AppBar 
        position="sticky" 
        elevation={0} 
        color="default" 
        sx={{ 
          background: 'rgba(255, 255, 255, 0.85)',
          backdropFilter: 'blur(10px)',
          borderBottom: '1px solid', 
          borderColor: 'rgba(0, 0, 0, 0.08)',
        }}
      >
        <Container maxWidth={false} sx={{ px: { xs: 2, md: 4 } }}>
          <Toolbar disableGutters sx={{ height: 72 }}>
            <Typography
              variant="h5"
              component="div"
              sx={{ 
                flexGrow: 1, 
                fontWeight: 900, 
                background: 'linear-gradient(135deg, #1976d2 0%, #42a5f5 100%)',
                backgroundClip: 'text',
                WebkitBackgroundClip: 'text',
                WebkitTextFillColor: 'transparent',
                letterSpacing: '-0.5px' 
              }}
            >
              ToDo Task System
            </Typography>
            <Button
              variant="contained"
              startIcon={<AddIcon />}
              onClick={handleOpenForm}
              // disabled={!activeUserId} -- ENABLED always
              sx={{ 
                borderRadius: '8px',
                textTransform: 'none',
                px: 3,
                fontWeight: 'bold',
                background: 'linear-gradient(135deg, #1976d2 0%, #42a5f5 100%)',
                boxShadow: '0 4px 12px rgba(25, 118, 210, 0.3)',
                transition: 'all 0.2s ease',
                '&:hover': {
                  background: 'linear-gradient(135deg, #1565c0 0%, #1976d2 100%)',
                  boxShadow: '0 6px 16px rgba(25, 118, 210, 0.4)',
                  transform: 'translateY(-2px)',
                },
                '&:active': {
                  transform: 'translateY(0) scale(0.98)',
                },
                '&:disabled': {
                  background: 'rgba(0, 0, 0, 0.12)',
                  boxShadow: 'none',
                }
              }}
            >
              Create Task
            </Button>
          </Toolbar>
        </Container>
      </AppBar>
      <Container component="main" sx={{ flexGrow: 1, py: 4, px: { xs: 2, md: 4 } }} maxWidth={false}>
        {children}
      </Container>
      <Box
        component="footer"
        sx={{
          py: 3,
          px: 2,
          mt: "auto",
          backgroundColor: (theme) =>
            theme.palette.mode === "light"
              ? theme.palette.grey[50]
              : theme.palette.grey[900],
          borderTop: '1px solid',
          borderColor: 'divider'
        }}
      >
        <Container maxWidth="sm">
          <Typography variant="body2" color="text.secondary" align="center">
            {"Copyright © "}
            ToDo Task System {new Date().getFullYear()}
            {"."}
          </Typography>
        </Container>
      </Box>

      <TaskForm
        open={isFormOpen}
        onClose={handleCloseForm}
        task={editingTask}
        initialStatus={initialStatus || undefined}
      />

      <Snackbar
        open={snackbar.open}
        autoHideDuration={6000}
        onClose={handleSnackbarClose}
        anchorOrigin={{ vertical: 'bottom', horizontal: 'center' }}
      >
        <Alert onClose={handleSnackbarClose} severity={snackbar.severity} sx={{ width: '100%' }}>
          {snackbar.message}
        </Alert>
      </Snackbar>
    </Box>
  );
}
