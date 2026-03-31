import axios from 'axios';
import type { ScanResult } from '../types/ScanResult';

const API_BASE = import.meta.env.VITE_API_URL ?? 'http://localhost:5000';

export async function scanFile(file: File): Promise<ScanResult> {
  const formData = new FormData();
  formData.append('file', file);

  const response = await axios.post<ScanResult>(`${API_BASE}/api/scan`, formData, {
    headers: { 'Content-Type': 'multipart/form-data' },
  });

  return response.data;
}
