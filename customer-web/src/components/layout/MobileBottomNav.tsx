'use client';

import Link from 'next/link';
import { useI18n } from '@/lib/i18n/client';

export function MobileBottomNav() {
  const { t } = useI18n();
  return (
    <nav className="mobile-bottom-nav" aria-label="Mobile navigation">
      <Link href="/"><span>⌂</span>{t('home')}</Link>
      <Link href="/favorites"><span>♡</span>{t('savedListings')}</Link>
      <Link href="/post"><span>＋</span>{t('postListing')}</Link>
      <Link href="/messages"><span>✉</span>{t('messages')}</Link>
      <Link href="/profile"><span>♙</span>{t('account')}</Link>
    </nav>
  );
}
