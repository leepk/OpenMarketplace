'use client';

import Link from 'next/link';
import { Icon } from '@/components/ui/Icon';
import { useI18n } from '@/lib/i18n/client';

export function SiteFooter() {
  const { t } = useI18n();
  return (
    <footer className="site-footer-modern">
      <div className="shell-wide footer-modern-grid">
        <div className="footer-brand-modern">
          <Link href="/" className="footer-logo-modern"><span><Icon name="logo" size={18} /></span>OpenMarketplace</Link>
          <p>{t('footerText')}</p>
          <div className="footer-socials-modern"><a>f</a><a>x</a><a>in</a><a>ig</a></div>
        </div>
        <div><h4>{t('marketplace')}</h4><Link href="/search">{t('browseListings')}</Link><Link href="/post">{t('postListing')}</Link><Link href="/map">{t('mapSearch')}</Link><Link href="/favorites">{t('savedListings')}</Link></div>
        <div><h4>{t('account')}</h4><Link href="/login">{t('login')}</Link><Link href="/register">{t('createAccount')}</Link><Link href="/messages">{t('messages')}</Link><Link href="/notifications">{t('notifications')}</Link></div>
        <div><h4>{t('trustSafety')}</h4><Link href="/search?category=services">{t('verifiedSellers')}</Link><Link href="/billing">{t('promoteSafely')}</Link><Link href="/profile">{t('sellerProfile')}</Link><Link href="/search">{t('reportListing')}</Link></div>
      </div>
      <div className="shell-wide footer-modern-bottom"><span>© {new Date().getFullYear()} OpenMarketplace</span><span>{t('privacyLine')}</span></div>
    </footer>
  );
}
