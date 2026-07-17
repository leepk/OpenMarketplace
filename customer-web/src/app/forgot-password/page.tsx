'use client';
import Link from 'next/link';
import { useState } from 'react';
import { apiClient } from '@/lib/api/apiClient';
import { useI18n } from '@/lib/i18n/client';

export default function ForgotPasswordPage() {
  const { t } = useI18n();
  const [busy, setBusy] = useState(false);
  const [message, setMessage] = useState('');
  async function submit(e: React.FormEvent<HTMLFormElement>) {
    e.preventDefault(); setBusy(true); setMessage('');
    const email = String(new FormData(e.currentTarget).get('email') || '').trim().toLowerCase();
    try { await apiClient.post('/auth/forgot-password', { email }); setMessage(t('resetRequestSent')); }
    catch (err: any) { setMessage(err.message || t('resetRequestSent')); }
    finally { setBusy(false); }
  }
  return <main className="auth-page-v2"><section className="auth-form-wrap-v2"><div className="auth-card-v2">
    <h1>{t('forgotPasswordTitle')}</h1><p className="auth-muted">{t('forgotPasswordText')}</p>
    <form onSubmit={submit}><label>{t('email')}<input name="email" type="email" required autoComplete="email" placeholder={t('emailPlaceholder')} /></label>
      <button className="auth-primary-v2" disabled={busy}>{busy ? t('sending') : t('sendResetLink')}</button></form>
    {message && <p className="form-success">{message}</p>}<p className="auth-switch-v2"><Link href="/login">{t('backToLogin')}</Link></p>
  </div></section></main>;
}
