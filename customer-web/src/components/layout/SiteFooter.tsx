'use client';

import Link from 'next/link';
import { Icon } from '@/components/ui/Icon';
import { useI18n } from '@/lib/i18n/client';
import { resolveSiteImage, useSiteSettings } from '@/lib/site-settings';

export function SiteFooter() {
  const { t } = useI18n();
  const site = useSiteSettings();
  const logo = resolveSiteImage(site.logoUrl);
  const siteName = site.siteName || t('appName');
  const socialLinks = [
    { label: 'Facebook', icon: 'f', href: site.facebookUrl },
    { label: 'YouTube', icon: 'yt', href: site.youtubeUrl },
    { label: 'Instagram', icon: 'ig', href: site.instagramUrl },
  ].filter(x => x.href);
  return (
    <footer className="site-footer-modern">
      <div className="shell-wide footer-modern-grid">
        <div className="footer-brand-modern">
          <Link href="/" className="footer-logo-modern"><span>{logo ? <img src={logo} alt="" /> : <Icon name="logo" size={18} />}</span>{siteName}</Link>
          <p>{site.footerText || t('footerText')}</p>
          {(site.contactEmail || site.contactPhone || site.contactAddress) && (
            <div className="footer-contact-modern">
              {site.contactEmail && <a href={`mailto:${site.contactEmail}`}>{site.contactEmail}</a>}
              {site.contactPhone && <a href={`tel:${site.contactPhone}`}>{site.contactPhone}</a>}
              {site.contactAddress && <span>{site.contactAddress}</span>}
            </div>
          )}
          <div className="footer-socials-modern">{socialLinks.map(link => <a key={link.label} href={link.href} target="_blank" rel="noreferrer" aria-label={link.label} title={link.label}><span aria-hidden="true">{link.icon}</span></a>)}</div>
        </div>
        <div><h4>{t('marketplace')}</h4><Link href="/search">{t('browseListings')}</Link><Link href="/post">{t('postListing')}</Link><Link href="/map">{t('mapSearch')}</Link><Link href="/favorites">{t('savedListings')}</Link></div>
        <div><h4>{t('account')}</h4><Link href="/login">{t('login')}</Link><Link href="/register">{t('createAccount')}</Link><Link href="/messages">{t('messages')}</Link><Link href="/notifications">{t('notifications')}</Link></div>
        <div><h4>{t('trustSafety')}</h4><Link href="/search?category=services">{t('verifiedSellers')}</Link><Link href="/billing">{t('promoteSafely')}</Link><Link href="/profile">{t('sellerProfile')}</Link><Link href="/search">{t('reportListing')}</Link></div>
      </div>
      <div className="shell-wide footer-modern-bottom"><span>© {new Date().getFullYear()} {siteName}</span><span>{t('privacyLine')}</span></div>
    </footer>
  );
}
