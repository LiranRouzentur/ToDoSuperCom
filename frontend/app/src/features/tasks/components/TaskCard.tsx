import { useState, useMemo, memo } from "react";
import {
  Card,
  CardContent,
  Typography,
  IconButton,
  Box,
  Stack,
  Chip,
  Avatar,
  Tooltip,
  Collapse,
} from "@mui/material";
import {
  CalendarMonth as CalendarIcon,
  Flag as FlagIcon,
  Edit as EditIcon,
  Delete as DeleteIcon,
  ExpandMore as ExpandMoreIcon,
} from "@mui/icons-material";
import dayjs from "dayjs";
import relativeTime from 'dayjs/plugin/relativeTime';
import type { Task } from "../types";
import { useAppDispatch } from "../../../store/hooks";
import { openTaskForm } from "../../../store/slices/uiSlice";
import { TRANSITIONS } from "../../../utils/animations";

dayjs.extend(relativeTime);

interface TaskCardProps {
  task: Task;
  onDelete: (id: string) => void;
  isDragging?: boolean;
}

export const TaskCard = memo(function TaskCard({ task, onDelete, isDragging }: TaskCardProps) {
  const [expanded, setExpanded] = useState(false);
  const dispatch = useAppDispatch();

  // ... (getPriorityColor and memos remain the same) 

  const getPriorityColor = (priority: string) => {
    switch (priority) {
      case "High": return "#ef5350";
      case "Medium": return "#ff9800";
      case "Low": return "#4caf50";
      default: return "#bdbdbd";
    }
  };

  const priorityColor = useMemo(() => getPriorityColor(task.priority), [task.priority]);
  const isOverdue = useMemo(
    () => dayjs(task.dueDateUtc).isBefore(dayjs()) && task.status !== "Completed",
    [task.dueDateUtc, task.status]
  );
  const formattedDueDate = useMemo(
    () => dayjs(task.dueDateUtc).format("MMM DD, YYYY HH:mm"),
    [task.dueDateUtc]
  );
  const createdAgo = useMemo(
    () => dayjs(task.createdAtUtc).fromNow(),
    [task.createdAtUtc]
  );
  const updatedAgo = useMemo(
    () => dayjs(task.updatedAtUtc).fromNow(),
    [task.updatedAtUtc]
  );

  return (
    <Card
      data-testid={`task-card-${task.id}`}
      sx={{
        position: "relative",
        // Enhanced "Blocked" Indicator design
        borderLeft: `6px solid ${priorityColor}`,
        borderRadius: 3, // More rounded
        // Subtle background tint (Premium look)
        background: `linear-gradient(to right, ${priorityColor}08, #ffffff 15%)`,
        
        // CRITICAL FIX: Disable transition during drag to prevent "fighting" with the dnd library
        transition: isDragging ? 'none' : `all ${TRANSITIONS.normal}`,
        cursor: isDragging ? 'grabbing' : 'grab',
        
        // Refined Hover Effect
        '&:hover': !isDragging ? { 
          transform: 'translateY(-3px)',
          boxShadow: `0 12px 24px -10px rgba(0,0,0,0.15), 0 0 0 1px ${priorityColor}20`,
        } : {},
        
        // Dragging State
        boxShadow: isDragging 
          ? `0 20px 40px rgba(0,0,0,0.2), 0 0 0 2px ${priorityColor}` 
          : (expanded ? '0 8px 20px -5px rgba(0,0,0,0.1)' : '0 2px 8px rgba(0,0,0,0.06)'),
        opacity: isDragging ? 0.9 : 1,
        
        // Overflow hidden to clip the gradient/border cleanly
        overflow: 'hidden' 
      }}
    >
      <CardContent sx={{ p: 1.5, "&:last-child": { pb: 1.5 } }}>
        {/* Collapsed View - Title Only */}
        <Box 
          sx={{ 
            display: 'flex', 
            justifyContent: 'space-between', 
            alignItems: 'center',
          }}
          onClick={() => setExpanded(!expanded)}
        >
          <Typography 
            data-testid="task-title"
            variant="body2" 
            sx={{ 
              fontWeight: 600, 
              lineHeight: 1.4, 
              flexGrow: 1,
              pr: 1
            }}
          >
            {task.title}
          </Typography>
          <IconButton 
            size="small" 
            sx={{ 
              transform: expanded ? 'rotate(180deg)' : 'rotate(0deg)',
              transition: `transform ${TRANSITIONS.spring}`,
              '&:hover': {
                bgcolor: 'action.hover',
              },
              '&:active': {
                transform: expanded ? 'rotate(180deg) scale(0.9)' : 'rotate(0deg) scale(0.9)',
              }
            }}
          >
            <ExpandMoreIcon fontSize="small" />
          </IconButton>
        </Box>

        {/* Expanded View - Compact Layout */}
        <Collapse in={expanded} timeout={300} easing={{ enter: 'cubic-bezier(0.4, 0, 0.2, 1)', exit: 'cubic-bezier(0.4, 0, 0.2, 1)' }}>
          <Box sx={{ mt: 2 }}>
            {/* Description */}
            <Typography variant="body2" color="text.secondary" sx={{ mb: 2, lineHeight: 1.5 }}>
              {task.description}
            </Typography>

            {/* Compact Info Grid */}
            <Stack spacing={1.5}>
              {/* Date & Priority Row */}
              <Stack direction="row" spacing={2} sx={{ flexWrap: 'wrap', gap: 1 }}>
                <Box sx={{ display: 'flex', alignItems: 'center', gap: 0.5 }}>
                  <CalendarIcon sx={{ fontSize: 16, color: 'text.secondary' }} />
                  <Typography variant="caption" sx={{ fontWeight: 500 }}>
                    {formattedDueDate}
                  </Typography>
                  {isOverdue && (
                    <Chip 
                      label="Overdue" 
                      size="small" 
                      color="error" 
                      sx={{ 
                        height: 18, 
                        fontSize: '0.65rem', 
                        ml: 0.5,
                        animation: 'pulse 2s ease-in-out infinite',
                      }} 
                    />
                  )}
                </Box>
                <Box sx={{ 
                  display: 'flex', 
                  alignItems: 'center', 
                  gap: 0.5,
                  px: 1,
                  py: 0.25,
                  borderRadius: 1,
                  background: `linear-gradient(135deg, ${priorityColor}15 0%, ${priorityColor}05 100%)`,
                  border: `1px solid ${priorityColor}30`,
                }}>
                  <FlagIcon sx={{ fontSize: 16, color: priorityColor }} />
                  <Typography variant="caption" sx={{ fontWeight: 600, color: priorityColor }}>{task.priority}</Typography>
                </Box>
              </Stack>

              {/* Owner & Assignee Row */}
              <Stack direction="row" spacing={2} sx={{ flexWrap: 'wrap', gap: 1 }}>
                <Tooltip title={task.owner?.email || ''} arrow>
                  <Box sx={{ display: 'flex', alignItems: 'center', gap: 0.5 }}>
                    <Avatar sx={{ width: 20, height: 20, fontSize: '0.7rem', bgcolor: 'primary.main' }}>
                      {task.owner?.fullName.charAt(0)}
                    </Avatar>
                    <Typography variant="caption" sx={{ fontWeight: 500 }}>{task.owner?.fullName}</Typography>
                  </Box>
                </Tooltip>
                {task.assignee && (
                  <Tooltip title={task.assignee?.email || ''} arrow>
                    <Box sx={{ display: 'flex', alignItems: 'center', gap: 0.5 }}>
                      <Avatar sx={{ width: 20, height: 20, fontSize: '0.7rem', bgcolor: 'secondary.main' }}>
                        {task.assignee?.fullName.charAt(0)}
                      </Avatar>
                      <Typography variant="caption" sx={{ fontWeight: 500 }}>{task.assignee?.fullName}</Typography>
                    </Box>
                  </Tooltip>
                )}
              </Stack>

              {/* System Info */}
              <Stack direction="row" spacing={2} sx={{ opacity: 0.7 }}>
                <Typography variant="caption" color="text.secondary">
                  Created {createdAgo}
                </Typography>
                <Typography variant="caption" color="text.secondary">
                  Updated {updatedAgo}
                </Typography>
              </Stack>

              {/* Action Buttons - Icons Only */}
              <Stack direction="row" spacing={1} sx={{ pt: 0.5 }}>
                <IconButton
                  size="small"
                  color="primary"
                  onClick={(e) => {
                    e.stopPropagation();
                    dispatch(openTaskForm({ taskId: task.id }));
                  }}
                  sx={{ 
                    border: '1px solid',
                    borderColor: 'primary.main',
                    borderRadius: 1,
                    transition: TRANSITIONS.fast,
                    '&:hover': {
                      bgcolor: 'primary.main',
                      color: 'white',
                      transform: 'scale(1.05)',
                    },
                    '&:active': {
                      transform: 'scale(0.95)',
                    }
                  }}
                  aria-label="edit-task"
                  data-testid="task-edit-button"
                >
                  <EditIcon fontSize="small" />
                </IconButton>
                <IconButton
                  size="small"
                  color="error"
                  onClick={(e) => {
                    e.stopPropagation();
                    onDelete(task.id);
                  }}
                  sx={{ 
                    border: '1px solid',
                    borderColor: 'error.main',
                    borderRadius: 1,
                    transition: TRANSITIONS.fast,
                    '&:hover': {
                      bgcolor: 'error.main',
                      color: 'white',
                      transform: 'scale(1.05)',
                    },
                    '&:active': {
                      transform: 'scale(0.95)',
                    }
                  }}
                  aria-label="delete-task"
                  data-testid="task-delete-button"
                >
                  <DeleteIcon fontSize="small" />
                </IconButton>
              </Stack>
            </Stack>
          </Box>
        </Collapse>
      </CardContent>
    </Card>
  );
}, (prevProps, nextProps) => {
  return (
    prevProps.task.id === nextProps.task.id &&
    prevProps.task.rowVersion === nextProps.task.rowVersion &&
    prevProps.isDragging === nextProps.isDragging
  );
});
