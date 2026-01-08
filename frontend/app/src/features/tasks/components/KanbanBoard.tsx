import { useState, useEffect, memo, useMemo } from "react";
import { Box, Typography, Stack, useTheme, Button } from "@mui/material";
import { Add as AddIcon } from "@mui/icons-material";
import { DragDropContext, Droppable, Draggable } from "@hello-pangea/dnd";
import { TaskCard } from "./TaskCard";
import type { Task, TaskStatus } from "../types";
import { validateStatusChange } from "../utils/StatusValidation";
import { useAppDispatch } from "../../../store/hooks";
import { showSnackbar } from "../../../store/slices/uiSlice";
import { updateTask as updateTaskThunk } from "../../../store/slices/tasksSlice";
import { getStaggerDelay } from "../../../utils/animations";

interface KanbanColumnProps {
  title: string;
  status: TaskStatus;
  color: string;
  textColor: string;
  tasks: Task[];
  onDelete: (id: string) => void;
  onAddTask: (status: TaskStatus) => void;
}

const KanbanColumn = memo(function KanbanColumn({ title, status, color, textColor, tasks, onDelete, onAddTask }: KanbanColumnProps) {
  const theme = useTheme();
  
  return (
    <Box 
      data-testid={`kanban-column-${status.toLowerCase()}`}
      sx={{ 
      flex: 1,
      alignSelf: 'flex-start', // Ensure columns don't stretch to max height
      minWidth: { xs: 280, md: 0 },
      height: 'fit-content', // Allow column to shrink to fit content
      display: 'flex', 
      flexDirection: 'column',
      bgcolor: theme.palette.mode === 'light' ? '#f4f5f7' : 'grey.900',
      borderRadius: 2,
      pb: 2
    }}>
      <Box sx={{ 
        px: 2,
        py: 1.5, 
        display: 'flex', 
        alignItems: 'center', 
        justifyContent: 'space-between',
        background: `linear-gradient(135deg, ${color} 0%, ${color}DD 50%, ${color}BB 100%)`,
        // backdropFilter: 'blur(10px)', // Performance optimization: Removed expensive blur
        color: textColor,
        borderTopLeftRadius: 8,
        borderTopRightRadius: 8,
        mb: 2,
        boxShadow: '0 6px 16px rgba(0,0,0,0.15)',
        border: '1px solid rgba(255,255,255,0.3)',
        position: 'relative',
        overflow: 'hidden',
        '&::before': {
          content: '""',
          position: 'absolute',
          top: 0,
          left: 0,
          right: 0,
          height: '60%',
          background: 'linear-gradient(180deg, rgba(255,255,255,0.4) 0%, rgba(255,255,255,0.1) 50%, transparent 100%)',
          borderRadius: '8px 8px 0 0',
          pointerEvents: 'none',
        },
      }}>
        <Typography 
          variant="body2" 
          sx={{ 
            fontWeight: 'bold', 
            textTransform: 'uppercase', 
            letterSpacing: '0.5px', 
            fontSize: '0.8rem', 
            position: 'relative', 
            zIndex: 1,
            px: 2 // Align with button's internal text
          }}
        >
          {title}
        </Typography>
        <Typography 
          variant="caption" 
          sx={{ 
            fontWeight: 600, 
            px: 2, // Align with button's internal text
            py: 0.25, 
            borderRadius: 1, 
            position: 'relative', 
            zIndex: 1, 
            opacity: 0.8 
          }}
        >
          {tasks.length}
        </Typography>
      </Box>

      <Button
        data-testid={`create-task-button-${status.toLowerCase()}`}
        endIcon={<AddIcon />}
        onClick={() => onAddTask(status)}
        sx={{
          width: 'auto', // Allow margin to constrain width
          mb: 2,
          mx: 2, // Add horizontal margin to align with cards
          color: 'text.secondary', // Gray text
          bgcolor: 'rgba(0,0,0,0.03)',
          justifyContent: 'space-between',
          px: 2,
          '&:hover': { bgcolor: 'rgba(0,0,0,0.08)' }
        }}
      >
        Add New Task
      </Button>

      <Droppable droppableId={status}>
        {(provided, snapshot) => (
          <Box
            ref={provided.innerRef}
            {...provided.droppableProps}
            sx={{
              flexGrow: 1,
              minHeight: tasks.length === 0 ? 100 : 0, // Only enforce minHeight when empty to ensure drop target exists
              bgcolor: 'transparent',
              borderRadius: 1,
              transition: 'none',
              px: 2,
              // Removed execution of internal scroll to allow board to grow with content
            }}
          >
            <Stack spacing={1.5}>
              {tasks.map((task, index) => (
                <Draggable key={task.id} draggableId={task.id} index={index}>
                  {(provided, snapshot) => (
                    <Box
                      ref={provided.innerRef}
                      {...provided.draggableProps}
                      {...provided.dragHandleProps}
                      sx={{
                        ...provided.draggableProps.style,
                        opacity: snapshot.isDragging ? 0.8 : 1,
                        animation: snapshot.isDragging ? 'none' : `fadeInUp 0.3s ease-out ${getStaggerDelay(index, 30)}ms both`,
                      }}
                    >
                      <TaskCard 
                        task={task} 
                        onDelete={onDelete}
                        isDragging={snapshot.isDragging}
                      />
                    </Box>
                  )}
                </Draggable>
              ))}
              {snapshot.isDraggingOver ? provided.placeholder : null}
            </Stack>
          </Box>
        )}
      </Droppable>
    </Box>
  );
});

interface KanbanBoardProps {
  tasks: Task[];
  onDelete: (id: string) => void;
  onConflict: (task: Task) => void;
  onUpdateSuccess: () => void;
  onAddTask: (status: TaskStatus) => void;
}

const statusConfig: { status: TaskStatus, title: string, color: string, textColor: string }[] = [
  { status: 'Open', title: 'Not Started', color: '#f3f5f9', textColor: '#333333' },        // Very light gray - dark text
  { status: 'InProgress', title: 'In Progress', color: '#819fa7', textColor: '#ffffff' }, // Medium blue-gray - white text
  { status: 'Draft', title: 'In Review', color: '#5b6e74', textColor: '#ffffff' },        // Dark blue-gray - white text
  { status: 'Completed', title: 'Completed', color: '#f2f2f0', textColor: '#333333' },    // Off-white - dark text
  { status: 'Cancelled', title: 'Cancelled', color: '#0d0d0d', textColor: '#ffffff' },    // Almost black - white text
  { status: 'Overdue', title: 'Overdue', color: '#E74C3C', textColor: '#ffffff' }         // Red - white text
];

export function KanbanBoard({ tasks, onDelete, onConflict, onUpdateSuccess, onAddTask }: KanbanBoardProps) {
  const dispatch = useAppDispatch();
  const [optimisticTasks, setOptimisticTasks] = useState(tasks);

  useEffect(() => {
    setOptimisticTasks(tasks);
  }, [tasks]);
  

  
  const handleDragEnd = async (result: any) => {
    const { destination, source, draggableId } = result;

    if (!destination) return;
    if (destination.droppableId === source.droppableId) return;

    const newStatus = destination.droppableId as TaskStatus;
    const task = optimisticTasks.find(t => t.id === draggableId);
    
    if (task) {
      // Validate status change
      const validation = validateStatusChange(task, newStatus);
      if (!validation.isValid) {
        dispatch(showSnackbar({ message: validation.error || "Invalid status change", severity: "error" }));
        return;
      }

      // Optimistic update
      const previousTasks = [...optimisticTasks];
      const updatedTasks = optimisticTasks.map(t => 
        t.id === task.id ? { ...t, status: newStatus } : t
      );
      setOptimisticTasks(updatedTasks);

      try {
        const updatedTask = await dispatch(updateTaskThunk({
          id: task.id,
          data: {
            title: task.title,
            description: task.description,
            dueDateUtc: task.dueDateUtc,
            priority: task.priority,
            status: newStatus,
            assignedUserId: task.assignee?.id || undefined,
            ownerUserId: task.owner?.id || undefined
          },
          rowVersion: task.rowVersion
        })).unwrap();
        
        // Immediately update optimisticTasks with the server response (including new rowVersion)
        // This prevents stale rowVersion issues when dragging the same task multiple times quickly
        setOptimisticTasks(prev => prev.map(t => 
          t.id === updatedTask.id ? updatedTask : t
        ));
        
        // Success - no refetch needed, Redux store already updated via updateTask.fulfilled
        dispatch(showSnackbar({ message: "Task status updated successfully", severity: "success" }));
      } catch (err: any) {
        // Revert on failure
        setOptimisticTasks(previousTasks);
        
        if (err?.status === 409) {
          onConflict(task);
        } else if (err?.message?.includes('404') || err?.message?.includes('Not Found')) {
          // Task doesn't exist on server - likely deleted
          dispatch(showSnackbar({ 
            message: "This task no longer exists. Refreshing task list...", 
            severity: "warning" 
          }));
          // Trigger a refetch to sync with server state
          onUpdateSuccess();
        } else {
          dispatch(showSnackbar({ message: err.message || "Failed to update task status", severity: "error" }));
        }
      }
    }
  };

  // Optimize tasksByStatus to return stable array references
  // This prevents KanbanColumn from re-rendering if its tasks haven't changed
  const tasksByStatus = useMemo(() => {
    const newGrouped = optimisticTasks.reduce((acc, task) => {
      if (!acc[task.status]) acc[task.status] = [];
      acc[task.status].push(task);
      return acc;
    }, {} as Record<string, Task[]>);

    // Ensure all statuses exist
    statusConfig.forEach(config => {
      if (!newGrouped[config.status]) newGrouped[config.status] = [];
    });

    return newGrouped;
  }, [optimisticTasks]);

  // Note: We rely on useMemo for tasksByStatus to provide stable references
  // The memo will only recompute when optimisticTasks changes


  return (
    <DragDropContext onDragEnd={handleDragEnd}>
      <Box sx={{ 
        display: 'flex', 
        gap: 2, 
        overflowX: 'auto', 
        pb: 3, 
        // minHeight removed to let content dictate height
        alignItems: 'flex-start'
      }}>
        {statusConfig.map(config => (
          <KanbanColumn 
            key={config.status}
            title={config.title}
            status={config.status}
            color={config.color}
            textColor={config.textColor}
            tasks={tasksByStatus[config.status] || []}
            onDelete={onDelete}
            onAddTask={onAddTask}
          />
        ))}
      </Box>
    </DragDropContext>
  );
}
