import type { ScanResult } from '../types/ScanResult';

const API_BASE = import.meta.env.VITE_API_URL ?? 'http://localhost:5000';

export async function scanFile(file: File): Promise<ScanResult> {
  const formData = new FormData();
  formData.append('file', file);

  const response = await fetch(`${API_BASE}/api/scan`, {
    method: 'POST',
    body: formData,
  });

  if (!response.ok) {
    const errorBody = await response.json().catch(() => ({}));
    throw new Error(errorBody.error ?? `HTTP ${response.status}`);
  }

  return response.json() as Promise<ScanResult>;
}
