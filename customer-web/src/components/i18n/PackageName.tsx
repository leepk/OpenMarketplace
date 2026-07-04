'use client';

import { useI18n } from '@/lib/i18n/client';

export function PackageName({ code }: { code?: string | null }) {
  const { packageLabel } = useI18n();
  return <>{packageLabel(code ?? 'FREE')}</>;
}
