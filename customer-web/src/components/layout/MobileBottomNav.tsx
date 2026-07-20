'use client';

import Link from 'next/link';
import { usePathname } from 'next/navigation';
import { Icon, type IconName } from '@/components/ui/Icon';
import { useI18n } from '@/lib/i18n/client';

type NavItem = {
  href: string;
  labelKey: 'home' | 'savedListings' | 'postListing' | 'messages' | 'account';
  icon: IconName;
  isPrimary?: boolean;
  matches?: (pathname: string) => boolean;
};

const items: NavItem[] = [
  { href: '/', labelKey: 'home', icon: 'home', matches: (pathname) => pathname === '/' },
  { href: '/favorites', labelKey: 'savedListings', icon: 'heart' },
  { href: '/post', labelKey: 'postListing', icon: 'plus', isPrimary: true },
  { href: '/messages', labelKey: 'messages', icon: 'mail' },
  {
    href: '/profile',
    labelKey: 'account',
    icon: 'user',
    matches: (pathname) => pathname === '/profile' || pathname.startsWith('/profile/') || pathname.startsWith('/my-listings') || pathname.startsWith('/billing') || pathname.startsWith('/notifications'),
  },
];

export function MobileBottomNav() {
  const pathname = usePathname();
  const { t } = useI18n();

  return (
    <nav className="mobile-bottom-nav" aria-label={t('mobileNavigation')}>
      {items.map((item) => {
        const active = item.matches ? item.matches(pathname) : pathname === item.href || pathname.startsWith(`${item.href}/`);
        return (
          <Link
            key={item.href}
            href={item.href}
            className={`${active ? 'is-active' : ''}${item.isPrimary ? ' is-primary' : ''}`.trim()}
            aria-current={active ? 'page' : undefined}
            aria-label={t(item.labelKey)}
          >
            <span className={`mobile-nav-icon${item.isPrimary ? ' mobile-nav-primary-icon' : ''}`} aria-hidden="true">
              {item.isPrimary ? <span className="mobile-nav-plus" /> : <Icon name={item.icon} size={23} />}
            </span>
            {!item.isPrimary && <span className="mobile-nav-label">{t(item.labelKey)}</span>}
          </Link>
        );
      })}
    </nav>
  );
}
