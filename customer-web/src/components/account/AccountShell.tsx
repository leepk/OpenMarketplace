'use client';

import Link from 'next/link';
import { usePathname } from 'next/navigation';
import { Icon } from '@/components/ui/Icon';
import type { SessionUser } from '@/lib/api/session';
import { useI18n } from '@/lib/i18n/client';

const nav = [
  { href: '/profile', icon: 'user', key: 'viewProfileTitle' },
  { href: '/my-listings', icon: 'list', key: 'myListings' },
  { href: '/favorites', icon: 'heart', key: 'savedListings' },
  { href: '/messages', icon: 'message', key: 'messages' },
  { href: '/notifications', icon: 'bell', key: 'notifications' },
  { href: '/billing', icon: 'card', key: 'paymentsInvoicesNav' },
];

export function AccountShell({ user, title, subtitle, action, children }: { user: SessionUser; title: string; subtitle: string; action?: React.ReactNode; children: React.ReactNode }) {
  const pathname = usePathname();
  const { t } = useI18n();
  const initials = user.name.split(' ').map(x => x[0]).join('').slice(0,2).toUpperCase();
  return (
    <section className="account-page-v3 shell-wide">
      <aside className="account-left-v3">
        <div className="account-profile-card-v3">
          <div className="account-avatar-v3">{initials}</div>
          <strong>{user.name}</strong>
          <span>{user.email ?? t('customerAccount')}</span>
          <Link href="/profile">{t('viewPublicProfile')}</Link>
        </div>
        <nav className="account-menu-v3">
          {nav.map((item) => <Link key={item.href} className={pathname === item.href ? 'active' : ''} href={item.href}><Icon name={item.icon as any} size={17}/><span>{t(item.key)}</span></Link>)}
        </nav>
        <div className="account-tip-v3"><b>{t('safeMarketplace')}</b><p>{t('safeMarketplaceText')}</p></div>
      </aside>
      <main className="account-main-v3">
        <div className="account-heading-v3"><div><span>{t('accountLabel')}</span><h1>{title}</h1><p>{subtitle}</p></div>{action}</div>
        {children}
      </main>
    </section>
  );
}
