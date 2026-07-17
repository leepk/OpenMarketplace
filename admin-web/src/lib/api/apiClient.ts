import { appConfig } from '../config';

export type AdminSessionUser = {
  id: string;
  name: string;
  email: string;
  role: string;
  source?: string;
  avatarUrl?: string;
};

export type AdminLoginResponse = {
  user: AdminSessionUser;
  token: string;
  accessToken?: string;
  refreshToken?: string;
  tokenType?: string;
  expiresInMinutes?: number;
};

const TOKEN_KEY = 'om_admin_token';
const REFRESH_TOKEN_KEY = 'om_admin_refresh_token';
const USER_KEY = 'om_admin_user';
const REMEMBER_KEY = 'om_admin_remember';
const EXPIRES_AT_KEY = 'om_admin_token_expires_at';


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

function isExpired(expiresAt: string) {
  const value = Number(expiresAt || 0);
  return Boolean(value && Date.now() >= value);
}

function notifyAuthExpired() {
  try { window.dispatchEvent(new CustomEvent('om-admin-auth-expired')); } catch {}
}

const safeLocal = {
  get(key: string) { try { return window.localStorage.getItem(key) ?? ''; } catch { return ''; } },
  set(key: string, value: string) { try { window.localStorage.setItem(key, value); } catch {} },
  remove(key: string) { try { window.localStorage.removeItem(key); } catch {} },
};

const safeSession = {
  get(key: string) { try { return window.sessionStorage.getItem(key) ?? ''; } catch { return ''; } },
  set(key: string, value: string) { try { window.sessionStorage.setItem(key, value); } catch {} },
  remove(key: string) { try { window.sessionStorage.removeItem(key); } catch {} },
};

export const authStore = {
  getToken() {
    const localToken = safeLocal.get(TOKEN_KEY);
    const sessionToken = safeSession.get(TOKEN_KEY);
    const token = localToken || sessionToken;
    const expiresAt = localToken ? safeLocal.get(EXPIRES_AT_KEY) : safeSession.get(EXPIRES_AT_KEY);
    if (token && isExpired(expiresAt)) {
      authStore.clear();
      notifyAuthExpired();
      return '';
    }
    return token;
  },
  isAuthenticated() {
    return Boolean(authStore.getToken() && authStore.getUser());
  },
  getRefreshToken() {
    return safeLocal.get(REFRESH_TOKEN_KEY) || safeSession.get(REFRESH_TOKEN_KEY);
  },
  getUser(): AdminSessionUser | null {
    const raw = safeLocal.get(USER_KEY) || safeSession.get(USER_KEY);
    if (!raw) return null;
    try { return JSON.parse(raw) as AdminSessionUser; } catch { return null; }
  },
  save(session: AdminLoginResponse, remember: boolean) {
    const token = session.token || session.accessToken || '';
    const refreshToken = session.refreshToken || '';
    const target = remember ? safeLocal : safeSession;
    authStore.clear();
    target.set(TOKEN_KEY, token);
    const jwtExpiresAt = decodeJwtExpiresAt(token);
    const fallbackExpiresAt = session.expiresInMinutes ? Date.now() + session.expiresInMinutes * 60 * 1000 : null;
    const expiresAt = jwtExpiresAt || fallbackExpiresAt;
    if (expiresAt) target.set(EXPIRES_AT_KEY, String(expiresAt));
    if (refreshToken) target.set(REFRESH_TOKEN_KEY, refreshToken);
    target.set(USER_KEY, JSON.stringify(session.user));
    target.set(REMEMBER_KEY, remember ? '1' : '0');
  },
  clear() {
    [safeLocal, safeSession].forEach(store => {
      store.remove(TOKEN_KEY);
      store.remove(REFRESH_TOKEN_KEY);
      store.remove(USER_KEY);
      store.remove(REMEMBER_KEY);
      store.remove(EXPIRES_AT_KEY);
    });
  },
};

function buildHeaders(init?: RequestInit) {
  const token = authStore.getToken();
  return {
    'Content-Type': 'application/json',
    ...(token ? { Authorization: `Bearer ${token}` } : {}),
    ...(init?.headers ?? {}),
  };
}

async function parseResponse(response: Response) {
  const text = await response.text();
  if (!text) return null;
  try { return JSON.parse(text); } catch { return text; }
}

function extractData<T>(payload: any): T {
  return (payload?.data ?? payload) as T;
}

async function request<T>(path: string, init?: RequestInit, options?: { skipAuthExpired?: boolean }) {
  const token = authStore.getToken();
  if (!token && !options?.skipAuthExpired) {
    notifyAuthExpired();
    throw new Error('Session expired. Please sign in again.');
  }

  const response = await fetch(`${appConfig.apiBaseUrl}${path}`, {
    ...init,
    headers: buildHeaders(init),
  });

  const payload = await parseResponse(response);

  if (response.status === 401 && !options?.skipAuthExpired) {
    authStore.clear();
    notifyAuthExpired();
  }

  if (!response.ok || payload?.success === false) {
    const message = payload?.error?.message || payload?.message || payload?.title || `API error ${response.status}`;
    throw new Error(message);
  }

  return extractData<T>(payload);
}

async function requestForm<T>(path: string, form: FormData) {
  const token = authStore.getToken();
  if (!token) {
    notifyAuthExpired();
    throw new Error('Session expired. Please sign in again.');
  }

  const response = await fetch(`${appConfig.apiBaseUrl}${path}`, {
    method: 'POST',
    headers: token ? { Authorization: `Bearer ${token}` } : undefined,
    body: form,
  });

  const payload = await parseResponse(response);
  if (response.status === 401) {
    authStore.clear();
    notifyAuthExpired();
  }
  if (!response.ok || payload?.success === false) {
    const message = payload?.error?.message || payload?.message || payload?.title || `API error ${response.status}`;
    throw new Error(message);
  }
  return extractData<T>(payload);
}

function normalizeLoginResponse(payload: any): AdminLoginResponse {
  const data = payload?.data ?? payload;
  const token = data?.token || data?.accessToken || data?.jwt || data?.jwtToken || data?.bearerToken || '';
  const user = data?.user || data?.admin || data?.profile || data?.account;

  if (!token) throw new Error('Admin API did not return a bearer token.');
  if (!user) throw new Error('Admin API did not return an admin user profile.');

  const normalizedUser: AdminSessionUser = {
    id: String(user.id || user.userId || user.sub || ''),
    name: String(user.name || user.fullName || user.displayName || user.email || 'Admin'),
    email: String(user.email || ''),
    role: String(user.role || user.userRole || 'Admin'),
    source: user.source || user.userSource,
    avatarUrl: user.avatarUrl || user.photoUrl,
  };

  return {
    ...data,
    token,
    accessToken: data?.accessToken || token,
    refreshToken: data?.refreshToken || data?.refresh || '',
    user: normalizedUser,
  };
}

export const apiClient = {
  get: <T,>(path: string) => request<T>(path),
  post: <T,>(path: string, body: unknown) => request<T>(path, { method: 'POST', body: JSON.stringify(body) }),
  put: <T,>(path: string, body: unknown) => request<T>(path, { method: 'PUT', body: JSON.stringify(body) }),
  delete: <T,>(path: string) => request<T>(path, { method: 'DELETE' }),
  uploadMedia: <T,>(file: File) => {
    const form = new FormData();
    form.append('file', file);
    return requestForm<T>('/media/upload', form);
  },
  login: async (email: string, password: string, rememberMe: boolean) => {
    const endpoints = [
      '/auth/admin-login',
      '/admin/auth/login',
      '/auth/login/admin',
      '/admin/login',
    ];
    const errors: string[] = [];

    for (const endpoint of endpoints) {
      try {
        const response = await fetch(`${appConfig.apiBaseUrl}${endpoint}`, {
          method: 'POST',
          headers: { 'Content-Type': 'application/json' },
          body: JSON.stringify({ email, password, rememberMe }),
        });
        const payload = await parseResponse(response);
        if (!response.ok || payload?.success === false) {
          errors.push(payload?.error?.message || payload?.message || `${endpoint}: ${response.status}`);
          if (response.status !== 404 && response.status !== 405) break;
          continue;
        }
        const session = normalizeLoginResponse(payload);
        authStore.save(session, rememberMe);
        return session;
      } catch (error) {
        errors.push(error instanceof Error ? error.message : String(error));
      }
    }

    throw new Error(errors.find(Boolean) || 'Login failed. Please check admin email and password.');
  },
  logout: () => authStore.clear(),
};
