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
    const name = String(f.get('name') ?? '').trim();
    const email = String(f.get('email') ?? '').trim().toLowerCase();
    const password = String(f.get('password') ?? '');
    const confirmPassword = String(f.get('confirmPassword') ?? '');
    if (!name) { setMsg(t('fullNameRequired')); setBusy(false); return; }
    if (!email || !email.includes('@')) { setMsg(t('validEmailRequired')); setBusy(false); return; }
    if (password.length < 6) { setMsg(t('passwordMin')); setBusy(false); return; }
    if (password !== confirmPassword) { setMsg(t('passwordsNoMatch')); setBusy(false); return; }
    try {
      const r: any = await apiClient.post('/auth/register', { name, email, phone: f.get('phone'), location: f.get('location'), password });
      saveSession(r);
      window.location.href = '/profile';
    } catch (err: any) { setMsg(err.message ?? t('registerFailed')); }
    finally { setBusy(false); }
  }

  return (
    <main className="auth-page-v2 register-page-v2">
      <section className="auth-panel-v2 register-panel-v2">
        <div className="auth-brand-v2"><span><Icon name="logo" size={24}/></span><strong>{t('appName')}</strong></div>
        <div className="auth-visual-v2"><div className="auth-post-card"><b>{t('postListing')}</b><span>{t('sellFasterText')}</span></div><div className="auth-badge-card"><b>4.9★</b><span>{t('sellerProfileTitle')}</span></div></div>
        <h1>{t('registerTitle')}</h1>
        <p>{t('registerSubtitle')}</p>
        <div className="auth-benefit-grid"><span><Icon name="plus" size={17}/> {t('postListing')}</span><span><Icon name="community" size={17}/> {t('buyerMessages')}</span><span><Icon name="star" size={17}/> {t('sellerProfileTitle')}</span></div>
      </section>
      <section className="auth-form-wrap-v2">
        <form className="auth-card-v2" onSubmit={submit} noValidate>
          <span className="auth-eyebrow">{t('createAccount')}</span>
          <h1>{t('registerTitle')}</h1>
          <p className="auth-muted">{t('registerSubtitle')}</p>
          <label>{t('fullName')} <b>*</b><input name="name" required placeholder={t('namePlaceholder')} autoComplete="name" /></label>
          <div className="auth-two-v2"><label>{t('email')} <b>*</b><input name="email" required type="email" placeholder="you@example.com" autoComplete="email" /></label><label>{t('phone')}<input name="phone" placeholder="(408) 555-1234" autoComplete="tel" /></label></div>
          <label>{t('location')}<input name="location" placeholder="San Jose, CA" defaultValue="San Jose, CA" /></label>
          <div className="auth-two-v2"><label>{t('password')} <b>*</b><input name="password" required minLength={6} type="password" placeholder={t('createPassword')} autoComplete="new-password" /></label><label>{t('confirmPassword')} <b>*</b><input name="confirmPassword" required minLength={6} type="password" placeholder={t('confirmPasswordPlaceholder')} autoComplete="new-password" /></label></div>
          <button className="auth-primary-v2" type="submit" disabled={busy}>{busy ? t('creating') : t('createAccount')}</button>
          <div className="auth-divider-v2"><span>{t('orContinueWith')}</span></div>
          <div className="auth-social-v2"><button type="button">Google</button><button type="button">Facebook</button></div>
          <p className="auth-switch-v2">{t('alreadyHaveAccount')} <Link href="/login">{t('login')}</Link></p>
          {msg && <p className="form-error">{msg}</p>}
        </form>
      </section>
    </main>
  );
}
