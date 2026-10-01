import type { AuthResponse, RegisterPayload, LoginPayload } from '../types';
import { apiGet, apiPost } from './api';
import { showToast } from '../utils/ui';

let authUser: { id: string; name: string; email: string; plan: string } | null = null;
export let authToken: string | null = localStorage.getItem('token');

export function getAuthUser() {
  return authUser;
}

export function setAuthUser(user: typeof authUser) {
  authUser = user;
}

export function isAuthenticated(): boolean {
  return !!authToken;
}

export async function login(payload: LoginPayload): Promise<AuthResponse> {
  const data = await apiPost<AuthResponse>('/auth/login', payload);
  authToken = data.token;
  authUser = data.user;
  localStorage.setItem('token', authToken!);
  return data;
}

export async function register(payload: RegisterPayload): Promise<AuthResponse> {
  const data = await apiPost<AuthResponse>('/auth/register', payload);
  authToken = data.token;
  authUser = data.user;
  localStorage.setItem('token', authToken!);
  return data;
}

export async function checkAuth(): Promise<void> {
  if (!authToken) return;
  try {
    const user = await apiGet<{ id: string; name: string; email: string; plan: string }>('/auth/me');
    authUser = user;
  } catch {
    authToken = null;
    authUser = null;
    localStorage.removeItem('token');
  }
}

export function logout(): void {
  authToken = null;
  authUser = null;
  localStorage.removeItem('token');
}

export function updateAuthUI(): void {
  const authBtn = document.getElementById('authBtn') as HTMLElement | null;
  const body = document.getElementById('settingsDropdownBody');

  if (authUser) {
    if (authBtn) authBtn.hidden = true;
    if (body) {
      const existingThemeSection = body.querySelector('.settings-theme-btn');
      body.innerHTML = `
        <div class="settings-user-info">
          <div class="settings-user-name">${escHtml(authUser.name)}</div>
          <div class="settings-user-email">${escHtml(authUser.email)}</div>
        </div>
        <hr class="settings-divider">
        <button class="settings-logout-btn" id="logoutBtn">
          <svg aria-hidden="true" width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M9 21H5a2 2 0 01-2-2V5a2 2 0 012-2h4"/><polyline points="16 17 21 12 16 7"/><line x1="21" y1="12" x2="9" y2="12"/></svg>
          Sair
        </button>
      `;
      if (existingThemeSection) {
        body.prepend(existingThemeSection);
        body.prepend(document.createElement('hr').cloneNode());
      }
      document.getElementById('logoutBtn')?.addEventListener('click', () => {
        logout();
        updateAuthUI();
        const dropdown = document.getElementById('settingsDropdown');
        if (dropdown) dropdown.hidden = true;
        const list = document.getElementById('resumesList');
        if (list) list.innerHTML = '<p class="tags-empty">Faça login para ver seus currículos salvos.</p>';
        showToast('Sessão encerrada.', 'success');
      });
    }
  } else {
    if (authBtn) authBtn.hidden = false;
    if (body) {
      const existingThemeSection = body.querySelector('.settings-theme-btn');
      body.innerHTML = '';
      if (existingThemeSection) {
        body.appendChild(existingThemeSection);
      }
    }
  }
}

function escHtml(s: string): string {
  const d = document.createElement('div');
  d.textContent = s;
  return d.innerHTML;
}
