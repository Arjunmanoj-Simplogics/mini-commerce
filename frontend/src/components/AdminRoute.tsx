import { Navigate } from 'react-router-dom';
import { CircularProgress, Stack } from '@mui/material';
import type { ReactNode } from 'react';
import { useAuth } from '../hooks/useAuth';

export default function AdminRoute({ children }: { children: ReactNode }) {
  const { ready, isAuthenticated, user } = useAuth();

  if (!ready) {
    return (
      <Stack sx={{ alignItems: 'center', py: 8 }}>
        <CircularProgress />
      </Stack>
    );
  }

  if (!isAuthenticated) return <Navigate to="/login" replace />;
  if (user?.role !== 'Admin') return <Navigate to="/" replace />;
  return children;
}
