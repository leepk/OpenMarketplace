'use client';

import Link from 'next/link';
import { Icon } from '@/components/ui/Icon';
import { HeaderUserMenu } from '@/components/layout/HeaderUserMenu';
import { LanguageSwitcher } from '@/components/layout/LanguageSwitcher';
import { useI18n } from '@/lib/i18n/client';
import { resolveSiteImage, useSiteSettings } from '@/lib/site-settings';

export function SiteHeader() {
  const normalizeSearch = (event: React.FormEvent<HTMLFormElement>) => {
    const input = event.currentTarget.elements.namedItem('q') as HTMLInputElement | null;
    if (input) input.value = input.value.trim().replace(/\s+/g, ' ');
  };
  const { t } = useI18n();
  const site = useSiteSettings();
  const logo = resolveSiteImage(site.logoUrl);
  const siteName = site.siteName || t('appName');
  return (
    <header className="app-header">
      <div className="app-header-inner shell-wide">
        <Link href="/" className="logo-lockup" aria-label={`${siteName} ${t('home')}`}>
          <span className="logo-pin">{logo ? <img src={logo} alt="" /> : <Icon name="logo" size={25} />}</span>
          <span>{siteName}</span>
        </Link>
        <form className="global-search" action="/search" onSubmit={normalizeSearch}>
          <Icon name="search" size={18} />
          <input name="q" placeholder={t('searchPlaceholder')} />
        </form>
        <select className="header-control" defaultValue="all" aria-label={t('category')}>
          <option value="all">{t('allCategories')}</option>
          <option value="vehicles">{t('vehicles')}</option>
          <option value="property-rentals">{t('propertyRentals')}</option>
          <option value="for-sale">{t('forSale')}</option>
          <option value="jobs">{t('jobs')}</option>
          <option value="services">{t('services')}</option>
          <option value="electronics">{t('electronics')}</option>
        </select>
        <LanguageSwitcher />
        <Link href="/post" className="post-listing"><Icon name="plus" size={20} /> {t('postListing')}</Link>
        <HeaderUserMenu />
      </div>
    </header>
  );
}
