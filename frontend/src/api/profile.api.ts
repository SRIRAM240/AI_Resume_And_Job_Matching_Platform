import api from './client';
import type { UserProfile } from '../types/api';

export const profileApi = {
  async getMe(): Promise<UserProfile> {
    const { data } = await api.get<UserProfile>('/api/profile/me');
    return data;
  },
  async updateMe(fullName: string): Promise<UserProfile> {
    const { data } = await api.put<UserProfile>('/api/profile/me', { fullName });
    return data;
  },
};
