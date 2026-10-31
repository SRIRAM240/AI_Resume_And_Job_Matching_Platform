import { useEffect, useState, type FormEvent } from 'react';
import {
  Alert, Box, Button, Card, CardContent, CircularProgress, Stack, TextField, Typography
} from '@mui/material';
import { profileApi } from '../api/profile.api';
import type { UserProfile } from '../types/api';
import { getErrorMessage } from '../utils/errors';
import { useAuth } from '../auth/AuthContext';

export default function ProfilePage() {
  const [profile, setProfile] = useState<UserProfile | null>(null);
  const [fullName, setFullName] = useState('');
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState('');
  const [success, setSuccess] = useState('');
  const { user } = useAuth();

  useEffect(() => {
    profileApi.getMe()
      .then((data) => {
        setProfile(data);
        setFullName(data.fullName);
      })
      .catch((e) => setError(getErrorMessage(e, 'Could not load profile.')))
      .finally(() => setLoading(false));
  }, []);

  async function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setSaving(true);
    setError('');
    setSuccess('');

    try {
      const updated = await profileApi.updateMe(fullName.trim());
      setProfile(updated);
      setFullName(updated.fullName);
      setSuccess('Profile updated successfully.');
    } catch (e) {
      setError(getErrorMessage(e, 'Could not update profile.'));
    } finally {
      setSaving(false);
    }
  }

  return (
    <Box className="page-shell">
      <Box className="page-header">
        <Box>
          <Typography variant="h1">My profile</Typography>
          <Typography color="text.secondary" sx={{ mt: 1 }}>
            Manage the account details used in your workspace.
          </Typography>
        </Box>
      </Box>

      {error && <Alert severity="error" sx={{ mb: 2 }} onClose={() => setError('')}>{error}</Alert>}
      {success && <Alert severity="success" sx={{ mb: 2 }} onClose={() => setSuccess('')}>{success}</Alert>}

      <Card sx={{ maxWidth: 720 }}>
        <CardContent sx={{ p: { xs: 2.5, md: 4 } }}>
          {loading ? (
            <CircularProgress />
          ) : (
            <Box component="form" onSubmit={submit}>
              <Stack spacing={2.5}>
                <Typography variant="h3">Account information</Typography>
                <TextField
                  label="Full name"
                  value={fullName}
                  onChange={(e) => setFullName(e.target.value)}
                  required
                  inputProps={{ minLength: 2, maxLength: 100 }}
                />
                <TextField
                  label="Email address"
                  value={profile?.email ?? user?.email ?? ''}
                  disabled
                  helperText="Email changes are not supported by the current profile API."
                />
                <TextField
                  label="Member since"
                  value={profile?.createdAt ? new Date(profile.createdAt).toLocaleDateString() : ''}
                  disabled
                />
                <Box>
                  <Button type="submit" variant="contained" disabled={saving || !fullName.trim()}>
                    {saving ? <CircularProgress size={20} /> : 'Save changes'}
                  </Button>
                </Box>
              </Stack>
            </Box>
          )}
        </CardContent>
      </Card>
    </Box>
  );
}
