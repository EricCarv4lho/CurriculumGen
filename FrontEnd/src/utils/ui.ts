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

export function showLoading(show: boolean): void {
  const overlay = document.getElementById('loadingOverlay');
  if (overlay) overlay.classList.toggle('active', show);
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
