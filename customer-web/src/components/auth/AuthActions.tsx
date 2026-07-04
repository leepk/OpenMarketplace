'use client';

import Link from 'next/link';
import { useEffect, useState } from 'react';
import { useI18n } from '@/lib/i18n/client';

export function AuthActions() {
  const { t } = useI18n();
  const [isAuthed, setIsAuthed] = useState(false);
  useEffect(() => { setIsAuthed(Boolean(localStorage.getItem('om_token') || localStorage.getItem('om_user'))); }, []);

  if (!isAuthed) {
    return <div className="auth-actions"><Link className="login-link" href="/login">{t('login')}</Link><Link className="create-link" href="/register">{t('createAccount')}</Link></div>;
  }

  return (
    <div className="user-menu">
      <Link href="/profile" className="avatar-mini">DW</Link>
      <button type="button" onClick={() => { localStorage.removeItem('om_token'); localStorage.removeItem('om_user'); setIsAuthed(false); }}>{t('logout')}</button>
    </div>
  );
}
