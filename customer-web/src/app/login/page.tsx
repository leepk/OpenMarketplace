'use client';
import Link from 'next/link';
import { useState } from 'react';
import { Icon } from '@/components/ui/Icon';
import { apiClient } from '@/lib/api/apiClient';
import { saveSession } from '@/lib/api/session';
import { useI18n } from '@/lib/i18n/client';

export default function Page() {
  const { t } = useI18n();
  const [msg, setMsg] = useState('');
  const [busy, setBusy] = useState(false);

  async function submit(e: React.FormEvent<HTMLFormElement>) {
    e.preventDefault();
    setBusy(true); setMsg('');
    const f = new FormData(e.currentTarget);
    const email = String(f.get('email') ?? '').trim().toLowerCase();
    const password = String(f.get('password') ?? '');
    if (!email || !password) { setMsg(t('emailPasswordRequired')); setBusy(false); return; }
    try {
      const r: any = await apiClient.post('/auth/login', { email, password });
      saveSession(r);
      window.location.href = '/profile';
    } catch (err: any) { setMsg(err.message ?? t('loginFailed')); }
    finally { setBusy(false); }
  }

  return (
    <main className="auth-page-v2">
      <section className="auth-panel-v2">
        <div className="auth-brand-v2"><span><Icon name="logo" size={24}/></span><strong>{t('appName')}</strong></div>
        <div className="auth-visual-v2">
          <div className="auth-phone-card"><b>$950</b><span>iPhone 15 Pro Max</span></div>
          <div className="auth-house-card"><b>{t('verified')}</b><span>{t('verifiedSellers')}</span></div>
        </div>
        <h1>{t('loginTitle')}</h1>
        <p>{t('loginSubtitle')}</p>
        <div className="auth-benefit-grid"><span><Icon name="shield" size={17}/> {t('safeMarketplace')}</span><span><Icon name="message" size={17}/> {t('messages')}</span><span><Icon name="heart" size={17}/> {t('savedListings')}</span></div>
      </section>
      <section className="auth-form-wrap-v2">
        <form className="auth-card-v2" onSubmit={submit}>
          <span className="auth-eyebrow">{t('loginTitle')}</span>
          <h1>{t('login')}</h1>
          <p className="auth-muted">{t('loginSubtitle')}</p>
          <label>{t('email')} <b>*</b><input name="email" required type="email" placeholder="you@example.com" autoComplete="email" /></label>
          <label>{t('password')} <b>*</b><input name="password" required type="password" placeholder={t('enterPassword')} autoComplete="current-password" /></label>
          <div className="auth-row-between"><label className="check-label"><input type="checkbox"/> {t('rememberMe')}</label><Link href="/forgot-password">{t('forgot')}</Link></div>
          <button className="auth-primary-v2" type="submit" disabled={busy}>{busy ? t('loggingIn') : t('login')}</button>
          <div className="auth-divider-v2"><span>{t('orContinueWith')}</span></div>
          <div className="auth-social-v2"><button type="button">Google</button><button type="button">Facebook</button></div>
          <p className="auth-switch-v2">{t('noAccount')} <Link href="/register">{t('createAccount')}</Link></p>
          {msg && <p className="form-error">{msg}</p>}
        </form>
      </section>
    </main>
  );
}
