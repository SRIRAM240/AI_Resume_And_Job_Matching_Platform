import axios from 'axios';
import type { ApiError } from '../types/api';

export function getErrorMessage(error: unknown, fallback: string): string {
  if (axios.isAxiosError(error)) {
    const data = error.response?.data;
    if (typeof data === 'string' && data.trim()) return data;
    if (data && typeof data === 'object') {
      const apiError = data as ApiError;
      if (apiError.message) return apiError.message;
      if (apiError.title) return apiError.title;
      if (apiError.errors) {
        const first = Object.values(apiError.errors).flat()[0];
        if (first) return first;
      }
    }
    if (!error.response) {
      return 'Could not connect to the API. Confirm the backend is running and the API URL is correct.';
    }
  }
  return fallback;
}
