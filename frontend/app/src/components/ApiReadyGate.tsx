import { useEffect, useState } from 'react';
import { Box, CircularProgress, Typography } from '@mui/material';
import { waitForApiReady } from '../services/apiService';

interface ApiReadyGateProps {
  children: React.ReactNode;
}

/**
 * Component that waits for API to be ready before rendering children.
 * Shows a loading screen while waiting.
 */
export function ApiReadyGate({ children }: ApiReadyGateProps) {
  const [isReady, setIsReady] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    let mounted = true;

    async function checkApiReady() {
      try {
        const ready = await waitForApiReady(60000); // Wait up to 60 seconds
        if (mounted) {
          if (ready) {
            setIsReady(true);
          } else {
            setError('API is not responding. Please check if the backend service is running.');
          }
        }
      } catch (err) {
        if (mounted) {
          setError(err instanceof Error ? err.message : 'Failed to connect to API');
        }
      }
    }

    checkApiReady();

    return () => {
      mounted = false;
    };
  }, []);

  if (error) {
    return (
      <Box
        sx={{
          display: 'flex',
          flexDirection: 'column',
          alignItems: 'center',
          justifyContent: 'center',
          minHeight: '100vh',
          gap: 2,
          p: 3,
        }}
      >
        <Typography variant="h5" color="error" gutterBottom>
          Connection Error
        </Typography>
        <Typography variant="body1" color="text.secondary" align="center">
          {error}
        </Typography>
        <Typography variant="body2" color="text.secondary" align="center" sx={{ mt: 2 }}>
          The API may still be starting up. Please wait a moment and refresh the page.
        </Typography>
      </Box>
    );
  }

  if (!isReady) {
    return (
      <Box
        sx={{
          display: 'flex',
          flexDirection: 'column',
          alignItems: 'center',
          justifyContent: 'center',
          minHeight: '100vh',
          gap: 2,
        }}
      >
        <CircularProgress size={48} />
        <Typography variant="h6" color="text.secondary">
          Initializing application...
        </Typography>
        <Typography variant="body2" color="text.secondary">
          Waiting for API to be ready
        </Typography>
      </Box>
    );
  }

  return <>{children}</>;
}

