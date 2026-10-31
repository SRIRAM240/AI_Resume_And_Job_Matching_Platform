import { useState, type FormEvent } from 'react';
import {
  Alert, Box, Button, CircularProgress, Link as MuiLink, Stack, TextField, Typography
} from '@mui/material';
import { Link, useNavigate } from 'react-router-dom';
import AutoAwesomeRoundedIcon from '@mui/icons-material/AutoAwesomeRounded';
import { useAuth } from '../auth/AuthContext';
import { getErrorMessage } from '../utils/errors';

export default function RegisterPage() {
  const [fullName, setFullName] = useState('');
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [error, setError] = useState('');
  const [loading, setLoading] = useState(false);

  const { register } = useAuth();
  const navigate = useNavigate();

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setError('');

    if (fullName.trim().length < 2) {
      setError('Please enter your full name (at least 2 characters).');
      return;
    }
    if (password.length < 8) {
      setError('Password must contain at least 8 characters.');
      return;
    }

    setLoading(true);
    try {
      await register({ fullName: fullName.trim(), email: email.trim(), password });
      navigate('/', { replace: true });
    } catch (e) {
      setError(getErrorMessage(e, 'Unable to create your account.'));
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
          <Typography sx={{ mt: 2.5, fontSize: 17, lineHeight: 1.7, opacity: 0.82 }}>
            Upload your resume, and compare it with the jobs that matter to you.
          </Typography>
        </Box>
      </Box>

      <Box className="auth-form-panel">
        <Box className="auth-form">
          <Typography variant="h1">Create your account</Typography>
          <Typography color="text.secondary" sx={{ mt: 1, mb: 4 }}>
            Get started by creating your workspace account.
          </Typography>

          {error && <Alert severity="error" sx={{ mb: 2 }}>{error}</Alert>}

          <Box component="form" onSubmit={handleSubmit}>
            <Stack spacing={2.1}>
              <TextField
                label="Full name"
                autoComplete="name"
                value={fullName}
                onChange={(e) => setFullName(e.target.value)}
                required
                inputProps={{ minLength: 2, maxLength: 100 }}
              />
              <TextField
                label="Email address"
                type="email"
                autoComplete="email"
                value={email}
                onChange={(e) => setEmail(e.target.value)}
                required
                inputProps={{ maxLength: 255 }}
              />
              <TextField
                label="Password"
                type="password"
                autoComplete="new-password"
                value={password}
                onChange={(e) => setPassword(e.target.value)}
                required
                helperText="Use at least 8 characters."
                inputProps={{ minLength: 8, maxLength: 128 }}
              />
              <Button type="submit" size="large" variant="contained" disabled={loading} sx={{ py: 1.35 }}>
                {loading ? <CircularProgress size={23} color="inherit" /> : 'Create account'}
              </Button>
            </Stack>
          </Box>

          <Typography color="text.secondary" textAlign="center" sx={{ mt: 3 }}>
            Already registered?{' '}
            <MuiLink component={Link} to="/login" underline="hover" fontWeight={700}>
              Sign in
            </MuiLink>
          </Typography>
        </Box>
      </Box>
    </Box>
  );
}
