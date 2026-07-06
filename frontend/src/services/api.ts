import type { ScanResult } from '../types/ScanResult';

// Token storage
const TOKEN_KEY = 'fs_token';

export function getToken(): string | null {
  return localStorage.getItem(TOKEN_KEY);
}

export function setToken(token: string): void {
  localStorage.setItem(TOKEN_KEY, token);
}

export function removeToken(): void {
  localStorage.removeItem(TOKEN_KEY);
}

function getAuthHeaders(): HeadersInit {
  const token = getToken();
  if (!token) return {};
  return { Authorization: `Bearer ${token}` };
}

// Auth API
interface LoginRequest {
  email: string;
  password: string;
}

interface LoginResponse {
  token: string;
  user: {
    id: number;
    email: string;
    hasApiKey: boolean;
  };
}

export async function login(data: LoginRequest): Promise<LoginResponse> {
  const response = await fetch('/api/auth/login', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(data),
  });

  if (!response.ok) {
    const errorBody = await response.json().catch(() => ({}));
    throw new Error(errorBody.error ?? `HTTP ${response.status}`);
  }

  const result = await response.json() as LoginResponse;
  setToken(result.token);
  return result;
}

export async function register(data: LoginRequest): Promise<LoginResponse> {
  const response = await fetch('/api/auth/register', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(data),
  });

  if (!response.ok) {
    const errorBody = await response.json().catch(() => ({}));
    throw new Error(errorBody.error ?? `HTTP ${response.status}`);
  }

  const result = await response.json() as LoginResponse;
  setToken(result.token);
  return result;
}

export function logout(): void {
  removeToken();
}

export async function getMe(): Promise<LoginResponse['user']> {
  const response = await fetch('/api/auth/me', {
    headers: { ...getAuthHeaders() },
  });

  if (!response.ok) {
    throw new Error('Not authenticated');
  }

  return response.json() as Promise<LoginResponse['user']>;
}

export async function saveApiKey(apiKey: string): Promise<void> {
  const response = await fetch('/api/auth/apikey', {
    method: 'POST',
    headers: { 
      'Content-Type': 'application/json',
      ...getAuthHeaders()
    },
    body: JSON.stringify({ apiKey }),
  });

  if (!response.ok) {
    const errorBody = await response.json().catch(() => ({}));
    throw new Error(errorBody.error ?? `HTTP ${response.status}`);
  }
}

export interface UserPreferences {
  provider?: string;
  visionModel?: string;
  textModel?: string;
}

export async function savePreferences(prefs: UserPreferences): Promise<void> {
  const response = await fetch('/api/auth/preferences', {
    method: 'POST',
    headers: { 
      'Content-Type': 'application/json',
      ...getAuthHeaders()
    },
    body: JSON.stringify({
      provider: prefs.provider,
      visionModel: prefs.visionModel,
      textModel: prefs.textModel,
    }),
  });

  if (!response.ok) {
    const errorBody = await response.json().catch(() => ({}));
    throw new Error(errorBody.error ?? `HTTP ${response.status}`);
  }
}

// Scanner API
export async function scanFile(file: File): Promise<ScanResult> {
  const formData = new FormData();
  formData.append('file', file);

  const response = await fetch('/api/scan', {
    method: 'POST',
    headers: getAuthHeaders(),
    body: formData,
  });

  if (!response.ok) {
    const errorBody = await response.json().catch(() => ({}));
    throw new Error(errorBody.error ?? `HTTP ${response.status}`);
  }

  return response.json() as Promise<ScanResult>;
}