import { useEffect, useState } from 'react';
import {
  Alert, Box, Button, Card, CardContent, Checkbox, Chip, CircularProgress,
  FormControl, FormControlLabel, Grid, InputLabel, LinearProgress, ListItemText,
  MenuItem, Select, Stack, Typography
} from '@mui/material';
import type { SelectChangeEvent } from '@mui/material';
import CompareArrowsRoundedIcon from '@mui/icons-material/CompareArrowsRounded';
import CheckCircleRoundedIcon from '@mui/icons-material/CheckCircleRounded';
import CancelRoundedIcon from '@mui/icons-material/CancelRounded';
import AutoAwesomeOutlinedIcon from '@mui/icons-material/AutoAwesomeOutlined';
import { Bar, Radar } from 'react-chartjs-2';
import {
  CategoryScale, Chart as ChartJS, LinearScale, BarElement,
  RadialLinearScale, PointElement, LineElement, Filler, Tooltip, Legend
} from 'chart.js';
import { resumesApi } from '../api/resumes.api';
import { jobsApi } from '../api/jobs.api';
import { matchingApi } from '../api/matching.api';
import { aiApi } from '../api/ai.api';
import type { Job, MatchResult, ResumeItem, AiAnalyticsResponse } from '../types/api';
import { getErrorMessage } from '../utils/errors';

ChartJS.register(
  CategoryScale, LinearScale, BarElement,
  RadialLinearScale, PointElement, LineElement, Filler,
  Tooltip, Legend
);

export default function ComparePage() {
  const [resumes, setResumes] = useState<ResumeItem[]>([]);
  const [resumeId, setResumeId] = useState('');
  const [jobs, setJobs] = useState<Job[]>([]);
  const [selectedJobIds, setSelectedJobIds] = useState<number[]>([]);
  const [results, setResults] = useState<MatchResult[]>([]);
  const [loadingResumes, setLoadingResumes] = useState(true);
  const [loadingJobs, setLoadingJobs] = useState(true);
  const [analyzing, setAnalyzing] = useState(false);
  const [error, setError] = useState('');
  const [hasRun, setHasRun] = useState(false);
  const [aiLoadingJobId, setAiLoadingJobId] = useState<number | null>(null);
  const [aiAnalytics, setAiAnalytics] = useState<Record<number, AiAnalyticsResponse>>({});
  const [aiErrors, setAiErrors] = useState<Record<number, string>>({});

  useEffect(() => {
    let active = true;

    resumesApi.list()
      .then((items) => {
        if (!active) return;
        setResumes(items);
        if (items.length) setResumeId(String(items[0].id));
      })
      .catch((e) => {
        if (active) setError(getErrorMessage(e, 'Could not load resumes.'));
      })
      .finally(() => {
        if (active) setLoadingResumes(false);
      });

    jobsApi.list()
      .then((items) => {
        if (!active) return;
        setJobs(items);
        setSelectedJobIds(items.map((j) => j.id));
      })
      .catch((e) => {
        if (active) setError(getErrorMessage(e, 'Could not load jobs.'));
      })
      .finally(() => {
        if (active) setLoadingJobs(false);
      });

    return () => { active = false; };
  }, []);

  const allSelected = jobs.length > 0 && selectedJobIds.length === jobs.length;

  function handleJobChange(e: SelectChangeEvent<number[]>) {
    const val = e.target.value;
    setSelectedJobIds(typeof val === 'string' ? val.split(',').map(Number) : (val as number[]));
  }

  async function runComparison() {
    if (!resumeId) {
      setError('Please select a resume first.');
      return;
    }
    if (selectedJobIds.length === 0) {
      setError('Please select at least one job.');
      return;
    }

    setAnalyzing(true);
    setError('');
    setHasRun(false);

    try {
      let matches: MatchResult[];
      if (selectedJobIds.length === 1) {
        matches = [await matchingApi.matchOne({ resumeId: Number(resumeId), jobId: selectedJobIds[0] })];
      } else {
        const all = await matchingApi.matchAllJobs(Number(resumeId));
        matches = all.filter((m) => selectedJobIds.includes(m.jobId));
        matches.sort((a, b) => b.matchScore - a.matchScore || a.jobTitle.localeCompare(b.jobTitle));
      }
      setResults(matches);
      setHasRun(true);
    } catch (e) {
      setError(getErrorMessage(e, 'Matching failed. Check that your resume has extracted text and that jobs exist.'));
    } finally {
      setAnalyzing(false);
    }
  }

  async function runAiAnalytics(jobId: number) {
    if (!resumeId) return;
    setAiLoadingJobId(jobId);
    setAiErrors((prev) => ({ ...prev, [jobId]: '' }));

    try {
      const data = await aiApi.analyzeAnalytics(Number(resumeId), jobId);
      setAiAnalytics((prev) => ({ ...prev, [jobId]: data }));
    } catch (e) {
      setAiErrors((prev) => ({
        ...prev,
        [jobId]: getErrorMessage(e, 'AI analytics failed. Check your Gemini configuration and try again.')
      }));
    } finally {
      setAiLoadingJobId(null);
    }
  }

  const chartData = {
    labels: results.slice(0, 8).map((r) => r.jobTitle),
    datasets: [{
      label: 'Match %',
      data: results.slice(0, 8).map((r) => r.matchScore),
      backgroundColor: '#7167eb',
      borderRadius: 6,
      maxBarThickness: 24
    }],
  };

  const loading = loadingResumes || loadingJobs;

  return (
    <Box className="page-shell">
      <Box className="page-header">
        <Box>
          <Typography variant="h1">Compare resume with jobs</Typography>
        </Box>
      </Box>

      {error && <Alert severity="error" sx={{ mb: 2 }} onClose={() => setError('')}>{error}</Alert>}

      {/* Setup */}
      <Card sx={{ mb: 3 }}>
        <CardContent sx={{ p: { xs: 2, md: 3 } }}>
          <Grid container spacing={2} alignItems="flex-end">
            <Grid size={{ xs: 12, sm: 4 }}>
              <FormControl fullWidth disabled={loadingResumes || resumes.length === 0}>
                <InputLabel id="resume-lbl">Resume</InputLabel>
                <Select
                  labelId="resume-lbl"
                  label="Resume"
                  value={resumeId}
                  onChange={(e: SelectChangeEvent) => setResumeId(e.target.value)}
                >
                  {resumes.map((r) => (
                    <MenuItem key={r.id} value={String(r.id)}>{r.fileName}</MenuItem>
                  ))}
                </Select>
              </FormControl>
            </Grid>

            <Grid size={{ xs: 12, sm: 5 }}>
              <Stack direction="row" alignItems="center" justifyContent="space-between" sx={{ mb: 0.5 }}>
                <Typography variant="body2" color="text.secondary">
                  {selectedJobIds.length} of {jobs.length} jobs selected
                </Typography>
                <FormControlLabel
                  sx={{ mr: 0 }}
                  control={
                    <Checkbox
                      size="small"
                      checked={allSelected}
                      indeterminate={selectedJobIds.length > 0 && !allSelected}
                      onChange={() => setSelectedJobIds(allSelected ? [] : jobs.map((j) => j.id))}
                      disabled={loadingJobs || jobs.length === 0}
                    />
                  }
                  label={<Typography variant="body2">All</Typography>}
                />
              </Stack>
              <FormControl fullWidth disabled={loadingJobs || jobs.length === 0}>
                <InputLabel id="jobs-lbl">Jobs</InputLabel>
                <Select
                  labelId="jobs-lbl"
                  label="Jobs"
                  multiple
                  value={selectedJobIds}
                  onChange={handleJobChange}
                  renderValue={(sel) => `${(sel as number[]).length} job${(sel as number[]).length === 1 ? '' : 's'}`}
                >
                  {jobs.map((j) => (
                    <MenuItem key={j.id} value={j.id}>
                      <Checkbox checked={selectedJobIds.includes(j.id)} />
                      <ListItemText primary={j.title} secondary={j.companyName} />
                    </MenuItem>
                  ))}
                </Select>
              </FormControl>
            </Grid>

            <Grid size={{ xs: 12, sm: 3 }}>
              <Button
                fullWidth
                size="large"
                variant="contained"
                startIcon={analyzing ? <CircularProgress size={18} color="inherit" /> : <CompareArrowsRoundedIcon />}
                disabled={analyzing || loading || !resumeId || selectedJobIds.length === 0}
                onClick={() => void runComparison()}
              >
                {analyzing ? 'Comparing…' : 'Compare'}
              </Button>
            </Grid>
          </Grid>
        </CardContent>
      </Card>

      {/* Loading & Empty states */}
      {loading && (
        <Box sx={{ textAlign: 'center', py: 6 }}>
          <CircularProgress />
        </Box>
      )}

      {!loading && resumes.length === 0 && (
        <Alert severity="info">Upload a resume first to compare with jobs.</Alert>
      )}

      {hasRun && results.length === 0 && (
        <Alert severity="info">No matches returned for the selected jobs.</Alert>
      )}

      {/* Results */}
      {hasRun && results.length > 0 && (
        <>
          <Grid container spacing={2.5} sx={{ mb: 3 }}>
            <Grid size={{ xs: 12, lg: 7 }}>
              <Card>
                <CardContent sx={{ p: 3 }}>
                  <Typography variant="h3" sx={{ mb: 2 }}>Top matches</Typography>
                  <Box sx={{ height: Math.max(220, Math.min(380, results.slice(0, 8).length * 44)) }}>
                    <Bar
                      data={chartData}
                      options={{
                        responsive: true,
                        maintainAspectRatio: false,
                        indexAxis: 'y',
                        plugins: { legend: { display: false }, tooltip: { callbacks: { label: (ctx) => ` ${ctx.parsed.x}%` } } },
                        scales: {
                          x: { min: 0, max: 100, ticks: { callback: (v) => `${v}%` }, grid: { color: '#eff0f6' } },
                          y: { grid: { display: false } },
                        },
                      }}
                    />
                  </Box>
                </CardContent>
              </Card>
            </Grid>

            <Grid size={{ xs: 12, lg: 5 }}>
              <Card sx={{ height: '100%' }}>
                <CardContent sx={{ p: 3 }}>
                  <Typography variant="h3" sx={{ mb: 2 }}>Summary</Typography>
                  <Typography variant="body2" color="text.secondary">Jobs compared</Typography>
                  <Typography variant="h2" sx={{ mb: 2 }}>{results.length}</Typography>

                  <Typography variant="body2" color="text.secondary">Average match</Typography>
                  <Typography variant="h2" sx={{ mb: 2 }}>
                    {Math.round(results.reduce((sum, r) => sum + r.matchScore, 0) / results.length)}%
                  </Typography>

                  <Typography variant="body2" color="text.secondary">Best match</Typography>
                  <Typography variant="h3">
                    {results[0].jobTitle} — {Math.round(results[0].matchScore)}%
                  </Typography>
                </CardContent>
              </Card>
            </Grid>
          </Grid>

          <Typography variant="h2" sx={{ mb: 2 }}>Results</Typography>

          <Stack spacing={2}>
            {results.map((r, index) => {
              const analytics = aiAnalytics[r.jobId];
              return (
                <Card key={`${r.jobId}-${r.resumeId}`}>
                  <CardContent sx={{ p: { xs: 2, md: 3 } }}>
                    <Stack direction={{ xs: 'column', sm: 'row' }} spacing={2} alignItems={{ sm: 'center' }}>
                      <Typography variant="body2" fontWeight={800} color="text.secondary" sx={{ minWidth: 28 }}>
                        #{index + 1}
                      </Typography>
                      <Box sx={{ flex: 1 }}>
                        <Typography variant="h3">{r.jobTitle}</Typography>
                        <Typography variant="body2" color="text.secondary">{r.companyName}</Typography>
                      </Box>
                      <Typography
                        variant="h2"
                        color={r.matchScore >= 75 ? 'success.main' : r.matchScore >= 50 ? 'primary.main' : 'text.primary'}
                      >
                        {Math.round(r.matchScore)}%
                      </Typography>
                    </Stack>

                    <LinearProgress
                      variant="determinate"
                      value={Math.min(100, r.matchScore)}
                      sx={{ mt: 1.5, height: 6, borderRadius: 4, bgcolor: '#eeeef6', '& .MuiLinearProgress-bar': { borderRadius: 4 } }}
                    />

                    {r.summary && (
                      <Typography variant="body2" color="text.secondary" sx={{ mt: 1.5 }}>
                        {r.summary}
                      </Typography>
                    )}

                    <Stack direction="row" spacing={3} sx={{ mt: 2 }}>
                      <Box>
                        <Typography variant="caption" color="text.secondary">Skill match</Typography>
                        <Typography fontWeight={700}>{r.skillMatchScore.toFixed(1)}%</Typography>
                      </Box>
                      <Box>
                        <Typography variant="caption" color="text.secondary">Description relevance</Typography>
                        <Typography fontWeight={700}>{r.descriptionRelevanceScore.toFixed(1)}%</Typography>
                      </Box>
                    </Stack>

                    <Grid container spacing={2} sx={{ mt: 1 }}>
                      <Grid size={{ xs: 12, md: 6 }}>
                        <Typography variant="body2" fontWeight={700} sx={{ mb: 0.75, display: 'flex', alignItems: 'center', gap: 0.5 }}>
                          <CheckCircleRoundedIcon color="success" sx={{ fontSize: 18 }} />
                          Matched ({r.matchingSkills.length})
                        </Typography>
                        <Stack direction="row" spacing={0.5} useFlexGap flexWrap="wrap">
                          {r.matchingSkills.length > 0 ? (
                            r.matchingSkills.map((s) => (
                              <Chip key={s} size="small" color="success" variant="outlined" label={s} />
                            ))
                          ) : (
                            <Typography variant="body2" color="text.secondary">None</Typography>
                          )}
                        </Stack>
                      </Grid>

                      <Grid size={{ xs: 12, md: 6 }}>
                        <Typography variant="body2" fontWeight={700} sx={{ mb: 0.75, display: 'flex', alignItems: 'center', gap: 0.5 }}>
                          <CancelRoundedIcon color="error" sx={{ fontSize: 18 }} />
                          Missing ({r.missingSkills.length})
                        </Typography>
                        <Stack direction="row" spacing={0.5} useFlexGap flexWrap="wrap">
                          {r.missingSkills.length > 0 ? (
                            r.missingSkills.map((s) => (
                              <Chip key={s} size="small" color="error" variant="outlined" label={s} />
                            ))
                          ) : (
                            <Typography variant="body2" color="text.secondary">None</Typography>
                          )}
                        </Stack>
                      </Grid>
                    </Grid>

                    {/* AI Analytics Button */}
                    <Box sx={{ mt: 2 }}>
                      <Button
                        size="small"
                        variant="outlined"
                        startIcon={aiLoadingJobId === r.jobId ? <CircularProgress size={14} /> : <AutoAwesomeOutlinedIcon />}
                        disabled={aiLoadingJobId !== null}
                        onClick={() => void runAiAnalytics(r.jobId)}
                      >
                        {aiLoadingJobId === r.jobId ? 'Analyzing insights…' : analytics ? 'Refresh AI analytics' : 'AI analytics'}
                      </Button>
                    </Box>

                    {aiErrors[r.jobId] && (
                      <Alert severity="error" sx={{ mt: 1.5 }} onClose={() => setAiErrors((prev) => ({ ...prev, [r.jobId]: '' }))}>
                        {aiErrors[r.jobId]}
                      </Alert>
                    )}

                    {/* Structured AI Analytics Section */}
                    {analytics && (
                      <Box sx={{ mt: 2.5, p: 2.5, bgcolor: '#fbfbff', border: '1px solid #e7e8f2', borderRadius: 2 }}>
                        {/* Top: Header & Verdict */}
                        <Stack
                          direction={{ xs: 'column', sm: 'row' }}
                          spacing={1.5}
                          alignItems={{ sm: 'center' }}
                          justifyContent="space-between"
                          sx={{ mb: 1.5 }}
                        >
                          <Stack direction="row" spacing={1} alignItems="center">
                            <AutoAwesomeOutlinedIcon sx={{ color: 'primary.main', fontSize: 20 }} />
                            <Typography variant="h3" sx={{ fontSize: '1.05rem' }}>AI Match Analytics</Typography>
                          </Stack>
                          <Chip
                            label={analytics.verdict}
                            color={
                              analytics.verdict.toLowerCase().includes('strong') ? 'success' :
                              analytics.verdict.toLowerCase().includes('good') ? 'primary' :
                              analytics.verdict.toLowerCase().includes('moderate') ? 'warning' : 'default'
                            }
                            sx={{ fontWeight: 700 }}
                          />
                        </Stack>

                        <Typography variant="body2" color="text.secondary" sx={{ mb: 2.5, lineHeight: 1.6 }}>
                          {analytics.summary}
                        </Typography>

                        {/* Chart & Category Scores */}
                        <Grid container spacing={2.5} sx={{ mb: 2.5 }} alignItems="center">
                          <Grid size={{ xs: 12, md: 5 }}>
                            <Box sx={{ height: 210, position: 'relative' }}>
                              <Radar
                                data={{
                                  labels: ['Technical', 'Experience', 'Education', 'Soft Skills', 'Overall Fit'],
                                  datasets: [{
                                    label: 'Competency Score',
                                    data: [
                                      analytics.categoryScores.technicalSkills,
                                      analytics.categoryScores.experienceRelevance,
                                      analytics.categoryScores.educationAndCertifications,
                                      analytics.categoryScores.softSkillsAndLeadership,
                                      analytics.categoryScores.overallFit,
                                    ],
                                    backgroundColor: 'rgba(98, 88, 232, 0.18)',
                                    borderColor: '#6258e8',
                                    pointBackgroundColor: '#6258e8',
                                    pointBorderColor: '#fff',
                                    borderWidth: 2,
                                  }]
                                }}
                                options={{
                                  responsive: true,
                                  maintainAspectRatio: false,
                                  scales: {
                                    r: {
                                      min: 0,
                                      max: 100,
                                      ticks: { stepSize: 25, display: false },
                                      grid: { color: '#e6e8f2' },
                                      pointLabels: { font: { size: 11, weight: 600 } },
                                    }
                                  },
                                  plugins: {
                                    legend: { display: false },
                                    tooltip: { callbacks: { label: (ctx) => ` ${ctx.raw}%` } }
                                  }
                                }}
                              />
                            </Box>
                          </Grid>

                          <Grid size={{ xs: 12, md: 7 }}>
                            <Stack spacing={1.2}>
                              {[
                                { label: 'Technical Competency', score: analytics.categoryScores.technicalSkills },
                                { label: 'Experience Relevance', score: analytics.categoryScores.experienceRelevance },
                                { label: 'Education & Certifications', score: analytics.categoryScores.educationAndCertifications },
                                { label: 'Soft Skills & Leadership', score: analytics.categoryScores.softSkillsAndLeadership },
                                { label: 'Overall Suitability', score: analytics.categoryScores.overallFit },
                              ].map((cat) => (
                                <Box key={cat.label}>
                                  <Stack direction="row" justifyContent="space-between" sx={{ mb: 0.25 }}>
                                    <Typography variant="caption" fontWeight={650}>{cat.label}</Typography>
                                    <Typography variant="caption" fontWeight={700} color="primary.main">{cat.score}%</Typography>
                                  </Stack>
                                  <LinearProgress
                                    variant="determinate"
                                    value={cat.score}
                                    sx={{ height: 6, borderRadius: 3, bgcolor: '#ebeef7' }}
                                  />
                                </Box>
                              ))}
                            </Stack>
                          </Grid>
                        </Grid>

                        {/* Insights: Strengths, Gaps, Interview Focus */}
                        <Grid container spacing={2}>
                          {analytics.strengths.length > 0 && (
                            <Grid size={{ xs: 12, md: 4 }}>
                              <Box sx={{ p: 1.75, bgcolor: '#ffffff', border: '1px solid #e7eaf3', borderRadius: 1.5, height: '100%' }}>
                                <Typography variant="body2" fontWeight={700} color="success.main" sx={{ mb: 1, display: 'flex', alignItems: 'center', gap: 0.5 }}>
                                  <CheckCircleRoundedIcon sx={{ fontSize: 16 }} /> Key Strengths
                                </Typography>
                                <Stack spacing={0.75}>
                                  {analytics.strengths.map((s, idx) => (
                                    <Typography key={idx} variant="caption" color="text.secondary" sx={{ display: 'block', lineHeight: 1.5 }}>
                                      • {s}
                                    </Typography>
                                  ))}
                                </Stack>
                              </Box>
                            </Grid>
                          )}

                          {analytics.improvements.length > 0 && (
                            <Grid size={{ xs: 12, md: 4 }}>
                              <Box sx={{ p: 1.75, bgcolor: '#ffffff', border: '1px solid #e7eaf3', borderRadius: 1.5, height: '100%' }}>
                                <Typography variant="body2" fontWeight={700} color="error.main" sx={{ mb: 1, display: 'flex', alignItems: 'center', gap: 0.5 }}>
                                  <CancelRoundedIcon sx={{ fontSize: 16 }} /> Areas to Tailor
                                </Typography>
                                <Stack spacing={0.75}>
                                  {analytics.improvements.map((item, idx) => (
                                    <Typography key={idx} variant="caption" color="text.secondary" sx={{ display: 'block', lineHeight: 1.5 }}>
                                      • {item}
                                    </Typography>
                                  ))}
                                </Stack>
                              </Box>
                            </Grid>
                          )}

                          {analytics.interviewFocusAreas.length > 0 && (
                            <Grid size={{ xs: 12, md: 4 }}>
                              <Box sx={{ p: 1.75, bgcolor: '#ffffff', border: '1px solid #e7eaf3', borderRadius: 1.5, height: '100%' }}>
                                <Typography variant="body2" fontWeight={700} color="primary.main" sx={{ mb: 1, display: 'flex', alignItems: 'center', gap: 0.5 }}>
                                  <AutoAwesomeOutlinedIcon sx={{ fontSize: 16 }} /> Interview Focus
                                </Typography>
                                <Stack spacing={0.75}>
                                  {analytics.interviewFocusAreas.map((topic, idx) => (
                                    <Typography key={idx} variant="caption" color="text.secondary" sx={{ display: 'block', lineHeight: 1.5 }}>
                                      • {topic}
                                    </Typography>
                                  ))}
                                </Stack>
                              </Box>
                            </Grid>
                          )}
                        </Grid>
                      </Box>
                    )}
                  </CardContent>
                </Card>
              );
            })}
          </Stack>
        </>
      )}
    </Box>
  );
}
