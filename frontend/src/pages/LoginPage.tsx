import { useState, type FormEvent } from 'react';
import {
  Alert, Box, Button, CircularProgress, Link as MuiLink, Stack, TextField, Typography
} from '@mui/material';
import { Link, useLocation, useNavigate } from 'react-router-dom';
import AutoAwesomeRoundedIcon from '@mui/icons-material/AutoAwesomeRounded';
import { useAuth } from '../auth/AuthContext';
import { getErrorMessage } from '../utils/errors';

export default function LoginPage() {
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [error, setError] = useState('');
  const [loading, setLoading] = useState(false);

  const { login } = useAuth();
  const navigate = useNavigate();
  const location = useLocation();

  const destination = (location.state as { from?: { pathname?: string } } | null)?.from?.pathname ?? '/';

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setError('');
    setLoading(true);

    try {
      await login({ email, password });
      navigate(destination, { replace: true });
    } catch (e) {
      setError(getErrorMessage(e, 'Unable to sign in. Check your email and password.'));
    } finally {
      setLoading(false);
    }
  }

  return (
    <Box className="auth-background">
      <Box className="auth-art">
        <Box sx={{ display: 'flex', alignItems: 'center', gap: 1.5 }}>
          <Box sx={{ width: 44, height: 44, borderRadius: 3, bgcolor: 'rgba(255,255,255,.18)', display: 'grid', placeItems: 'center' }}>
            <AutoAwesomeRoundedIcon />
          </Box>
          <Typography fontWeight={800} fontSize={20}>ATS Resume Checker</Typography>
        </Box>
        <Box sx={{ maxWidth: 520, py: 8 }}>
          <Typography variant="overline" sx={{ letterSpacing: '.16em', opacity: 0.8 }}>RESUME INTELLIGENCE</Typography>
          <Typography sx={{ fontSize: { xs: 34, md: 48 }, lineHeight: 1.1, fontWeight: 800, letterSpacing: '-.05em', mt: 2 }}>
            Find the roles your resume is built for.
          </Typography>
          <Typography sx={{ mt: 2.5, fontSize: 17, lineHeight: 1.7, opacity: 0.82 }}>
            Compare your experience against real job requirements and understand where your strongest matches are.
          </Typography>
        </Box>
      </Box>

      <Box className="auth-form-panel">
        <Box className="auth-form">
          <Typography variant="h1">Welcome back</Typography>
          <Typography color="text.secondary" sx={{ mt: 1, mb: 4 }}>
            Sign in to continue to your analysis workspace.
          </Typography>

          {error && <Alert severity="error" sx={{ mb: 2 }}>{error}</Alert>}

          <Box component="form" onSubmit={handleSubmit}>
            <Stack spacing={2.3}>
              <TextField
                label="Email address"
                type="email"
                autoComplete="email"
                value={email}
                onChange={(e) => setEmail(e.target.value)}
                required
              />
              <TextField
                label="Password"
                type="password"
                autoComplete="current-password"
                value={password}
                onChange={(e) => setPassword(e.target.value)}
                required
                inputProps={{ minLength: 8, maxLength: 128 }}
              />
              <Button type="submit" size="large" variant="contained" disabled={loading} sx={{ py: 1.35 }}>
                {loading ? <CircularProgress size={23} color="inherit" /> : 'Sign in'}
              </Button>
            </Stack>
          </Box>

          <Typography color="text.secondary" textAlign="center" sx={{ mt: 3 }}>
            New to ATS Resume Checker?{' '}
            <MuiLink component={Link} to="/register" underline="hover" fontWeight={700}>
              Create an account
            </MuiLink>
          </Typography>
        </Box>
      </Box>
    </Box>
  );
}
