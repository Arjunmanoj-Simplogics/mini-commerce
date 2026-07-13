import { Alert, Box, Button, Stack, TextField, Typography } from '@mui/material';
import { type FormEvent, useState } from 'react';
import { Link as RouterLink, useNavigate } from 'react-router-dom';
import { useAuth } from '../hooks/useAuth';

export default function LoginPage() {
  const { login } = useAuth();
  const navigate = useNavigate();
  const [email, setEmail] = useState('admin@minicommerce.local');
  const [password, setPassword] = useState('Admin123!');
  const [error, setError] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);

  const onSubmit = async (e: FormEvent) => {
    e.preventDefault();
    setBusy(true);
    setError(null);
    try {
      await login(email, password);
      navigate('/shop');
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Login failed');
    } finally {
      setBusy(false);
    }
  };

  return (
    <Stack spacing={3} className="animate-rise" sx={{ maxWidth: 420, mx: 'auto' }}>
      <Typography variant="h3">Sign in</Typography>
      <Box className="glass-panel" sx={{ borderRadius: 4, p: 3 }}>
        {error && (
          <Alert severity="error" sx={{ mb: 2 }}>
            {error}
          </Alert>
        )}
        <Stack component="form" spacing={2} onSubmit={onSubmit}>
          <TextField label="Email" value={email} onChange={(e) => setEmail(e.target.value)} required fullWidth />
          <TextField
            label="Password"
            type="password"
            value={password}
            onChange={(e) => setPassword(e.target.value)}
            required
            fullWidth
          />
          <Button type="submit" variant="contained" color="secondary" disabled={busy}>
            {busy ? 'Signing in…' : 'Sign in'}
          </Button>
          <Button component={RouterLink} to="/register">
            Create an account
          </Button>
        </Stack>
      </Box>
    </Stack>
  );
}
