import { useEffect, useState } from 'react';
import { AdminShell } from './components/layout/AdminShell';
import { LoginPage } from './pages/LoginPage';
import { authStore, type AdminSessionUser } from './lib/api/apiClient';
import { adminRoutes, getCurrentPath, loginRoute, navigateTo, normalizePath } from './routes';
import type { Theme } from './types';

export function App() {
  const [theme, setTheme] = useState<Theme>(() => {
    try {
      return (localStorage.getItem('om_admin_theme') as Theme) || 'light';
    } catch {
      return 'light';
    }
  });

  const [session, setSession] = useState<{ token: string; user: AdminSessionUser } | null>(() => {
    try {
      const token = authStore.getToken();
      const user = authStore.getUser();
      return token && user ? { token, user } : null;
    } catch {
      return null;
    }
  });

  useEffect(() => {
    document.documentElement.dataset.theme = theme;
    try {
      localStorage.setItem('om_admin_theme', theme);
    } catch {}
  }, [theme]);

  useEffect(() => {
    const clear = () => {
      authStore.clear();
      setSession(null);
      navigateTo(loginRoute, true);
    };
    const checkSession = () => {
      if (authStore.getUser() && !authStore.getToken()) clear();
    };
    window.addEventListener('om-admin-auth-expired', clear);
    const timer = window.setInterval(checkSession, 30_000);
    return () => {
      window.removeEventListener('om-admin-auth-expired', clear);
      window.clearInterval(timer);
    };
  }, []);

  useEffect(() => {
    const path = normalizePath(window.location.pathname);
    if (!session && path !== loginRoute) {
      try { sessionStorage.setItem('om_admin_return_to', getCurrentPath()); } catch {}
      navigateTo(loginRoute, true);
    }
    if (session && (path === loginRoute || path === '/')) {
      let returnTo = '';
      try {
        returnTo = sessionStorage.getItem('om_admin_return_to') || '';
        sessionStorage.removeItem('om_admin_return_to');
      } catch {}
      navigateTo(returnTo && normalizePath(returnTo) !== loginRoute ? returnTo : adminRoutes.dashboard, true);
    }
  }, [session]);

  function handleLogin(nextSession: { token: string; user: AdminSessionUser }) {
    setSession(nextSession);
    let returnTo = '';
    try {
      returnTo = sessionStorage.getItem('om_admin_return_to') || '';
      sessionStorage.removeItem('om_admin_return_to');
    } catch {}
    navigateTo(returnTo && normalizePath(returnTo) !== loginRoute ? returnTo : adminRoutes.dashboard, true);
  }

  function handleLogout() {
    setSession(null);
    navigateTo(loginRoute, true);
  }

  if (!session) return <LoginPage onLogin={handleLogin} theme={theme} setTheme={setTheme} />;

  return <AdminShell user={session.user} theme={theme} setTheme={setTheme} onLogout={handleLogout} />;
}
