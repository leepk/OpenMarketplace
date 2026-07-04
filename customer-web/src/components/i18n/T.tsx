'use client';

import { useI18n } from '@/lib/i18n/client';

export function T({ k, fallback }: { k: string; fallback?: string }) {
  const { t } = useI18n();
  return <>{t(k) || fallback || k}</>;
}
