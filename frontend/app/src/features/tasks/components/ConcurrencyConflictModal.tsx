import {
  Dialog,
  DialogTitle,
  DialogContent,
  DialogActions,
  Button,
  Typography,
  Box,
  Alert,
} from "@mui/material";
import type { Task } from "../types";

interface ConcurrencyConflictModalProps {
  task: Task;
  open: boolean;
  onClose: () => void;
  onReload: () => void;
}

export function ConcurrencyConflictModal({
  task,
  open,
  onClose,
  onReload,
}: ConcurrencyConflictModalProps) {
  return (
    <Dialog open={open} onClose={onClose} maxWidth="md" fullWidth>
      <DialogTitle>Concurrency Conflict</DialogTitle>
      <DialogContent>
        <Alert severity="warning" sx={{ mb: 2 }}>
          This task was updated by another process. Your version is outdated.
        </Alert>
        <Typography variant="subtitle2" gutterBottom>
          Current Server Version:
        </Typography>
        <Box sx={{ p: 2, bgcolor: "grey.100", borderRadius: 1 }}>
          <Typography variant="body2">
            <strong>Title:</strong> {task.title}
          </Typography>
          <Typography variant="body2">
            <strong>Status:</strong> {task.status}
          </Typography>
          <Typography variant="body2">
            <strong>Priority:</strong> {task.priority}
          </Typography>
          <Typography variant="body2">
            <strong>Due Date:</strong>{" "}
            {new Date(task.dueDateUtc).toLocaleString()}
          </Typography>
          <Typography variant="body2">
            <strong>Last Updated:</strong>{" "}
            {new Date(task.updatedAtUtc).toLocaleString()}
          </Typography>
        </Box>
      </DialogContent>
      <DialogActions>
        <Button onClick={onClose}>Cancel</Button>
        <Button onClick={onReload} variant="contained">
          Reload Latest Version
        </Button>
      </DialogActions>
    </Dialog>
  );
}

