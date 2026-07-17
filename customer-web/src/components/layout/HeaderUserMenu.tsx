'use client';

import Link from 'next/link';
import { useEffect, useMemo, useState } from 'react';
import { Icon } from '@/components/ui/Icon';
import { marketplaceApi } from '@/lib/api/apiClient';
import { clearSession, getSessionUser, type SessionUser } from '@/lib/api/session';
import { avatarUrl } from '@/lib/media/avatar';
import { useI18n } from '@/lib/i18n/client';

type Counts = { saved: number; messages: number; notifications: number };

export function HeaderUserMenu() {
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

  const initials = useMemo(() => (user?.name ?? 'Guest').split(' ').map(p => p[0]).join('').slice(0,2).toUpperCase(), [user?.name]);
  const avatarSrc = avatarUrl(user?.avatarUrl);
  const logout = () => { clearSession(); setUser(null); window.dispatchEvent(new Event('om-session-changed')); };

  if (!user) {
    return <div className="auth-links"><Link href="/login">{t('login')}</Link><Link href="/register" className="create-account">{t('createAccount')}</Link></div>;
  }

  return (
    <div className="header-user-actions">
      <Link className="round-action" href="/favorites" title={t('savedListings')}><Icon name="heart" size={20} />{counts.saved > 0 && <b>{counts.saved}</b>}</Link>
      <Link className="round-action" href="/messages" title={t('messages')}><Icon name="message" size={20} />{counts.messages > 0 && <b>{counts.messages}</b>}</Link>
      <Link className="round-action" href="/notifications" title={t('notifications')}><Icon name="bell" size={20} />{counts.notifications > 0 && <b className="red-dot">{counts.notifications}</b>}</Link>
      <Link href="/profile" className="avatar-chip"><img src={avatarSrc} alt={user.name} /></Link>
      <button className="logout-button" type="button" onClick={logout}>{t('logout')}</button>
    </div>
  );
}
