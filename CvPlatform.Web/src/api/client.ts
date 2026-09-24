import axios from 'axios';
import type { AxiosError } from 'axios';

const baseURL = import.meta.env.VITE_API_URL ?? '';

export const apiBaseUrl = baseURL;

export const tokenStorageKey = 'cvplatform.token';

export const apiClient = axios.create({ baseURL });

apiClient.interceptors.request.use((config) => {
  const token = localStorage.getItem(tokenStorageKey);
  if (token) {
    config.headers.Authorization = `Bearer ${token}`;
  }
  return config;
});

export class ApiError extends Error {
  readonly status: number;

  constructor(status: number, message: string) {
    super(message);
    this.name = 'ApiError';
    this.status = status;
  }
}

type ApiErrorPayload = { message?: string; errors?: string[] };

apiClient.interceptors.response.use(
  (response) => response,
  (error: AxiosError<ApiErrorPayload>) => {
    const status = error.response?.status ?? 0;
    const data = error.response?.data;
    const message = data?.errors?.join(' ') ?? data?.message ?? error.message;
    if (status === 401) {
      localStorage.removeItem(tokenStorageKey);
    }
    return Promise.reject(new ApiError(status, message));
  },
);

export function readRolesFromToken(token: string | null): string[] {
  if (!token) {
    return [];
  }

  const payload = token.split('.')[1];
  if (!payload) {
    return [];
  }

  try {
    const decoded = JSON.parse(window.atob(payload.replace(/-/g, '+').replace(/_/g, '/'))) as Record<string, unknown>;
    const role = decoded.role ?? decoded.roles;
    if (Array.isArray(role)) {
      return role.map(String);
    }
    return role ? [String(role)] : [];
  } catch {
    return [];
  }
}
