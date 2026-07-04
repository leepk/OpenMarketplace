'use client';

import { useI18n } from '@/lib/i18n/client';

export function SponsoredInline() {
  const { t } = useI18n();
  return <article className="sponsored-inline"><div className="sponsor-thumb"/><div><span>{t('sponsoredCaps')}</span><h3>{t('findDreamHome')}</h3><p>{t('modernApartments')}</p></div><button type="button">{t('ad')}⌄</button></article>;
}
