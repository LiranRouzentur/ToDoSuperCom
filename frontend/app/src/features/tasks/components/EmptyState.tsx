import { Box, Typography, Button } from "@mui/material";
import { Assignment as AssignmentIcon } from "@mui/icons-material";

interface EmptyStateProps {
  onCreateClick: () => void;
}

export function EmptyState({ onCreateClick }: EmptyStateProps) {
  return (
    <Box
      sx={{
        display: "flex",
        flexDirection: "column",
        alignItems: "center",
        justifyContent: "center",
        p: 4,
        textAlign: "center",
      }}
    >
      <AssignmentIcon sx={{ fontSize: 64, color: "text.secondary", mb: 2 }} />
      <Typography variant="h5" gutterBottom>
        No tasks found
      </Typography>
      <Typography variant="body2" color="text.secondary" sx={{ mb: 3 }}>
        Get started by creating your first task.
      </Typography>
      <Button variant="contained" onClick={onCreateClick}>
        Create Task
      </Button>
    </Box>
  );
}

