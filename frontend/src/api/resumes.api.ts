import api from './client';
import type { ResumeDetails, ResumeItem } from '../types/api';

export const resumesApi = {
  async list(): Promise<ResumeItem[]> {
    const { data } = await api.get<ResumeItem[]>('/api/resumes');
    return data;
  },
  async upload(file: File): Promise<ResumeItem> {
    const form = new FormData();
    form.append('File', file);
    const { data } = await api.post<ResumeItem>('/api/resumes/upload', form);
    return data;
  },
  async getById(id: number): Promise<ResumeDetails> {
    const { data } = await api.get<ResumeDetails>(`/api/resumes/${id}`);
    return data;
  },
  async remove(id: number): Promise<void> {
    await api.delete(`/api/resumes/${id}`);
  },
  async download(id: number, fileName: string): Promise<void> {
    const { data } = await api.get<Blob>(`/api/resumes/${id}/download`, { responseType: 'blob' });
    const url = URL.createObjectURL(data);
    const anchor = document.createElement('a');
    anchor.href = url;
    anchor.download = fileName;
    anchor.click();
    URL.revokeObjectURL(url);
  },
};
