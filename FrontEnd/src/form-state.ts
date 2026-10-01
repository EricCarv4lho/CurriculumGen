interface EducationItem {
  id: number;
  course: string;
  institutionName: string;
  startDate: string;
  endDate: string;
  current: boolean;
}

interface ExperienceItem {
  id: number;
  companyName: string;
  jobTitle: string;
  startDate: string;
  endDate: string;
  description: string;
  current: boolean;
}

const experiences: ExperienceItem[] = [];
(window as any).__experiences = experiences;

export function addExperience(): void {
  const id = Date.now();
  const item: ExperienceItem = { id, companyName: '', jobTitle: '', startDate: '', endDate: '', description: '', current: false };
  experiences.push(item);
  renderExperienceItem(item);
}

function renderExperienceItem(item: ExperienceItem): void {
  const div = document.createElement('div');
  div.className = 'dynamic-item';
  div.id = `exp-${item.id}`;
  div.innerHTML = `
    <div class="dynamic-item-header">
      <span class="dynamic-item-title"><svg aria-hidden="true" width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><rect x="2" y="7" width="20" height="14" rx="2" ry="2"/><path d="M16 21V5a2 2 0 00-2-2h-4a2 2 0 00-2 2v16"/></svg> Experiência #${experiences.indexOf(item) + 1}</span>
      <button type="button" class="remove-btn" onclick="removeExperience(${item.id})"><svg aria-hidden="true" width="12" height="12" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5" stroke-linecap="round" stroke-linejoin="round"><line x1="18" y1="6" x2="6" y2="18"/><line x1="6" y1="6" x2="18" y2="18"/></svg> Remover</button>
    </div>
    <div class="form-grid">
      <div class="field">
        <label for="expCompany-${item.id}">Empresa</label>
        <input type="text" id="expCompany-${item.id}" placeholder="Ex: Google" onchange="updateExp(${item.id},'companyName',this.value)" />
      </div>
      <div class="field">
        <label for="expRole-${item.id}">Cargo</label>
        <input type="text" id="expRole-${item.id}" placeholder="Ex: Desenvolvedor Sênior" onchange="updateExp(${item.id},'jobTitle',this.value)" />
      </div>
      <div class="field">
        <label for="expStart-${item.id}">Data de Início</label>
        <input type="month" id="expStart-${item.id}" onchange="updateExp(${item.id},'startDate',this.value)" />
      </div>
      <div class="field" id="endDateField-${item.id}">
        <label for="endDate-${item.id}">Data de Fim</label>
        <input type="month" id="endDate-${item.id}" onchange="updateExp(${item.id},'endDate',this.value)" />
        <div class="checkbox-row">
          <input type="checkbox" id="current-${item.id}" onchange="toggleCurrentJob(${item.id}, this.checked)" />
          <label for="current-${item.id}">Emprego atual</label>
        </div>
      </div>
      <div class="field col-2">
        <label for="expDesc-${item.id}">Descrição das Atividades</label>
        <textarea rows="3" id="expDesc-${item.id}" placeholder="Descreva suas responsabilidades e conquistas..." onchange="updateExp(${item.id},'description',this.value)"></textarea>
      </div>
    </div>
  `;
  document.getElementById('experienceList')?.appendChild(div);
}

function showDateError(fieldId: string, hasError: boolean): void {
  const endField = document.getElementById(fieldId);
  if (!endField) return;
  const existing = endField.querySelector('.field-error');
  if (hasError) {
    endField.classList.add('invalid');
    if (!existing) {
      const err = document.createElement('span');
      err.className = 'field-error';
      err.textContent = 'Data de fim deve ser posterior à data de início';
      endField.appendChild(err);
    }
  } else {
    endField.classList.remove('invalid');
    existing?.remove();
  }
}

function validateExperienceDates(item: ExperienceItem): boolean {
  if (!item.startDate || !item.endDate || item.current) {
    showDateError(`endDateField-${item.id}`, false);
    return true;
  }
  const valid = item.endDate >= item.startDate;
  showDateError(`endDateField-${item.id}`, !valid);
  return valid;
}

// Keeps "Experiência #N" / "Formação #N" / "Idioma #N" in order after removals.
function renumberItems(listId: string, label: string): void {
  document.querySelectorAll(`#${listId} .dynamic-item-title`).forEach((el, i) => {
    const textNode = el.lastChild;
    if (textNode) textNode.textContent = ` ${label} #${i + 1}`;
  });
}

(window as any).updateExp = (id: number, key: string, value: string) => {
  const item = experiences.find((e) => e.id === id);
  if (item) {
    (item as any)[key] = value;
    if (key === 'startDate' || key === 'endDate') validateExperienceDates(item);
  }
};

(window as any).removeExperience = (id: number) => {
  const idx = experiences.findIndex((e) => e.id === id);
  if (idx !== -1) experiences.splice(idx, 1);
  const el = document.getElementById(`exp-${id}`);
  if (el) el.remove();
  renumberItems('experienceList', 'Experiência');
};

(window as any).toggleCurrentJob = (id: number, checked: boolean) => {
  const item = experiences.find((e) => e.id === id);
  if (item) {
    item.current = checked;
    const endInput = document.getElementById(`endDate-${id}`) as HTMLInputElement | null;
    if (endInput) {
      endInput.disabled = checked;
      if (checked) { endInput.value = ''; item.endDate = ''; }
    }
  }
};

// ── Education ──────────────────────────────────────────────────────────

const educations: EducationItem[] = [];
(window as any).__educations = educations;

export function addEducation(): void {
  const id = Date.now();
  const item: EducationItem = { id, course: '', institutionName: '', startDate: '', endDate: '', current: false };
  educations.push(item);
  renderEducationItem(item);
}

function renderEducationItem(item: EducationItem): void {
  const div = document.createElement('div');
  div.className = 'dynamic-item';
  div.id = `edu-${item.id}`;
  div.innerHTML = `
    <div class="dynamic-item-header">
      <span class="dynamic-item-title"><svg aria-hidden="true" width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M22 10v6M2 10l10-5 10 5-10 5z"/><path d="M6 12v5c3 3 9 3 12 0v-5"/></svg> Formação #${educations.indexOf(item) + 1}</span>
      <button type="button" class="remove-btn" onclick="removeEducation(${item.id})"><svg aria-hidden="true" width="12" height="12" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5" stroke-linecap="round" stroke-linejoin="round"><line x1="18" y1="6" x2="6" y2="18"/><line x1="6" y1="6" x2="18" y2="18"/></svg> Remover</button>
    </div>
    <div class="form-grid">
      <div class="field">
        <label for="eduCourse-${item.id}">Curso / Graduação</label>
        <input type="text" id="eduCourse-${item.id}" placeholder="Ex: Ciência da Computação" onchange="updateEdu(${item.id},'course',this.value)" />
      </div>
      <div class="field">
        <label for="eduInst-${item.id}">Instituição</label>
        <input type="text" id="eduInst-${item.id}" placeholder="Ex: USP" onchange="updateEdu(${item.id},'institutionName',this.value)" />
      </div>
      <div class="field">
        <label for="eduStart-${item.id}">Data de Início</label>
        <input type="month" id="eduStart-${item.id}" onchange="updateEdu(${item.id},'startDate',this.value)" />
      </div>
      <div class="field" id="eduEndDateField-${item.id}">
        <label for="eduEnd-${item.id}">Data de Fim</label>
        <input type="month" id="eduEnd-${item.id}" onchange="updateEdu(${item.id},'endDate',this.value)" />
        <div class="checkbox-row">
          <input type="checkbox" id="eduCurrent-${item.id}" onchange="toggleCurrentEdu(${item.id},this.checked)" />
          <label for="eduCurrent-${item.id}">Em andamento</label>
        </div>
      </div>
    </div>
  `;
  document.getElementById('educationList')?.appendChild(div);
}

function validateEducationDates(item: EducationItem): boolean {
  if (!item.startDate || !item.endDate || item.current) {
    showDateError(`eduEndDateField-${item.id}`, false);
    return true;
  }
  const valid = item.endDate >= item.startDate;
  showDateError(`eduEndDateField-${item.id}`, !valid);
  return valid;
}

(window as any).updateEdu = (id: number, key: string, value: string) => {
  const item = educations.find((e) => e.id === id);
  if (item) {
    (item as any)[key] = value;
    if (key === 'startDate' || key === 'endDate') validateEducationDates(item);
  }
};

(window as any).removeEducation = (id: number) => {
  const idx = educations.findIndex((e) => e.id === id);
  if (idx !== -1) educations.splice(idx, 1);
  const el = document.getElementById(`edu-${id}`);
  if (el) el.remove();
  renumberItems('educationList', 'Formação');
};

(window as any).toggleCurrentEdu = (id: number, checked: boolean) => {
  const item = educations.find((e) => e.id === id);
  if (item) {
    item.current = checked;
    const endInput = document.getElementById(`eduEnd-${id}`) as HTMLInputElement | null;
    if (endInput) {
      endInput.disabled = checked;
      if (checked) { endInput.value = ''; item.endDate = ''; }
    }
  }
};

// ── Skills ─────────────────────────────────────────────────────────────

const skills: string[] = [];
(window as any).__skills = skills;

const SKILL_SUGGESTIONS = [
  'JavaScript', 'TypeScript', 'React', 'Node.js', 'Python', 'C#', '.NET',
  'SQL', 'Git', 'Docker', 'AWS', 'Comunicação', 'Liderança', 'Agile/Scrum',
  'Resolução de Problemas', 'Trabalho em Equipe', 'HTML/CSS', 'Java',
];

function renderSkillTags(): void {
  const container = document.getElementById('skillsContainer');
  if (!container) return;
  if (skills.length === 0) {
    container.innerHTML = '<span class="tags-empty">Nenhuma habilidade adicionada ainda.</span>';
    return;
  }
  container.innerHTML = skills
    .map(
      (s) =>
        `<span class="tag">
          ${escHtml(s)}
          <button type="button" class="tag-remove" aria-label="Remover ${escHtml(s)}" onclick="removeSkill('${escHtml(s)}')"><svg width="10" height="10" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="3" stroke-linecap="round" stroke-linejoin="round"><line x1="18" y1="6" x2="6" y2="18"/><line x1="6" y1="6" x2="18" y2="18"/></svg></button>
        </span>`,
    )
    .join('');
}

function escHtml(str: string): string {
  return str.replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;').replace(/"/g, '&quot;');
}

export function addSkill(skill: string): void {
  const clean = skill.trim();
  if (!clean || skills.includes(clean)) return;
  skills.push(clean);
  renderSkillTags();
  const input = document.getElementById('skillInput') as HTMLInputElement | null;
  if (input) input.value = '';
}

(window as any).removeSkill = (skill: string) => {
  const idx = skills.indexOf(skill);
  if (idx !== -1) skills.splice(idx, 1);
  renderSkillTags();
};

const skillInput = document.getElementById('skillInput') as HTMLInputElement | null;
document.getElementById('addSkillBtn')?.addEventListener('click', () => {
  if (skillInput) addSkill(skillInput.value);
});
skillInput?.addEventListener('keydown', (e) => {
  if (e.key === 'Enter') {
    e.preventDefault();
    if (skillInput) addSkill(skillInput.value);
  }
});

// Suggestion chips
const suggestionsChips = document.getElementById('suggestionsChips');
SKILL_SUGGESTIONS.forEach((sug) => {
  const chip = document.createElement('button');
  chip.type = 'button';
  chip.className = 'suggestion-chip';
  chip.textContent = sug;
  chip.addEventListener('click', () => addSkill(sug));
  suggestionsChips?.appendChild(chip);
});

// ── Languages ──────────────────────────────────────────────────────────

interface LanguageItem {
  id: number;
  name: string;
  proficiency: string;
}

const languages: LanguageItem[] = [];
(window as any).__languages = languages;

const PROFICIENCY_LEVELS = ['Básico', 'Intermediário', 'Avançado', 'Fluente', 'Nativo'];

export function addLanguage(): void {
  const id = Date.now();
  const item: LanguageItem = { id, name: '', proficiency: '' };
  languages.push(item);
  renderLanguageItem(item);
}

function renderLanguageItem(item: LanguageItem): void {
  const options = PROFICIENCY_LEVELS.map((l) => `<option value="${l}">${l}</option>`).join('');
  const div = document.createElement('div');
  div.className = 'dynamic-item';
  div.id = `lang-${item.id}`;
  div.innerHTML = `
    <div class="dynamic-item-header">
      <span class="dynamic-item-title"><svg aria-hidden="true" width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><circle cx="12" cy="12" r="10"/><line x1="2" y1="12" x2="22" y2="12"/><path d="M12 2a15.3 15.3 0 014 10 15.3 15.3 0 01-4 10 15.3 15.3 0 01-4-10 15.3 15.3 0 014-10z"/></svg> Idioma #${languages.indexOf(item) + 1}</span>
      <button type="button" class="remove-btn" onclick="removeLanguage(${item.id})"><svg aria-hidden="true" width="12" height="12" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5" stroke-linecap="round" stroke-linejoin="round"><line x1="18" y1="6" x2="6" y2="18"/><line x1="6" y1="6" x2="18" y2="18"/></svg> Remover</button>
    </div>
    <div class="form-grid">
      <div class="field">
        <label for="langName-${item.id}">Idioma</label>
        <input type="text" id="langName-${item.id}" placeholder="Ex: Inglês" onchange="updateLang(${item.id},'name',this.value)" />
      </div>
      <div class="field">
        <label for="langLevel-${item.id}">Nível</label>
        <select id="langLevel-${item.id}" onchange="updateLang(${item.id},'proficiency',this.value)">
          <option value="" disabled selected>Selecione o nível</option>
          ${options}
        </select>
      </div>
    </div>
  `;
  document.getElementById('languageList')?.appendChild(div);
}

(window as any).updateLang = (id: number, key: string, value: string) => {
  const item = languages.find((l) => l.id === id);
  if (item) (item as any)[key] = value;
};

(window as any).removeLanguage = (id: number) => {
  const idx = languages.findIndex((l) => l.id === id);
  if (idx !== -1) languages.splice(idx, 1);
  const el = document.getElementById(`lang-${id}`);
  if (el) el.remove();
  renumberItems('languageList', 'Idioma');
};

document.getElementById('addExperienceBtn')?.addEventListener('click', addExperience);
document.getElementById('addEducationBtn')?.addEventListener('click', addEducation);
document.getElementById('addLanguageBtn')?.addEventListener('click', addLanguage);
