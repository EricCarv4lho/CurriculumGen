import { $ } from './dom';
import {
  login,
  register,
  checkAuth,
  updateAuthUI,
  isAuthenticated,
  logout,
} from './services/auth';
import { buildPayload, generateCurriculum, generateCoverLetter, downloadBlob } from './services/curriculum';
import { translateCurriculum } from './services/translate';
import { showToast, showLoading, formatFileSize, toUserMessage, showConfirm } from './utils/ui';
import { validateStep } from './validation';
import { listResumes, createResume, uploadResumePdf, downloadResumePdfBlob, deleteResume, getResume, templateNameToId, templateIdToName, formatDate } from './services/resumes';
import './form-state';

// ── Theme ────────────────────────────────────────────────────────────

const savedTheme = localStorage.getItem('theme')
  || (window.matchMedia('(prefers-color-scheme: dark)').matches ? 'dark' : 'light');
document.documentElement.setAttribute('data-theme', savedTheme);
updateSettingsThemeUI(savedTheme);

function updateSettingsThemeUI(theme: string): void {
  const sun = $.settingsThemeToggle?.querySelector('.st-sun') as HTMLElement | null;
  const moon = $.settingsThemeToggle?.querySelector('.st-moon') as HTMLElement | null;
  const label = document.getElementById('settingsThemeLabel');
  if (!sun || !moon || !label) return;
  if (theme === 'dark') {
    sun.removeAttribute('hidden');
    moon.setAttribute('hidden', '');
    label.textContent = 'Tema claro';
  } else {
    moon.removeAttribute('hidden');
    sun.setAttribute('hidden', '');
    label.textContent = 'Tema escuro';
  }
}

$.settingsThemeToggle?.addEventListener('click', () => {
  const current = document.documentElement.getAttribute('data-theme');
  const next = current === 'dark' ? 'light' : 'dark';
  document.documentElement.setAttribute('data-theme', next);
  localStorage.setItem('theme', next);
  updateSettingsThemeUI(next);
});

// ── Settings dropdown ────────────────────────────────────────────────

$.settingsBtn?.addEventListener('click', (e) => {
  e.stopPropagation();
  if ($.settingsDropdown) $.settingsDropdown.hidden = !$.settingsDropdown.hidden;
});

document.addEventListener('click', () => {
  if ($.settingsDropdown && !$.settingsDropdown.hidden) $.settingsDropdown.hidden = true;
});

$.settingsDropdown?.addEventListener('click', (e) => {
  e.stopPropagation();
});

// ── Auth ─────────────────────────────────────────────────────────────

let isRegisterMode = false;

$.authModal?.addEventListener('click', (e: MouseEvent) => {
  const target = e.target as HTMLElement;
  if (target.id === 'authToggle') {
    e.preventDefault();
    isRegisterMode = !isRegisterMode;
    if ($.authModalTitle) $.authModalTitle.textContent = isRegisterMode ? 'Criar Conta' : 'Entrar';
    if ($.authSubmit) $.authSubmit.textContent = isRegisterMode ? 'Cadastrar' : 'Entrar';
    if ($.authToggleText) {
      $.authToggleText.innerHTML = isRegisterMode
        ? 'Já tem conta? <a href="#" id="authToggle">Entrar</a>'
        : 'Não tem conta? <a href="#" id="authToggle">Cadastre-se</a>';
    }
    if ($.authNameField) $.authNameField.hidden = !isRegisterMode;
    if ($.authName) $.authName.required = isRegisterMode;
    if ($.authError) $.authError.textContent = '';
  }
  if (e.target === e.currentTarget) {
    $.authModal?.classList.remove('active');
    if ($.authError) $.authError.textContent = '';
  }
});

function openAuthModal(): void {
  $.authModal?.classList.add('active');
  $.authEmail?.focus();
}

$.authBtn?.addEventListener('click', openAuthModal);

document.addEventListener('keydown', (e) => {
  if (e.key === 'Escape' && $.authModal?.classList.contains('active')) {
    $.authModal.classList.remove('active');
    if ($.authError) $.authError.textContent = '';
  }
});

// Set when the login modal was opened by clicking "Meus Currículos" while
// logged out — after a successful login we go straight to that tab.
let pendingResumesLoad = false;

$.authForm?.addEventListener('submit', async (e) => {
  e.preventDefault();

  const email = $.authEmail?.value.trim() || '';
  const password = $.authPassword?.value || '';
  const name = $.authName?.value.trim() || '';
  if ($.authError) $.authError.textContent = '';

  const submitBtn = $.authSubmit;
  if (submitBtn) {
    submitBtn.disabled = true;
    submitBtn.classList.add('is-loading');
    submitBtn.textContent = isRegisterMode ? 'Cadastrando...' : 'Entrando...';
  }

  try {
    if (isRegisterMode) {
      await register({ name, email, password });
    } else {
      await login({ email, password });
    }
    updateAuthUI();
    $.authModal?.classList.remove('active');
    showToast(isRegisterMode ? 'Conta criada com sucesso!' : 'Login realizado!', 'success');
    if (pendingResumesLoad) {
      pendingResumesLoad = false;
      switchTab('btn-resumes');
    }
  } catch (err: unknown) {
    if ($.authError) $.authError.textContent = toUserMessage(err, 'Erro de conexão com o servidor.');
  } finally {
    if (submitBtn) {
      submitBtn.disabled = false;
      submitBtn.classList.remove('is-loading');
      submitBtn.textContent = isRegisterMode ? 'Cadastrar' : 'Entrar';
    }
  }
});

// Fired by services/api.ts when a non-auth endpoint answers 401 mid-session.
window.addEventListener('session-expired', () => {
  logout();
  updateAuthUI();
  const resumesSection = document.getElementById('sectionResumes');
  if (resumesSection && !resumesSection.hidden) loadResumes();
  if ($.authError) $.authError.textContent = 'Sua sessão expirou. Entre novamente para continuar.';
  openAuthModal();
});

// ── Translate ────────────────────────────────────────────────────────

let selectedFile: File | null = null;

$.dropzoneArea?.addEventListener('click', () => {
  if (!selectedFile) $.fileInput?.click();
});

$.dropzoneArea?.addEventListener('keydown', (e: KeyboardEvent) => {
  if (e.key === 'Enter' || e.key === ' ') {
    e.preventDefault();
    if (!selectedFile) $.fileInput?.click();
  }
});

$.dropzoneArea?.addEventListener('dragover', (e) => {
  e.preventDefault();
  $.dropzoneArea?.classList.add('drag-over');
});

$.dropzoneArea?.addEventListener('dragleave', () => {
  $.dropzoneArea?.classList.remove('drag-over');
});

$.dropzoneArea?.addEventListener('drop', (e) => {
  e.preventDefault();
  $.dropzoneArea?.classList.remove('drag-over');
  const files = e.dataTransfer?.files;
  if (files && files.length > 0) handleFile(files[0]);
});

$.fileInput?.addEventListener('change', () => {
  const files = $.fileInput?.files;
  if (files && files.length > 0) handleFile(files[0]);
});

function handleFile(file: File): void {
  const maxSize = 10 * 1024 * 1024;
  const allowedTypes = [
    'application/pdf',
    'application/msword',
    'application/vnd.openxmlformats-officedocument.wordprocessingml.document',
    'image/jpeg',
    'image/png',
  ];

  if (!allowedTypes.includes(file.type)) {
    showToast('Formato de arquivo não suportado.', 'error');
    return;
  }

  if (file.size > maxSize) {
    showToast('Arquivo muito grande. Máximo: 10MB', 'error');
    return;
  }

  selectedFile = file;
  if ($.fileName) $.fileName.textContent = file.name;
  if ($.fileSize) $.fileSize.textContent = formatFileSize(file.size);
  if ($.filePreview) $.filePreview.hidden = false;
  $.dropzoneArea?.classList.add('has-file');
  updateTranslateBtn();
}

$.removeFile?.addEventListener('click', (e) => {
  e.stopPropagation();
  selectedFile = null;
  if ($.fileInput) $.fileInput.value = '';
  if ($.filePreview) $.filePreview.hidden = true;
  $.dropzoneArea?.classList.remove('has-file');
  updateTranslateBtn();
});

$.targetLanguage?.addEventListener('change', updateTranslateBtn);

function updateTranslateBtn(): void {
  if ($.translateBtn) {
    $.translateBtn.disabled = !(selectedFile && $.targetLanguage?.value);
  }
}

$.translateBtn?.addEventListener('click', async () => {
  if (!selectedFile || !$.targetLanguage?.value) return;

  showLoading(true, 'Traduzindo seu currículo...');
  $.translateBtn!.disabled = true;

  try {
    const blob = await translateCurriculum(selectedFile, $.targetLanguage.value);

    const filename = `curriculo_${$.targetLanguage.value}_${selectedFile.name.replace(/\.[^/.]+$/, '')}.pdf`;
    downloadBlob(blob, filename);

    if (isAuthenticated()) {
      const langOpt = $.targetLanguage?.options[$.targetLanguage.selectedIndex];
      const langName = langOpt?.text || $.targetLanguage.value;
      const tName = `Tradução - ${langName} - ${selectedFile.name}`;
      try {
        const saved = await createResume(tName, null, buildPayload());
        await uploadResumePdf(saved.id, blob);
      } catch {
        showToast('Currículo traduzido, mas não foi possível salvar em "Meus Currículos".', 'warning');
      }
    }

    showToast('Currículo traduzido e baixado com sucesso!', 'success');
  } catch (err: unknown) {
    showToast(toUserMessage(err), 'error');
  } finally {
    showLoading(false);
    $.translateBtn!.disabled = false;
  }
});

// ── Generate ─────────────────────────────────────────────────────────

$.form?.addEventListener('submit', async (e) => {
  e.preventDefault();
  if (!validateStep(1)) return;

  const payload = buildPayload();

  if (!payload.fullName || !payload.email) {
    showToast('Preencha pelo menos as informações pessoais antes de gerar o currículo.', 'warning');
    return;
  }

  showLoading(true, 'Gerando seu currículo...');
  if ($.submitBtn) $.submitBtn.disabled = true;

  try {
    const blob = await generateCurriculum(payload);
    downloadBlob(blob, `curriculo_${payload.fullName.replace(/\s+/g, '_')}.pdf`);

    if (isAuthenticated()) {
      const templateId = templateNameToId(payload.template);
      const name = `Currículo - ${payload.fullName} (${new Date().toLocaleDateString('pt-BR')})`;
      await createResume(name, templateId, payload);
      showToast('Currículo gerado e salvo em "Meus Currículos"!', 'success');
    } else {
      showToast('Currículo gerado e baixado com sucesso!', 'success');
    }
  } catch (err: unknown) {
    showToast(toUserMessage(err), 'error');
  } finally {
    showLoading(false);
    if ($.submitBtn) $.submitBtn.disabled = false;
  }
});

// ── Cover Letter ──────────────────────────────────────────────────────

async function handleGenerateCover(): Promise<void> {
  const jobDesc = $.coverJobDesc?.value.trim();
  if (!jobDesc) {
    showToast('Por favor, cole a descrição da vaga.', 'warning');
    return;
  }

  showLoading(true, 'Escrevendo sua carta...');
  if ($.coverGenerateBtn) $.coverGenerateBtn.disabled = true;

  try {
    const extraNotes = $.coverExtra?.value.trim() || '';
    const resumeData = JSON.stringify(buildPayload());

    const text = await generateCoverLetter({
      resumeData,
      jobDescription: extraNotes
        ? `${jobDesc}\n\nOBSERVAÇÕES ADICIONAIS:\n${extraNotes}`
        : jobDesc,
    });

    if ($.coverLetterBody) {
      $.coverLetterBody.textContent = text;
    }
    if ($.coverResult) $.coverResult.hidden = false;
    showToast('Carta gerada com sucesso!', 'success');
  } catch (err: unknown) {
    showToast(toUserMessage(err), 'error');
  } finally {
    showLoading(false);
    if ($.coverGenerateBtn) $.coverGenerateBtn.disabled = false;
  }
}

$.coverGenerateBtn?.addEventListener('click', handleGenerateCover);
$.coverRegenBtn?.addEventListener('click', handleGenerateCover);

$.coverCopyBtn?.addEventListener('click', async () => {
  const text = $.coverLetterBody?.textContent;
  if (!text) return;
  try {
    await navigator.clipboard.writeText(text);
    showToast('Carta copiada para a área de transferência!', 'success');
  } catch {
    showToast('Não foi possível copiar. Selecione o texto manualmente.', 'error');
  }
});

// ── Steps ─────────────────────────────────────────────────────────────

const TOTAL_STEPS = 7;
let currentStep = 1;

function goToStep(step: number): void {
  document.querySelectorAll('.step-section').forEach((s) => s.classList.remove('active'));
  document.getElementById(`step${step}`)?.classList.add('active');

  document.querySelectorAll('.step-btn').forEach((btn, i) => {
    const num = i + 1;
    btn.classList.remove('active', 'done');
    if (num === step) btn.classList.add('active');
    if (num < step) btn.classList.add('done');
    if (num === step) btn.setAttribute('aria-current', 'step');
    else btn.removeAttribute('aria-current');
  });

  if ($.prevBtn) $.prevBtn.hidden = step <= 1;
  if ($.nextBtn) $.nextBtn.hidden = step === TOTAL_STEPS;
  if ($.submitBtn) $.submitBtn.hidden = step !== TOTAL_STEPS;

  currentStep = step;
  window.scrollTo({ top: 0, behavior: 'smooth' });
  // On narrow screens the step row scrolls horizontally — keep the active
  // step visible instead of letting it hide past the fade.
  document.querySelector(`.step-btn[data-step="${step}"]`)?.scrollIntoView({
    behavior: 'smooth',
    block: 'nearest',
    inline: 'center',
  });
}

$.nextBtn?.addEventListener('click', () => {
  if (validateStep(currentStep)) {
    goToStep(currentStep + 1);
  }
});

$.prevBtn?.addEventListener('click', () => goToStep(currentStep - 1));

document.querySelectorAll('.step-btn').forEach((btn) => {
  btn.addEventListener('click', () => {
    const target = parseInt((btn as HTMLElement).dataset.step ?? '0');
    if (target < currentStep) goToStep(target);
  });
});

// ── Tabs ──────────────────────────────────────────────────────────────

async function loadResumes(): Promise<void> {
  const container = document.getElementById('resumesList');
  if (!container) return;

  if (!isAuthenticated()) {
    container.innerHTML = '<p class="tags-empty">Faça login para ver seus currículos salvos.</p>';
    return;
  }

  try {
    container.innerHTML = `
      <div class="list-loading" role="status" aria-label="Carregando currículos">
        <div class="spinner"></div>
      </div>`;

    const resumes = await listResumes();
    if (resumes.length === 0) {
      container.innerHTML = '<p class="tags-empty">Nenhum currículo salvo ainda. Gere um currículo e ele será salvo automaticamente.</p>';
      return;
    }

    container.innerHTML = resumes.map((r) => `
      <div class="resume-card" data-id="${r.id}">
        <div class="resume-card-info">
          <div class="resume-card-name">${escHtml(r.name)}</div>
          <div class="resume-card-date">${templateIdToName(r.templateId)} &middot; ${formatDate(r.updatedAt)}</div>
        </div>
        <div class="resume-card-actions">
          <button type="button" class="btn btn-sm btn-primary resume-download" data-id="${r.id}">
            <svg aria-hidden="true" width="12" height="12" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5" stroke-linecap="round" stroke-linejoin="round"><path d="M21 15v4a2 2 0 01-2 2H5a2 2 0 01-2-2v-4"/><polyline points="7 10 12 15 17 10"/><line x1="12" y1="15" x2="12" y2="3"/></svg>
            Baixar
          </button>
          <button type="button" class="btn btn-sm btn-ghost resume-delete" data-id="${r.id}">
            <svg aria-hidden="true" width="12" height="12" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5" stroke-linecap="round" stroke-linejoin="round"><polyline points="3 6 5 6 21 6"/><path d="M19 6v14a2 2 0 01-2 2H7a2 2 0 01-2-2V6m3 0V4a2 2 0 012-2h4a2 2 0 012 2v2"/></svg>
          </button>
        </div>
      </div>
    `).join('');

    container.querySelectorAll('.resume-download').forEach((btn) => {
      btn.addEventListener('click', async (e) => {
        const id = parseInt((e.currentTarget as HTMLElement).dataset.id ?? '0');
        await downloadResume(id);
      });
    });

    container.querySelectorAll('.resume-delete').forEach((btn) => {
      btn.addEventListener('click', async (e) => {
        const id = parseInt((e.currentTarget as HTMLElement).dataset.id ?? '0');
        await removeResume(id);
      });
    });
  } catch {
    container.innerHTML = `
      <p class="tags-empty">Erro ao carregar currículos.</p>
      <div class="retry-wrap">
        <button type="button" class="btn btn-sm" id="retryResumesBtn">Tentar novamente</button>
      </div>`;
    document.getElementById('retryResumesBtn')?.addEventListener('click', () => loadResumes());
  }
}

function setResumeOverlayMessage(message: string): void {
  const text = document.querySelector('#resumeLoadingOverlay .loading-text');
  if (text) text.textContent = message;
}

async function downloadResume(id: number): Promise<void> {
  const overlay = document.getElementById('resumeLoadingOverlay');
  setResumeOverlayMessage('Preparando download...');
  if (overlay) overlay.classList.add('active');

  try {
    const resume = await getResume(id);

    if (resume.hasPdf) {
      setResumeOverlayMessage('Baixando PDF...');
      const blob = await downloadResumePdfBlob(id);
      downloadBlob(blob, `${resume.name}.pdf`);
      showToast('Currículo baixado com sucesso!', 'success');
      return;
    }

    if (!resume.data || !resume.data.fullName || !resume.data.email) {
      showToast('Este currículo está incompleto e não pode ser baixado. Remova-o e crie um novo.', 'warning');
      return;
    }
    setResumeOverlayMessage('Gerando PDF...');
    const blob = await generateCurriculum(resume.data);
    downloadBlob(blob, `curriculo_${resume.data.fullName.replace(/\s+/g, '_')}.pdf`);
    showToast('Currículo baixado com sucesso!', 'success');
  } catch (err: unknown) {
    showToast(toUserMessage(err, 'Erro ao baixar currículo.'), 'error');
  } finally {
    if (overlay) overlay.classList.remove('active');
  }
}

async function removeResume(id: number): Promise<void> {
  const confirmed = await showConfirm('Excluir este currículo? Esta ação não pode ser desfeita.', 'Excluir');
  if (!confirmed) return;
  try {
    await deleteResume(id);
    showToast('Currículo removido.', 'success');
    await loadResumes();
  } catch (err: unknown) {
    showToast(toUserMessage(err, 'Erro ao remover currículo.'), 'error');
  }
}

function switchTab(tabId: string): void {
  document.querySelectorAll('.tab-btn').forEach((b) => {
    const active = b.id === tabId;
    b.classList.toggle('active', active);
    b.setAttribute('aria-selected', active ? 'true' : 'false');
  });

  if (tabId === 'btn-resumes') {
    if (!isAuthenticated()) {
      pendingResumesLoad = true;
      openAuthModal();
      document.getElementById('sectionCreateForm')!.hidden = false;
      document.getElementById('sectionTranslateForm')!.hidden = true;
      document.getElementById('sectionCover')!.hidden = true;
      document.getElementById('sectionResumes')!.hidden = true;
      const createBtn = document.getElementById('btn-createForm');
      createBtn?.classList.add('active');
      createBtn?.setAttribute('aria-selected', 'true');
      const resumesBtn = document.getElementById('btn-resumes');
      resumesBtn?.classList.remove('active');
      resumesBtn?.setAttribute('aria-selected', 'false');
      return;
    }
    document.getElementById('sectionCreateForm')!.hidden = true;
    document.getElementById('sectionTranslateForm')!.hidden = true;
    document.getElementById('sectionCover')!.hidden = true;
    document.getElementById('sectionResumes')!.hidden = false;
    loadResumes();
    return;
  }

  document.getElementById('sectionCreateForm')!.hidden = tabId !== 'btn-createForm';
  document.getElementById('sectionTranslateForm')!.hidden = tabId !== 'btn-translate';
  document.getElementById('sectionCover')!.hidden = tabId !== 'btn-cover';
  document.getElementById('sectionResumes')!.hidden = tabId !== 'btn-resumes';
}

document.querySelectorAll('.tab-btn').forEach((btn) => {
  btn.addEventListener('click', () => switchTab(btn.id));
});

function escHtml(s: string): string {
  const d = document.createElement('div');
  d.textContent = s;
  return d.innerHTML;
}

// ── Template Selector ────────────────────────────────────────────────

document.querySelectorAll('.template-card').forEach((card) => {
  const selectCard = (target: Element) => {
    document.querySelectorAll('.template-card').forEach((c) => {
      c.classList.remove('selected');
      c.setAttribute('aria-pressed', 'false');
    });
    target.classList.add('selected');
    target.setAttribute('aria-pressed', 'true');
    const val = (target as HTMLElement).dataset.template ?? 'classic';
    const select = document.getElementById('templateSelect') as HTMLSelectElement | null;
    if (select) select.value = val;
  };

  card.addEventListener('click', () => selectCard(card));
  (card as HTMLElement).addEventListener('keydown', (e: KeyboardEvent) => {
    if (e.key === 'Enter' || e.key === ' ') {
      e.preventDefault();
      selectCard(card);
    }
  });
});

// ── Character counters ───────────────────────────────────────────────

function bindCharCounter(textareaId: string, counterId: string): void {
  const textarea = document.getElementById(textareaId) as HTMLTextAreaElement | null;
  const counter = document.getElementById(counterId);
  if (!textarea || !counter) return;
  const update = () => {
    counter.textContent = `${textarea.value.length} / ${textarea.maxLength}`;
  };
  textarea.addEventListener('input', update);
  update();
}

bindCharCounter('careerObjective', 'countCareerObjective');
bindCharCounter('highlights', 'countHighlights');

// ── Init ─────────────────────────────────────────────────────────────

checkAuth().then(() => {
  updateAuthUI();
  goToStep(1);
});

// Every SVG in the app is a decorative icon next to a text label.
document.querySelectorAll('svg').forEach((s) => s.setAttribute('aria-hidden', 'true'));
