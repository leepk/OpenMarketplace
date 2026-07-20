import type { AdminTab } from './types';

export const adminRoutes: Record<AdminTab, string> = {
  dashboard: '/dashboard',
  pending: '/listing-review',
  listings: '/listings',
  categories: '/categories',
  users: '/users',
  messages: '/messages',
  notifications: '/notifications',
  banners: '/ads',
  payments: '/payments',
  reports: '/reports',
  settings: '/settings',
  siteSettings: '/site-settings',
  blockedWords: '/blocked-words',
  localities: '/localities',
  health: '/system',
};

export const loginRoute = '/login';
const routeEntries = Object.entries(adminRoutes) as Array<[AdminTab, string]>;

export function getTabPath(tab: AdminTab) {
  return adminRoutes[tab] ?? adminRoutes.dashboard;
}

export function normalizePath(pathname: string) {
  const cleaned = pathname.split('?')[0].split('#')[0].replace(/\/+$/, '');
  return cleaned || '/';
}

export function getCurrentPath() {
  const path = normalizePath(window.location.pathname);
  return `${path}${window.location.search || ''}${window.location.hash || ''}`;
}

export function getTabFromPath(pathname: string): AdminTab {
  const normalized = normalizePath(pathname);

  if (normalized === '/' || normalized === '') return 'dashboard';
  if (normalized === loginRoute) return 'dashboard';
  if (normalized.startsWith('/listing-review')) return 'pending';
  if (normalized.startsWith('/listings')) return 'listings';
  if (normalized.startsWith('/categories')) return 'categories';
  if (normalized.startsWith('/users')) return 'users';
  if (normalized.startsWith('/messages')) return 'messages';
  if (normalized.startsWith('/notifications')) return 'notifications';
  if (normalized.startsWith('/ads')) return 'banners';
  if (normalized.startsWith('/payments')) return 'payments';
  if (normalized.startsWith('/reports')) return 'reports';
  if (normalized.startsWith('/site-settings')) return 'siteSettings';
  if (normalized.startsWith('/blocked-words')) return 'blockedWords';
  if (normalized.startsWith('/localities')) return 'localities';
  if (normalized.startsWith('/settings')) return 'settings';
  if (normalized.startsWith('/system')) return 'health';

  const exact = routeEntries.find(([, path]) => path === normalized);
  return exact?.[0] ?? 'dashboard';
}

export function navigateTo(path: string, replace = false) {
  const target = path || adminRoutes.dashboard;
  const targetPath = normalizePath(target);
  const currentPath = normalizePath(window.location.pathname);
  if (currentPath === targetPath && (window.location.search || '') === '') return;
  if (replace) window.history.replaceState({}, '', target);
  else window.history.pushState({}, '', target);
  window.dispatchEvent(new PopStateEvent('popstate'));
}
