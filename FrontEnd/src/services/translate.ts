import { apiPostFormData } from './api';

export async function translateCurriculum(
  file: File,
  targetLanguage: string,
): Promise<Blob> {
  const formData = new FormData();
  formData.append('file', file);
  formData.append('targetLanguage', targetLanguage);
  return apiPostFormData('/Curriculum/translate', formData);
}
