import { appConfig } from '@/lib/config';

export function mediaUrl(url?: string | null): string | null {
  if (!url) return null;
  if (/^https?:\/\//i.test(url) || url.startsWith('data:') || url.startsWith('blob:')) return url;
  const apiRoot = appConfig.apiBaseUrl.replace(/\/api\/v\d+\/?$/, '').replace(/\/$/, '');
  if (url.startsWith('/')) return `${apiRoot}${url}`;
  return `${apiRoot}/${url}`;
}
