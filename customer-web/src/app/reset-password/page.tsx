'use client';
import Link from 'next/link';
import { Suspense, useState } from 'react';
import { useSearchParams } from 'next/navigation';
import { apiClient } from '@/lib/api/apiClient';
import { useI18n } from '@/lib/i18n/client';

function ResetPasswordForm() {
  const { t } = useI18n();
  const params = useSearchParams();
  const token = params.get('token') || '';
  const email = params.get('email') || '';
  const [busy, setBusy] = useState(false);
  const [message, setMessage] = useState('');
  const [ok, setOk] = useState(false);

  async function submit(e: React.FormEvent<HTMLFormElement>) {
    e.preventDefault();
    const f = new FormData(e.currentTarget);
    const password = String(f.get('password') || '');
    const confirm = String(f.get('confirm') || '');
    if (password !== confirm) { setMessage(t('passwordMismatch')); return; }
    setBusy(true); setMessage('');
    try {
      await apiClient.post('/auth/reset-password', { email, token, newPassword: password });
      setOk(true); setMessage(t('passwordResetSuccess'));
    } catch (err: any) { setMessage(err.message || t('invalidResetLink')); }
    finally { setBusy(false); }
  }

  if (!token || !email) return <div className="auth-card-v2"><h1>{t('resetPasswordTitle')}</h1><p className="form-error">{t('invalidResetLink')}</p><Link href="/forgot-password">{t('forgotPasswordTitle')}</Link></div>;

  return <div className="auth-card-v2"><h1>{t('resetPasswordTitle')}</h1>
    {!ok && <form onSubmit={submit}>
      <label>{t('newPassword')}<input name="password" type="password" minLength={6} required autoComplete="new-password" /></label>
      <label>{t('confirmPassword')}<input name="confirm" type="password" minLength={6} required autoComplete="new-password" /></label>
      <button className="auth-primary-v2" disabled={busy}>{busy ? t('sending') : t('resetPassword')}</button>
    </form>}
    {message && <p className={ok ? 'form-success' : 'form-error'}>{message}</p>}
    <p className="auth-switch-v2"><Link href="/login">{t('backToLogin')}</Link></p>
  </div>;
}

export default function ResetPasswordPage() {
  return <main className="auth-page-v2"><section className="auth-form-wrap-v2"><Suspense fallback={null}><ResetPasswordForm /></Suspense></section></main>;
}
