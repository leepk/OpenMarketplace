'use client';

import Link from 'next/link';
import { useEffect, useMemo, useState } from 'react';
import { Icon } from '@/components/ui/Icon';
import { CategoryBrowser } from '@/components/categories/CategoryBrowser';
import { marketplaceApi, type CategoryDto } from '@/lib/api/apiClient';
import { getSessionUser, type SessionUser } from '@/lib/api/session';
import { mediaUrl } from '@/lib/media/url';
import { useI18n } from '@/lib/i18n/client';

type Counts = { saved: number; messages: number; notifications: number };

export function CustomerAccountSidebar({ categories }: { categories: CategoryDto[] }) {
  const { t } = useI18n();
  const [user, setUser] = useState<SessionUser | null>(null);
  const [counts, setCounts] = useState<Counts>({ saved: 0, messages: 0, notifications: 0 });

  useEffect(() => {
    const sync = () => setUser(getSessionUser());
    sync();
    window.addEventListener('storage', sync);
    window.addEventListener('om-session-changed', sync);
    return () => { window.removeEventListener('storage', sync); window.removeEventListener('om-session-changed', sync); };
  }, []);

  useEffect(() => {
    if (!user?.id) { setCounts({ saved: 0, messages: 0, notifications: 0 }); return; }
    let cancelled = false;
    Promise.allSettled([marketplaceApi.favorites(user.id), marketplaceApi.conversations(user.id), marketplaceApi.notifications(user.id)]).then(([fav,msg,not]) => {
      if (cancelled) return;
      setCounts({
        saved: fav.status === 'fulfilled' ? fav.value.totalItems ?? fav.value.items?.length ?? 0 : 0,
        messages: msg.status === 'fulfilled' ? msg.value.items?.length ?? 0 : 0,
        notifications: not.status === 'fulfilled' ? not.value.unread ?? 0 : 0,
      });
    });
    return () => { cancelled = true; };
  }, [user?.id]);

  const initials = useMemo(() => (user?.name ?? 'Guest').split(' ').map(x => x[0]).slice(0, 2).join('').toUpperCase(), [user?.name]);
  const avatarSrc = mediaUrl(user?.avatarUrl);

  return (
    <aside className="left-rail">
      <section className="profile-card-mini">
        <div className="profile-avatar-modern">{user && avatarSrc ? <img src={avatarSrc} alt={user.name} /> : (user ? initials : <Icon name="user" size={24} />)}</div>
        <div><strong>{user?.name ?? t('guest')}</strong><span>{user?.location ?? t('loginToSync')}</span>{user ? <Link href="/profile">{t('viewProfile')}</Link> : <Link href="/login">{t('loginCreate')}</Link>}</div>
      </section>
      <nav className="account-nav">
        <Link className="active" href="/"><Icon name="home" /> {t('home')}</Link>
        <Link href={user ? '/my-listings' : '/login'}><Icon name="list" /> {t('myListings')}</Link>
        <Link href={user ? '/favorites' : '/login'}><Icon name="heart" /> {t('savedListings')} {user && counts.saved > 0 ? <b>{counts.saved}</b> : null}</Link>
        <Link href={user ? '/messages' : '/login'}><Icon name="message" /> {t('messages')} {user && counts.messages > 0 ? <b>{counts.messages}</b> : null}</Link>
        <Link href={user ? '/notifications' : '/login'}><Icon name="bell" /> {t('notifications')} {user && counts.notifications > 0 ? <b className="danger-count">{counts.notifications}</b> : null}</Link>
      </nav>
      <div className="rail-section-title">{t('browseCategories')}</div>
      <CategoryBrowser categories={categories} compact />
      <Link href="/search" className="view-all-cats">{t('viewAllCategories')}</Link>
      <section className="sell-faster-card"><strong>{t('sellFaster')}</strong><p>{t('sellFasterText')}</p><span>🚀</span></section>
    </aside>
  );
}
