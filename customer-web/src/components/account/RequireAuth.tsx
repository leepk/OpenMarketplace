'use client';

import Link from 'next/link';
import { useEffect, useState } from 'react';
import { getSessionUser, type SessionUser } from '@/lib/api/session';
import { Icon } from '@/components/ui/Icon';
import { useI18n } from '@/lib/i18n/client';

export function useSessionUser() {
  const [user, setUser] = useState<SessionUser | null>(null);
  const [ready, setReady] = useState(false);
  useEffect(() => {
    const sync = () => { setUser(getSessionUser()); setReady(true); };
    sync();
    window.addEventListener('storage', sync);
    window.addEventListener('om-session-changed', sync);
    return () => { window.removeEventListener('storage', sync); window.removeEventListener('om-session-changed', sync); };
  }, []);
  return { user, ready };
}

export function AuthEmpty({ title, text }: { title: string; text: string }) {
  const { t } = useI18n();
  return (
    <section className="account-page-v3 shell-wide">
      <div className="account-auth-empty">
        <span><Icon name="shield" size={34} /></span>
        <h1>{title}</h1>
        <p>{text}</p>
        <div><Link className="primary-button" href="/login">{t('login')}</Link><Link className="secondary-button" href="/register">{t('createAccount')}</Link></div>
      </div>
    </section>
  );
}
