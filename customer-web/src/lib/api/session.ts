export type SessionUser = {
  id: string;
  name: string;
  email?: string;
  location?: string;
  avatarUrl?: string;
};

const USER_KEY = 'om_user';
const TOKEN_KEY = 'om_token';
const EXPIRES_AT_KEY = 'om_token_expires_at';

function decodeJwtExpiresAt(token: string): number | null {
  try {
    const payload = token.split('.')[1];
    if (!payload) return null;
    const normalized = payload.replace(/-/g, '+').replace(/_/g, '/');
    const json = JSON.parse(atob(normalized.padEnd(Math.ceil(normalized.length / 4) * 4, '=')));
    return typeof json.exp === 'number' ? json.exp * 1000 : null;
  } catch {
    return null;
  }
}

function isExpired(expiresAt: string | null) {
  const value = Number(expiresAt || 0);
  return Boolean(value && Date.now() >= value);
}

export function isSessionExpired() {
  if (typeof window === 'undefined') return false;
  const token = localStorage.getItem(TOKEN_KEY);
  if (!token) return false;
  const savedExpiresAt = localStorage.getItem(EXPIRES_AT_KEY);
  const jwtExpiresAt = decodeJwtExpiresAt(token);
  if (jwtExpiresAt && !savedExpiresAt) localStorage.setItem(EXPIRES_AT_KEY, String(jwtExpiresAt));
  return isExpired(savedExpiresAt || (jwtExpiresAt ? String(jwtExpiresAt) : null));
}

export function redirectToLogin() {
  if (typeof window === 'undefined') return;
  const current = `${window.location.pathname}${window.location.search}`;
  if (!window.location.pathname.startsWith('/login')) {
    window.location.href = `/login?returnUrl=${encodeURIComponent(current)}`;
  }
}

export function getSessionUser(): SessionUser | null {
  if (typeof window === 'undefined') return null;
  if (isSessionExpired()) {
    clearSession();
    return null;
  }
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
  if (isSessionExpired()) {
    clearSession();
    return null;
  }
  return localStorage.getItem(TOKEN_KEY);
}

export function saveSession(payload: { token?: string; accessToken?: string; expiresInMinutes?: number; expiresIn?: number; user?: any }) {
  if (typeof window === 'undefined') return;
  const token = payload.token ?? payload.accessToken ?? '';
  if (token) {
    localStorage.setItem(TOKEN_KEY, token);
    const jwtExpiresAt = decodeJwtExpiresAt(token);
    const minutesExpiresAt = payload.expiresInMinutes ? Date.now() + payload.expiresInMinutes * 60 * 1000 : null;
    const secondsExpiresAt = payload.expiresIn ? Date.now() + payload.expiresIn * 1000 : null;
    const expiresAt = jwtExpiresAt || minutesExpiresAt || secondsExpiresAt;
    if (expiresAt) localStorage.setItem(EXPIRES_AT_KEY, String(expiresAt));
  }
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
  localStorage.removeItem(EXPIRES_AT_KEY);
  localStorage.removeItem(USER_KEY);
  localStorage.removeItem('om_user_name');
  window.dispatchEvent(new Event('om-session-changed'));
}
