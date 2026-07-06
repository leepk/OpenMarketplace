'use client';

import Link from 'next/link';
import { useEffect, useState } from 'react';
import { useI18n } from '@/lib/i18n/client';
import { clearSession, getSessionToken, getSessionUser } from '@/lib/api/session';

export function AuthActions() {
  const { t } = useI18n();
  const [isAuthed, setIsAuthed] = useState(false);
  useEffect(() => {
    const sync = () => setIsAuthed(Boolean(getSessionToken() || getSessionUser()));
    sync();
    window.addEventListener('storage', sync);
    window.addEventListener('om-session-changed', sync);
    return () => { window.removeEventListener('storage', sync); window.removeEventListener('om-session-changed', sync); };
  }, []);

  if (!isAuthed) {
    return <div className="auth-actions"><Link className="login-link" href="/login">{t('login')}</Link><Link className="create-link" href="/register">{t('createAccount')}</Link></div>;
  }

  return (
    <div className="user-menu">
      <Link href="/profile" className="avatar-mini">DW</Link>
      <button type="button" onClick={() => { clearSession(); setIsAuthed(false); }}>{t('logout')}</button>
    </div>
  );
}
