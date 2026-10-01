import { ApiError } from '../services/api';

let toastTimer: ReturnType<typeof setTimeout>;

export function showToast(message: string, type: 'success' | 'error' | 'warning' = 'success'): void {
  const toast = document.getElementById('toast');
  if (!toast) return;
  toast.textContent = message;
  toast.className = `toast show ${type}`;
  clearTimeout(toastTimer);
  toastTimer = setTimeout(() => {
    toast.className = 'toast';
  }, 4200);
}

export function showLoading(show: boolean, message?: string): void {
  const overlay = document.getElementById('loadingOverlay');
  if (!overlay) return;
  if (message) {
    const text = overlay.querySelector('.loading-text');
    if (text) text.textContent = message;
  }
  overlay.classList.toggle('active', show);
}

export function escHtml(str: string): string {
  return str
    .replace(/&/g, '&amp;')
    .replace(/</g, '&lt;')
    .replace(/>/g, '&gt;')
    .replace(/"/g, '&quot;');
}

export function formatFileSize(bytes: number): string {
  if (bytes < 1024) return bytes + ' B';
  if (bytes < 1024 * 1024) return (bytes / 1024).toFixed(1) + ' KB';
  return (bytes / (1024 * 1024)).toFixed(1) + ' MB';
}

/**
 * Normalizes any thrown error into a user-facing message. fetch rejects with
 * a TypeError when offline/unreachable — that must never reach the UI raw.
 */
export function toUserMessage(err: unknown, fallback = 'Algo deu errado. Tente novamente.'): string {
  if (err instanceof ApiError) return err.message;
  if (err instanceof TypeError) return 'Falha na conexão. Verifique sua internet e tente novamente.';
  if (err instanceof Error && err.message) return err.message;
  return fallback;
}

export function showConfirm(message: string, confirmText = 'Confirmar'): Promise<boolean> {
  return new Promise((resolve) => {
    const overlay = document.getElementById('confirmModal');
    const messageEl = document.getElementById('confirmMessage');
    const okBtn = document.getElementById('confirmOk');
    const cancelBtn = document.getElementById('confirmCancel');
    if (!overlay || !messageEl || !okBtn || !cancelBtn) {
      resolve(false);
      return;
    }

    messageEl.textContent = message;
    okBtn.textContent = confirmText;
    overlay.classList.add('active');
    okBtn.focus();

    const close = (result: boolean) => {
      overlay.classList.remove('active');
      okBtn.removeEventListener('click', onOk);
      cancelBtn.removeEventListener('click', onCancel);
      overlay.removeEventListener('click', onOverlay);
      document.removeEventListener('keydown', onKeydown);
      resolve(result);
    };
    const onOk = () => close(true);
    const onCancel = () => close(false);
    const onOverlay = (e: Event) => {
      if (e.target === overlay) close(false);
    };
    const onKeydown = (e: KeyboardEvent) => {
      if (e.key === 'Escape') close(false);
    };

    okBtn.addEventListener('click', onOk);
    cancelBtn.addEventListener('click', onCancel);
    overlay.addEventListener('click', onOverlay);
    document.addEventListener('keydown', onKeydown);
  });
}
