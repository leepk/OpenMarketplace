'use client';

import { useI18n } from '@/lib/i18n/client';

export function CategoryName({ name }: { name?: string | null }) {
  const { category } = useI18n();
  return <>{category(name ?? '')}</>;
}
