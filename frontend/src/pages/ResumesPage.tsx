import { useCallback, useEffect, useRef, useState } from 'react';
import {
  Alert, Box, Button, Card, CardContent, CircularProgress,
  Dialog, DialogActions, DialogContent, DialogTitle, IconButton, Stack, Typography
} from '@mui/material';
import UploadFileRoundedIcon from '@mui/icons-material/UploadFileRounded';
import DownloadRoundedIcon from '@mui/icons-material/DownloadRounded';
import DeleteOutlineRoundedIcon from '@mui/icons-material/DeleteOutlineRounded';
import DescriptionRoundedIcon from '@mui/icons-material/DescriptionRounded';
import { resumesApi } from '../api/resumes.api';
import type { ResumeItem } from '../types/api';
import { getErrorMessage } from '../utils/errors';

export default function ResumesPage() {
  const [resumes, setResumes] = useState<ResumeItem[]>([]);
  const [loading, setLoading] = useState(true);
  const [uploading, setUploading] = useState(false);
  const [error, setError] = useState('');
  const [success, setSuccess] = useState('');
  const [deleteTarget, setDeleteTarget] = useState<ResumeItem | null>(null);
  const inputRef = useRef<HTMLInputElement>(null);

  const load = useCallback(async () => {
    setLoading(true);
    try {
      const data = await resumesApi.list();
      setResumes(data);
      setError('');
    } catch (e) {
      setError(getErrorMessage(e, 'Could not load resumes.'));
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    void load();
  }, [load]);

  async function upload(file?: File) {
    if (!file) return;
    setError('');
    setSuccess('');

    if (!/\.(pdf|docx)$/i.test(file.name)) {
      setError('Choose a PDF or DOCX file.');
      return;
    }
    if (file.size > 5 * 1024 * 1024) {
      setError('The backend currently accepts files up to 5 MB.');
      return;
    }

    setUploading(true);
    try {
      const result = await resumesApi.upload(file);
      setSuccess(result.message || 'Resume uploaded successfully.');
      await load();
    } catch (e) {
      setError(getErrorMessage(e, 'Resume upload failed.'));
    } finally {
      setUploading(false);
      if (inputRef.current) inputRef.current.value = '';
    }
  }

  async function deleteResume() {
    if (!deleteTarget) return;
    try {
      await resumesApi.remove(deleteTarget.id);
      setSuccess('Resume deleted.');
      setDeleteTarget(null);
      await load();
    } catch (e) {
      setError(getErrorMessage(e, 'Could not delete resume.'));
      setDeleteTarget(null);
    }
  }

  return (
    <Box className="page-shell">
      <Box className="page-header">
        <Box>
          <Typography variant="h1">My resumes</Typography>
          <Typography color="text.secondary" sx={{ mt: 1 }}>
            Upload and manage your resumes.
          </Typography>
        </Box>
        <Button
          variant="contained"
          startIcon={uploading ? <CircularProgress size={18} color="inherit" /> : <UploadFileRoundedIcon />}
          onClick={() => inputRef.current?.click()}
          disabled={uploading}
        >
          {uploading ? 'Uploading…' : 'Upload resume'}
        </Button>
        <input
          ref={inputRef}
          type="file"
          accept=".pdf,.docx,application/pdf,application/vnd.openxmlformats-officedocument.wordprocessingml.document"
          hidden
          onChange={(e) => void upload(e.target.files?.[0])}
        />
      </Box>

      {error && <Alert severity="error" sx={{ mb: 2 }} onClose={() => setError('')}>{error}</Alert>}
      {success && <Alert severity="success" sx={{ mb: 2 }} onClose={() => setSuccess('')}>{success}</Alert>}

      {loading ? (
        <Box sx={{ display: 'grid', placeItems: 'center', py: 8 }}>
          <CircularProgress />
        </Box>
      ) : resumes.length === 0 ? (
        <Card>
          <CardContent sx={{ py: 7, textAlign: 'center' }}>
            <DescriptionRoundedIcon sx={{ fontSize: 44, color: 'text.secondary' }} />
            <Typography variant="h3" sx={{ mt: 2 }}>No resumes yet</Typography>
            <Typography color="text.secondary" sx={{ mt: 1 }}>
              Upload a PDF or DOCX to start comparing your experience against jobs.
            </Typography>
            <Button sx={{ mt: 2 }} variant="contained" onClick={() => inputRef.current?.click()}>
              Upload resume
            </Button>
          </CardContent>
        </Card>
      ) : (
        <Stack spacing={1.5}>
          {resumes.map((resume) => (
            <Card key={resume.id}>
              <CardContent sx={{ display: 'flex', alignItems: 'center', gap: 2, flexWrap: 'wrap' }}>
                <Box sx={{ bgcolor: '#efedff', color: 'primary.main', p: 1.4, borderRadius: 2.5 }}>
                  <DescriptionRoundedIcon />
                </Box>
                <Box sx={{ flex: 1, minWidth: 180 }}>
                  <Typography fontWeight={750}>{resume.fileName}</Typography>
                  <Typography variant="body2" color="text.secondary">
                    Uploaded {new Date(resume.uploadedAt).toLocaleString()}
                  </Typography>
                </Box>
                <IconButton aria-label={`Download ${resume.fileName}`} onClick={() => void resumesApi.download(resume.id, resume.fileName)}>
                  <DownloadRoundedIcon />
                </IconButton>
                <IconButton aria-label={`Delete ${resume.fileName}`} color="error" onClick={() => setDeleteTarget(resume)}>
                  <DeleteOutlineRoundedIcon />
                </IconButton>
              </CardContent>
            </Card>
          ))}
        </Stack>
      )}

      {/* Delete dialog */}
      <Dialog open={Boolean(deleteTarget)} onClose={() => setDeleteTarget(null)}>
        <DialogTitle>Delete this resume?</DialogTitle>
        <DialogContent>
          <Typography>This will remove {deleteTarget?.fileName} from your account.</Typography>
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setDeleteTarget(null)}>Cancel</Button>
          <Button color="error" onClick={() => void deleteResume()}>Delete</Button>
        </DialogActions>
      </Dialog>
    </Box>
  );
}
