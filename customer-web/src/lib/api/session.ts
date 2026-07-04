export type SessionUser = {
  id: string;
  name: string;
  email?: string;
  location?: string;
  avatarUrl?: string;
};

const USER_KEY = 'om_user';
const TOKEN_KEY = 'om_token';

export function getSessionUser(): SessionUser | null {
  if (typeof window === 'undefined') return null;
  const raw = localStorage.getItem(USER_KEY);
  if (raw) {
    try {
      const parsed = JSON.parse(raw);
      if (parsed?.id) return parsed as SessionUser;
    } catch {}
  }
  return null;
}

export function getSessionToken(): string | null {
  if (typeof window === 'undefined') return null;
  return localStorage.getItem(TOKEN_KEY);
}

export function saveSession(payload: { token?: string; accessToken?: string; user?: any }) {
  if (typeof window === 'undefined') return;
  const token = payload.token ?? payload.accessToken ?? '';
  if (token) localStorage.setItem(TOKEN_KEY, token);
  if (payload.user) {
    localStorage.setItem(USER_KEY, JSON.stringify({
      id: payload.user.id,
      name: payload.user.name ?? payload.user.fullName ?? payload.user.email ?? 'Customer',
      email: payload.user.email,
      location: payload.user.location,
      avatarUrl: payload.user.avatarUrl,
    }));
  }
  window.dispatchEvent(new Event('om-session-changed'));
}

export function clearSession() {
  if (typeof window === 'undefined') return;
  localStorage.removeItem(TOKEN_KEY);
  localStorage.removeItem(USER_KEY);
  localStorage.removeItem('om_user_name');
  window.dispatchEvent(new Event('om-session-changed'));
}
