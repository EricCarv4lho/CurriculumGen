const API_BASE = '/api';

export class ApiError extends Error {
  constructor(
    message: string,
    public status?: number,
  ) {
    super(message);
    this.name = 'ApiError';
  }
}

function getAuthToken(): string | null {
  return localStorage.getItem('token');
}

function toErrorMessage(res: Response, data: unknown): string {
  const obj = data as { message?: string } | null;
  if (res.status === 401) return obj?.message || 'Você precisa entrar na sua conta para usar esta função.';
  if (obj?.message) return obj.message;
  return `Erro ${res.status}`;
}

export function getHeaders(): HeadersInit {
  const headers: HeadersInit = { 'Content-Type': 'application/json' };
  const token = getAuthToken();
  if (token) {
    (headers as Record<string, string>)['Authorization'] = `Bearer ${token}`;
  }
  return headers;
}

export async function apiGet<T>(path: string): Promise<T> {
  const res = await fetch(`${API_BASE}${path}`, { headers: getHeaders() });
  if (!res.ok) {
    const data = await res.json().catch(() => null);
    throw new ApiError(toErrorMessage(res, data), res.status);
  }
  return res.json();
}

export async function apiPost<T>(path: string, body?: unknown): Promise<T> {
  const res = await fetch(`${API_BASE}${path}`, {
    method: 'POST',
    headers: getHeaders(),
    body: body !== undefined ? JSON.stringify(body) : undefined,
  });
  if (!res.ok) {
    const data = await res.json().catch(() => null);
    throw new ApiError(toErrorMessage(res, data), res.status);
  }
  return res.json();
}

export async function apiPostBlob(path: string, body?: unknown): Promise<Blob> {
  const res = await fetch(`${API_BASE}${path}`, {
    method: 'POST',
    headers: getHeaders(),
    body: body !== undefined ? JSON.stringify(body) : undefined,
  });
  if (!res.ok) {
    const data = await res.json().catch(() => null);
    throw new ApiError(
      Array.isArray(data) ? data.map((e: { message: string }) => e.message).join(' | ')
      : toErrorMessage(res, data),
      res.status,
    );
  }
  return res.blob();
}

export async function apiDelete(path: string): Promise<void> {
  const res = await fetch(`${API_BASE}${path}`, {
    method: 'DELETE',
    headers: getHeaders(),
  });
  if (!res.ok) {
    const data = await res.json().catch(() => null);
    throw new ApiError(toErrorMessage(res, data), res.status);
  }
}

export async function apiPut<T>(path: string, body?: unknown): Promise<T> {
  const res = await fetch(`${API_BASE}${path}`, {
    method: 'PUT',
    headers: getHeaders(),
    body: body !== undefined ? JSON.stringify(body) : undefined,
  });
  if (!res.ok) {
    const data = await res.json().catch(() => null);
    throw new ApiError(toErrorMessage(res, data), res.status);
  }
  return res.json();
}

export async function apiPostFormData(path: string, formData: FormData): Promise<Blob> {
  const headers: HeadersInit = {};
  const token = getAuthToken();
  if (token) {
    (headers as Record<string, string>)['Authorization'] = `Bearer ${token}`;
  }
  const res = await fetch(`${API_BASE}${path}`, {
    method: 'POST',
    headers,
    body: formData,
  });
  if (!res.ok) {
    const data = await res.json().catch(() => null);
    throw new ApiError(toErrorMessage(res, data), res.status);
  }
  return res.blob();
}

export function getApiBase(): string {
  return API_BASE;
}
