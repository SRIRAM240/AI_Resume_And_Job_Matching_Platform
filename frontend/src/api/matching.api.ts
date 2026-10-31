import api from './client';
import type { MatchRequest, MatchResult } from '../types/api';

export const matchingApi = {
  async matchOne(payload: MatchRequest): Promise<MatchResult> {
    const { data } = await api.post<MatchResult>('/api/matching', payload);
    return data;
  },
  async matchAllJobs(resumeId: number): Promise<MatchResult[]> {
    const { data } = await api.get<MatchResult[]>(`/api/matching/resume/${resumeId}/jobs`);
    return data;
  },
};
