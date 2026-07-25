import axios, { AxiosError, AxiosInstance, AxiosResponse } from 'axios';
import type { ApiError } from '../types';

const BASE_URL = import.meta.env.VITE_API_URL || 'https://localhost:44321/api/v1';

const api: AxiosInstance = axios.create({
  baseURL: BASE_URL,
  headers: { 'Content-Type': 'application/json' },
});

// ── Request interceptor: attach JWT ─────────────────────────────────────────
api.interceptors.request.use((config) => {
  const token =
    localStorage.getItem('staff_token') ||
    localStorage.getItem('customer_token');
  if (token) config.headers.Authorization = `Bearer ${token}`;
  return config;
});

// ── Response interceptor: normalize errors ───────────────────────────────────
api.interceptors.response.use(
  (res: AxiosResponse) => res,
  (error: AxiosError) => {
    const apiError: ApiError = {
      status: error.response?.status ?? 0,
      message:
        (error.response?.data as { message?: string })?.message ||
        error.message ||
        'An unexpected error occurred',
      errors: (error.response?.data as { errors?: Record<string, string[]> })
        ?.errors,
    };
    // Auto-logout on 401
    if (apiError.status === 401) {
      localStorage.removeItem('staff_token');
      localStorage.removeItem('customer_token');
      window.location.href = '/';
    }
    return Promise.reject(apiError);
  }
);

export default api;
