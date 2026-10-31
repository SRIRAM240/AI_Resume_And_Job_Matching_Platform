import { useCallback, useEffect, useState, type FormEvent } from 'react';
import {
  Alert, Box, Button, Card, CardContent, Chip, CircularProgress,
  Dialog, DialogActions, DialogContent, DialogTitle, Grid, Stack, TextField, Typography
} from '@mui/material';
import AddRoundedIcon from '@mui/icons-material/AddRounded';
import EditRoundedIcon from '@mui/icons-material/EditRounded';
import DeleteOutlineRoundedIcon from '@mui/icons-material/DeleteOutlineRounded';
import WorkOutlineRoundedIcon from '@mui/icons-material/WorkOutlineRounded';
import { jobsApi } from '../api/jobs.api';
import type { Job, JobRequest } from '../types/api';
import { getErrorMessage } from '../utils/errors';

const emptyForm: JobRequest = {
  title: '',
  companyName: '',
  description: '',
  requiredSkills: '',
  location: '',
  employmentType: 'Full-time'
};

export default function JobsPage() {
  const [jobs, setJobs] = useState<Job[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [success, setSuccess] = useState('');
  const [open, setOpen] = useState(false);
  const [editingJobId, setEditingJobId] = useState<number | null>(null);
  const [deleteTarget, setDeleteTarget] = useState<Job | null>(null);
  const [deleting, setDeleting] = useState(false);
  const [form, setForm] = useState<JobRequest>(emptyForm);
  const [saving, setSaving] = useState(false);

  const load = useCallback(async () => {
    setLoading(true);
    try {
      const data = await jobsApi.list();
      setJobs(data);
      setError('');
    } catch (e) {
      setError(getErrorMessage(e, 'Could not load jobs.'));
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    void load();
  }, [load]);

  function handleOpenAdd() {
    setEditingJobId(null);
    setForm(emptyForm);
    setOpen(true);
  }

  function handleEdit(job: Job) {
    setEditingJobId(job.id);
    setForm({
      title: job.title,
      companyName: job.companyName,
      description: job.description,
      requiredSkills: job.requiredSkills || '',
      location: job.location || '',
      employmentType: job.employmentType || 'Full-time'
    });
    setOpen(true);
  }

  async function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setSaving(true);
    setError('');

    const payload = {
      ...form,
      title: form.title.trim(),
      companyName: form.companyName.trim(),
      description: form.description.trim(),
      requiredSkills: form.requiredSkills.trim()
    };

    try {
      if (editingJobId) {
        await jobsApi.update(editingJobId, payload);
        setSuccess('Job description updated.');
      } else {
        await jobsApi.create(payload);
        setSuccess('Job description created.');
      }
      setOpen(false);
      setForm(emptyForm);
      setEditingJobId(null);
      await load();
    } catch (e) {
      setError(getErrorMessage(e, editingJobId ? 'Could not update the job.' : 'Could not create the job.'));
    } finally {
      setSaving(false);
    }
  }

  async function handleDeleteJob() {
    if (!deleteTarget) return;
    setDeleting(true);
    try {
      await jobsApi.remove(deleteTarget.id);
      setSuccess('Job description deleted.');
      setDeleteTarget(null);
      await load();
    } catch (e) {
      setError(getErrorMessage(e, 'Could not delete the job.'));
      setDeleteTarget(null);
    } finally {
      setDeleting(false);
    }
  }

  return (
    <Box className="page-shell">
      <Box className="page-header">
        <Box>
          <Typography variant="h1">Job descriptions</Typography>
        </Box>
        <Button variant="contained" startIcon={<AddRoundedIcon />} onClick={handleOpenAdd}>
          Add job description
        </Button>
      </Box>

      {error && <Alert severity="error" sx={{ mb: 2 }} onClose={() => setError('')}>{error}</Alert>}
      {success && <Alert severity="success" sx={{ mb: 2 }} onClose={() => setSuccess('')}>{success}</Alert>}

      {loading ? (
        <Box sx={{ display: 'grid', placeItems: 'center', py: 8 }}>
          <CircularProgress />
        </Box>
      ) : jobs.length === 0 ? (
        <Card>
          <CardContent sx={{ py: 6, textAlign: 'center' }}>
            <WorkOutlineRoundedIcon sx={{ fontSize: 44, color: 'text.secondary' }} />
            <Typography variant="h3" sx={{ mt: 1.5 }}>No job descriptions found</Typography>
            <Typography color="text.secondary" sx={{ mt: 1 }}>
              Add a job description to make it available for matching.
            </Typography>
          </CardContent>
        </Card>
      ) : (
        <Grid container spacing={2}>
          {jobs.map((job) => (
            <Grid key={job.id} size={{ xs: 12, md: 6, xl: 4 }}>
              <Card sx={{ height: '100%', display: 'flex', flexDirection: 'column' }}>
                <CardContent sx={{ p: 2.8, flex: 1, display: 'flex', flexDirection: 'column' }}>
                  <Typography variant="h3">{job.title}</Typography>
                  <Typography color="text.secondary" sx={{ mt: 0.6 }}>{job.companyName}</Typography>
                  <Stack direction="row" spacing={1} sx={{ mt: 2, flexWrap: 'wrap', gap: 0.5 }}>
                    <Chip size="small" label={job.location || 'Location not specified'} />
                    <Chip size="small" label={job.employmentType || 'Type not specified'} />
                  </Stack>
                  <Typography variant="body2" sx={{ mt: 2, lineHeight: 1.75 }}>
                    {job.description.length > 210 ? `${job.description.slice(0, 210)}…` : job.description}
                  </Typography>
                  <Typography variant="caption" color="text.secondary" display="block" sx={{ mt: 2 }}>
                    Required skills
                  </Typography>
                  <Typography variant="body2" fontWeight={650} sx={{ mt: 0.5 }}>
                    {job.requiredSkills || 'Not specified'}
                  </Typography>

                  <Box sx={{ mt: 'auto', pt: 2, display: 'flex', justifyContent: 'flex-end', gap: 1 }}>
                    <Button
                      size="small"
                      variant="outlined"
                      startIcon={<EditRoundedIcon />}
                      onClick={() => handleEdit(job)}
                    >
                      Edit
                    </Button>
                    <Button
                      size="small"
                      variant="outlined"
                      color="error"
                      startIcon={<DeleteOutlineRoundedIcon />}
                      onClick={() => setDeleteTarget(job)}
                    >
                      Delete
                    </Button>
                  </Box>
                </CardContent>
              </Card>
            </Grid>
          ))}
        </Grid>
      )}

      <Dialog open={open} onClose={() => setOpen(false)} fullWidth maxWidth="md">
        <Box component="form" onSubmit={submit}>
          <DialogTitle>{editingJobId ? 'Edit job description' : 'Add job description'}</DialogTitle>
          <DialogContent>
            <Stack spacing={2} sx={{ pt: 1 }}>
              <TextField
                label="Job title"
                value={form.title}
                onChange={(e) => setForm({ ...form, title: e.target.value })}
                required
                inputProps={{ minLength: 2, maxLength: 200 }}
              />
              <TextField
                label="Company name"
                value={form.companyName}
                onChange={(e) => setForm({ ...form, companyName: e.target.value })}
                required
                inputProps={{ minLength: 2, maxLength: 200 }}
              />
              <TextField
                label="Job description (at least 20 characters)"
                value={form.description}
                onChange={(e) => setForm({ ...form, description: e.target.value })}
                required
                multiline
                minRows={4}
                inputProps={{ minLength: 20 }}
              />
              <TextField
                label="Required skills (comma-separated)"
                value={form.requiredSkills}
                onChange={(e) => setForm({ ...form, requiredSkills: e.target.value })}
                required
                multiline
                minRows={2}
                inputProps={{ maxLength: 2000 }}
                helperText="Example: C#, ASP.NET Core, Web API, SQL Server"
              />
              <Grid container spacing={2}>
                <Grid size={{ xs: 12, sm: 6 }}>
                  <TextField
                    label="Location"
                    value={form.location}
                    onChange={(e) => setForm({ ...form, location: e.target.value })}
                    inputProps={{ maxLength: 150 }}
                  />
                </Grid>
                <Grid size={{ xs: 12, sm: 6 }}>
                  <TextField
                    label="Employment type"
                    value={form.employmentType}
                    onChange={(e) => setForm({ ...form, employmentType: e.target.value })}
                    inputProps={{ maxLength: 50 }}
                    placeholder="Full-time"
                  />
                </Grid>
              </Grid>
            </Stack>
          </DialogContent>
          <DialogActions sx={{ p: 2.5 }}>
            <Button onClick={() => setOpen(false)}>Cancel</Button>
            <Button type="submit" variant="contained" disabled={saving}>
              {saving ? <CircularProgress size={20} /> : editingJobId ? 'Update job' : 'Save job'}
            </Button>
          </DialogActions>
        </Box>
      </Dialog>

      {/* Delete confirmation dialog */}
      <Dialog open={Boolean(deleteTarget)} onClose={() => setDeleteTarget(null)}>
        <DialogTitle>Delete this job description?</DialogTitle>
        <DialogContent>
          <Typography>This will remove &quot;{deleteTarget?.title}&quot; from available jobs.</Typography>
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setDeleteTarget(null)}>Cancel</Button>
          <Button color="error" variant="contained" disabled={deleting} onClick={() => void handleDeleteJob()}>
            {deleting ? <CircularProgress size={20} color="inherit" /> : 'Delete'}
          </Button>
        </DialogActions>
      </Dialog>
    </Box>
  );
}
