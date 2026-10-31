import api from './client';
import type { AiAnalyticsResponse } from '../types/api';

export interface AiAnalysisResponse {
  resumeId: number;
  jobId: number;
  jobTitle: string;
  companyName: string;
  analysis: string;
}

export const aiApi = {
  async analyze(resumeId: number, jobId: number): Promise<AiAnalysisResponse> {
    const { data } = await api.post<AiAnalysisResponse>('/api/ai-analysis', { resumeId, jobId });
    return data;
  },
  async analyzeAnalytics(resumeId: number, jobId: number): Promise<AiAnalyticsResponse> {
    const { data } = await api.post<AiAnalyticsResponse>('/api/ai-analysis/analytics', { resumeId, jobId });
    return data;
  },
};
