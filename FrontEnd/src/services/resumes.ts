import { apiGet, apiPost, apiDelete, getApiBase } from './api';
import type { CurriculumData } from '../types/curriculum';

const BASE = '/resumes';

export type ResumeListItem = {
  id: number;
  name: string;
  templateId: number | null;
  hasPdf: boolean;
  createdAt: string;
  updatedAt: string;
};

export type ResumeFull = ResumeListItem & {
  data: CurriculumData;
};

export async function listResumes(): Promise<ResumeListItem[]> {
  return apiGet<ResumeListItem[]>(BASE);
}

export async function getResume(id: number): Promise<ResumeFull> {
  return apiGet<ResumeFull>(`${BASE}/${id}`);
}

export async function createResume(name: string, templateId: number | null, data: CurriculumData): Promise<ResumeListItem> {
  return apiPost<ResumeListItem>(BASE, { name, templateId, data });
}

export async function uploadResumePdf(id: number, pdfBlob: Blob): Promise<void> {
  const formData = new FormData();
  formData.append('file', pdfBlob, 'curriculo.pdf');
  const headers: Record<string, string> = {};
  const token = localStorage.getItem('token');
  if (token) {
    headers['Authorization'] = `Bearer ${token}`;
  }
  const res = await fetch(`${getApiBase()}${BASE}/${id}/pdf`, {
    method: 'POST',
    headers,
    body: formData,
  });
  if (!res.ok) {
    const data = await res.json().catch(() => null);
    throw new Error(data?.message || 'Erro ao enviar PDF.');
  }
}

export async function downloadResumePdfBlob(id: number): Promise<Blob> {
  const headers: Record<string, string> = {};
  const token = localStorage.getItem('token');
  if (token) {
    headers['Authorization'] = `Bearer ${token}`;
  }
  const res = await fetch(`${getApiBase()}${BASE}/${id}/pdf`, { headers });
  if (!res.ok) throw new Error('Erro ao baixar PDF salvo.');
  return res.blob();
}

export async function deleteResume(id: number): Promise<void> {
  await apiDelete(`${BASE}/${id}`);
}

const TEMPLATE_IDS: Record<string, number> = {
  classic: 1,
  modern: 2,
  executive: 3,
  creative: 4,
};

const TEMPLATE_NAMES: Record<number, string> = {
  1: 'Clássico',
  2: 'Moderno',
  3: 'Executivo',
  4: 'Criativo',
};

export function templateNameToId(name: string): number {
  return TEMPLATE_IDS[name] || 1;
}

export function templateIdToName(id: number | null): string {
  return id ? TEMPLATE_NAMES[id] || 'Clássico' : 'Clássico';
}

export function formatDate(iso: string): string {
  const d = new Date(iso);
  return d.toLocaleDateString('pt-BR', { day: '2-digit', month: '2-digit', year: 'numeric' });
}
