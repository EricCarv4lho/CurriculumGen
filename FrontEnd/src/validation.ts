import { showToast } from './utils/ui';

function val(id: string): string {
  const el = document.getElementById(id) as HTMLInputElement | HTMLTextAreaElement | null;
  return el ? el.value.trim() : '';
}

function setError(fieldId: string, errId: string, msg: string): boolean {
  const field = document.getElementById(fieldId) as HTMLElement | null;
  const errEl = document.getElementById(errId) as HTMLElement | null;
  if (!field) return false;
  if (msg) {
    field.classList.add('invalid');
    if (errEl) errEl.textContent = msg;
  } else {
    field.classList.remove('invalid');
    if (errEl) errEl.textContent = '';
  }
  return !!msg;
}

function clearErrors(): void {
  document.querySelectorAll('.invalid').forEach((el) => el.classList.remove('invalid'));
  document.querySelectorAll('.field-error').forEach((el) => (el.textContent = ''));
}

function validateDate(startDate: string, endDate: string, isCurrent: boolean): boolean {
  if (!startDate || !endDate || isCurrent) return true;
  return endDate >= startDate;
}

const errors: string[] = [];

function addErr(msg: string): void {
  errors.push(msg);
}

export function validateStep(step: number): boolean {
  clearErrors();
  errors.length = 0;
  let valid = true;

  if (step === 1) {
    if (!val('fullName')) {
      setError('fullName', 'errFullName', 'Nome obrigatório');
      valid = false;
    }
    if (!val('professionalTitle')) {
      setError('professionalTitle', 'errProfessionalTitle', 'Título obrigatório');
      valid = false;
    }
    if (!val('city')) {
      setError('city', 'errCity', 'Cidade obrigatória');
      valid = false;
    }
    if (!val('state')) {
      setError('state', 'errState', 'Estado obrigatório');
      valid = false;
    }
    if (!val('phoneNumber')) {
      setError('phoneNumber', 'errPhoneNumber', 'Telefone obrigatório');
      valid = false;
    }
    const email = val('email');
    if (!email) {
      setError('email', 'errEmail', 'E-mail obrigatório');
      valid = false;
    } else if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email)) {
      setError('email', 'errEmail', 'E-mail inválido');
      valid = false;
    }
  }

  if (step === 2) {
    if (!val('careerObjective')) {
      setError('careerObjective', 'errCareerObjective', 'Objetivo obrigatório');
      valid = false;
    }
    if (!val('highlights')) {
      setError('highlights', 'errHighlights', 'Destaques obrigatório');
      valid = false;
    }
  }

  if (step === 3) {
    // Experiências são opcionais: valida apenas os itens que existirem.
    const exps: any[] = (window as any).__experiences ?? [];
    exps.forEach((exp: any, i: number) => {
      const el = document.getElementById(`exp-${exp.id}`);
      if (!el) return;
      if (!exp.companyName || !exp.jobTitle) {
        el.classList.add('invalid');
        addErr(`Experiência #${i + 1}: preencha empresa e cargo.`);
        valid = false;
      }
      if (!validateDate(exp.startDate, exp.endDate, exp.current)) {
        el.classList.add('invalid');
        addErr(`Experiência #${i + 1}: data de fim deve ser posterior à data de início.`);
        valid = false;
      }
    });
  }

  if (step === 4) {
    const edus: any[] = (window as any).__educations ?? [];
    edus.forEach((edu: any, i: number) => {
      const el = document.getElementById(`edu-${edu.id}`);
      if (!el) return;
      if (!edu.course || !edu.institutionName) {
        el.classList.add('invalid');
        addErr(`Formação #${i + 1}: preencha curso e instituição.`);
        valid = false;
      }
      if (!validateDate(edu.startDate, edu.endDate, edu.current)) {
        el.classList.add('invalid');
        addErr(`Formação #${i + 1}: data de fim deve ser posterior à data de início.`);
        valid = false;
      }
    });
  }

  if (!valid) {
    const msgs = [...new Set(errors)];
    showToast(msgs.join(' | '), 'error');
  }
  return valid;
}
