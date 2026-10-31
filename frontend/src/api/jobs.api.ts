import api from './client';
import type { Job, JobRequest } from '../types/api';

export const jobsApi = {
  async list(): Promise<Job[]> {
    const { data } = await api.get<Job[]>('/api/jobs');
    return data;
  },
  async getById(id: number): Promise<Job> {
    const { data } = await api.get<Job>(`/api/jobs/${id}`);
    return data;
  },
  async create(payload: JobRequest): Promise<Job> {
    const { data } = await api.post<Job>('/api/jobs', payload);
    return data;
  },
  async update(id: number, payload: JobRequest): Promise<Job> {
    const { data } = await api.put<Job>(`/api/jobs/${id}`, payload);
    return data;
  },
  async remove(id: number): Promise<void> {
    await api.delete(`/api/jobs/${id}`);
  },
};
