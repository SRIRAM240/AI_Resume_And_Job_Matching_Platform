import { useEffect, useState } from 'react';
import {
  Alert, Box, Button, Card, CardContent, CircularProgress, Divider, Grid, Stack, Typography
} from '@mui/material';
import UploadFileRoundedIcon from '@mui/icons-material/UploadFileRounded';
import CompareArrowsRoundedIcon from '@mui/icons-material/CompareArrowsRounded';
import DescriptionRoundedIcon from '@mui/icons-material/DescriptionRounded';
import ArrowForwardRoundedIcon from '@mui/icons-material/ArrowForwardRounded';
import { Link } from 'react-router-dom';
import { jobsApi } from '../api/jobs.api';
import { profileApi } from '../api/profile.api';
import { resumesApi } from '../api/resumes.api';
import type { Job, ResumeItem, UserProfile } from '../types/api';

function Metric({ label, value }: { label: string; value: string | number }) {
  return (
    <Box sx={{ py: 1.5, px: 2.25, borderLeft: { sm: '1px solid #e6e8ef' }, '&:first-of-type': { borderLeft: 0 }, minWidth: 0 }}>
      <Typography variant="body2" color="text.secondary">{label}</Typography>
      <Typography sx={{ mt: 0.5, fontSize: '1.7rem', fontWeight: 750, letterSpacing: '-.04em', color: '#20283b' }}>
        {value}
      </Typography>
    </Box>
  );
}

export default function DashboardPage() {
  const [profile, setProfile] = useState<UserProfile | null>(null);
  const [resumes, setResumes] = useState<ResumeItem[]>([]);
  const [jobs, setJobs] = useState<Job[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');

  useEffect(() => {
    let active = true;
    Promise.allSettled([profileApi.getMe(), resumesApi.list(), jobsApi.list()])
      .then(([profileRes, resumesRes, jobsRes]) => {
        if (!active) return;
        if (profileRes.status === 'fulfilled') setProfile(profileRes.value);
        if (resumesRes.status === 'fulfilled') setResumes(resumesRes.value);
        if (jobsRes.status === 'fulfilled') setJobs(jobsRes.value);
        if ([profileRes, resumesRes, jobsRes].some((r) => r.status === 'rejected')) {
          setError('Some information could not be loaded. Check API connection and try refreshing.');
        }
      })
      .finally(() => {
        if (active) setLoading(false);
      });

    return () => { active = false; };
  }, []);

  const firstName = profile?.fullName?.trim().split(/\s+/)[0] || 'there';

  return (
    <Box className="page-shell">
      <Box className="page-header">
        <Box>
          <Typography variant="h1">Good to see you, {firstName}</Typography>
        </Box>
        <Button component={Link} to="/compare" variant="contained" startIcon={<CompareArrowsRoundedIcon />}>
          Compare a resume
        </Button>
      </Box>

      {error && <Alert severity="warning" sx={{ mb: 2 }}>{error}</Alert>}

      {loading ? (
        <Box sx={{ display: 'grid', placeItems: 'center', py: 8 }}>
          <CircularProgress />
        </Box>
      ) : (
        <>
          <Card sx={{ mb: 3, boxShadow: 'none' }}>
            <Box sx={{ display: 'grid', gridTemplateColumns: { xs: '1fr', sm: 'repeat(2, 1fr)' } }}>
              <Metric label="Your resumes" value={resumes.length} />
              <Metric label="Available jobs" value={jobs.length} />
            </Box>
          </Card>

          <Grid container spacing={3}>
            <Grid size={{ xs: 12, lg: 7 }}>
              <Card sx={{ height: '100%', boxShadow: 'none' }}>
                <CardContent sx={{ p: { xs: 2.5, md: 3 } }}>
                  <Typography variant="overline" sx={{ color: 'primary.main', fontWeight: 750, letterSpacing: '.08em' }}>
                    GET STARTED
                  </Typography>
                  <Typography variant="h2" sx={{ mt: 0.5 }}>Find roles that fit</Typography>
                  <Stack direction={{ xs: 'column', sm: 'row' }} spacing={1.25} sx={{ mt: 2.5 }}>
                    <Button component={Link} to="/compare" variant="contained" endIcon={<ArrowForwardRoundedIcon />}>
                      Compare jobs
                    </Button>
                    <Button component={Link} to="/resumes" variant="outlined" startIcon={<UploadFileRoundedIcon />}>
                      Manage resumes
                    </Button>
                  </Stack>
                </CardContent>
              </Card>
            </Grid>

            <Grid size={{ xs: 12, lg: 5 }}>
              <Card sx={{ height: '100%', boxShadow: 'none' }}>
                <CardContent sx={{ p: { xs: 2.5, md: 3 } }}>
                  <Stack direction="row" alignItems="center" justifyContent="space-between" sx={{ mb: 1.5 }}>
                    <Typography variant="h3">Recent resumes</Typography>
                    <Button component={Link} to="/resumes" size="small" endIcon={<ArrowForwardRoundedIcon />}>
                      View all
                    </Button>
                  </Stack>
                  {resumes.length === 0 ? (
                    <Box sx={{ py: 2 }}>
                      <Typography color="text.secondary">No resumes uploaded yet.</Typography>
                      <Button component={Link} to="/resumes" sx={{ mt: 1 }} startIcon={<UploadFileRoundedIcon />}>
                        Upload a resume
                      </Button>
                    </Box>
                  ) : (
                    <Stack divider={<Divider flexItem />}>
                      {resumes.slice(0, 4).map((resume) => (
                        <Stack key={resume.id} direction="row" spacing={1.5} alignItems="center" sx={{ py: 1.5 }}>
                          <DescriptionRoundedIcon sx={{ color: 'text.secondary' }} />
                          <Box sx={{ minWidth: 0, flex: 1 }}>
                            <Typography fontWeight={650} noWrap>{resume.fileName}</Typography>
                            <Typography variant="caption" color="text.secondary">
                              Added {new Date(resume.uploadedAt).toLocaleDateString()}
                            </Typography>
                          </Box>
                        </Stack>
                      ))}
                    </Stack>
                  )}
                </CardContent>
              </Card>
            </Grid>
          </Grid>

          <Box sx={{ mt: 3.5 }}>
            <Stack direction="row" alignItems="center" justifyContent="space-between" sx={{ mb: 1.5 }}>
              <Typography variant="h2">Job descriptions</Typography>
              <Button component={Link} to="/jobs" size="small" endIcon={<ArrowForwardRoundedIcon />}>
                All jobs
              </Button>
            </Stack>

            {jobs.length === 0 ? (
              <Alert severity="info">No jobs have been added yet.</Alert>
            ) : (
              <Card sx={{ boxShadow: 'none' }}>
                <Stack divider={<Divider flexItem />}>
                  {jobs.slice(0, 4).map((job) => (
                    <Box
                      key={job.id}
                      sx={{
                        p: 2.25,
                        display: 'flex',
                        alignItems: { xs: 'flex-start', sm: 'center' },
                        flexDirection: { xs: 'column', sm: 'row' },
                        gap: 1.5,
                      }}
                    >
                      <Box sx={{ flex: 1, minWidth: 0 }}>
                        <Typography fontWeight={700}>{job.title}</Typography>
                        <Typography variant="body2" color="text.secondary" sx={{ mt: 0.25 }}>
                          {job.companyName}{job.location ? ` · ${job.location}` : ''}
                        </Typography>
                        <Typography variant="body2" color="text.secondary" sx={{ mt: 0.8, lineHeight: 1.65 }}>
                          {job.description.length > 180 ? `${job.description.slice(0, 180)}…` : job.description}
                        </Typography>
                      </Box>
                      <Button component={Link} to="/compare" size="small" endIcon={<ArrowForwardRoundedIcon />}>
                        Compare
                      </Button>
                    </Box>
                  ))}
                </Stack>
              </Card>
            )}
          </Box>
        </>
      )}
    </Box>
  );
}
