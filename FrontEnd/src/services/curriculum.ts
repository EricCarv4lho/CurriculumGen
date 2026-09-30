import type { CurriculumData } from '../types';
import { apiPost, apiPostBlob } from './api';

function val(id: string): string {
  const el = document.getElementById(id) as HTMLInputElement | HTMLTextAreaElement | null;
  return el ? el.value.trim() : '';
}

function toISODate(monthStr: string, isEnd = false): string | null {
  if (!monthStr) return null;
  const d = new Date(monthStr + '-01');
  if (isEnd) d.setMonth(d.getMonth() + 1, 0);
  return d.toISOString();
}

export function buildPayload(): CurriculumData {
  return {
    fullName: val('fullName'),
    professionalTitle: val('professionalTitle'),
    city: val('city'),
    state: val('state'),
    phoneNumber: val('phoneNumber'),
    email: val('email'),
    linkedinLink: val('linkedinLink') || null,
    gitHubLink: val('gitHubLink') || null,
    careerObjective: val('careerObjective'),
    highlights: val('highlights'),
    skillSet: (window as any).__skills ?? [],
    template: (document.getElementById('templateSelect') as HTMLSelectElement)?.value || 'classic',
    experiences: ((window as any).__experiences ?? []).map((e: any) => ({
      companyName: e.companyName,
      jobTitle: e.jobTitle,
      startDate: toISODate(e.startDate) || new Date().toISOString(),
      endDate: e.current ? null : (toISODate(e.endDate, true) || null),
      description: e.description,
    })),
    educationList: ((window as any).__educations ?? []).map((e: any) => ({
      course: e.course,
      institutionName: e.institutionName,
      startDate: toISODate(e.startDate) || new Date().toISOString(),
      endDate: e.current ? null : (toISODate(e.endDate, true) || null),
    })),
    languages: ((window as any).__languages ?? []).map((l: any) => ({
      name: l.name,
      proficiency: l.proficiency,
    })),
  };
}

export async function generateCurriculum(payload: CurriculumData): Promise<Blob> {
  return apiPostBlob('/Curriculum/generate', payload);
}

export async function generateCoverLetter(coverData: {
  resumeData: string;
  jobDescription: string;
}): Promise<string> {
  const result = await apiPost<{ text: string }>('/Curriculum/cover-letter-text', coverData);
  return result.text;
}

export function downloadBlob(blob: Blob, filename: string): void {
  const url = URL.createObjectURL(blob);
  const a = document.createElement('a');
  a.href = url;
  a.download = filename;
  document.body.appendChild(a);
  a.click();
  document.body.removeChild(a);
  URL.revokeObjectURL(url);
}
