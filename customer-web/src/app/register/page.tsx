'use client';
import Link from 'next/link';
import { useEffect, useMemo, useState } from 'react';
import { Icon } from '@/components/ui/Icon';
import { apiClient } from '@/lib/api/apiClient';
import { saveSession } from '@/lib/api/session';
import { appConfig } from '@/lib/config';
import { useI18n } from '@/lib/i18n/client';

export default function Page() {
  const { t } = useI18n();
  const [msg, setMsg] = useState('');
  const [busy, setBusy] = useState(false);
  const [password, setPassword] = useState('');
  const [showPassword, setShowPassword] = useState(false);
  const [providers, setProviders] = useState<any>({ google: { enabled: false }, facebook: { enabled: false } });

  useEffect(() => { apiClient.get('/auth/providers').then(setProviders).catch(() => undefined); }, []);

  const passwordStrength = useMemo(() => {
    let score = 0;
    if (password.length >= 6) score += 1;
    if (password.length >= 10) score += 1;
    if (/[A-Z]/.test(password) && /[a-z]/.test(password)) score += 1;
    if (/\d/.test(password)) score += 1;
    if (/[^A-Za-z0-9]/.test(password)) score += 1;
    return Math.min(score, 4);
  }, [password]);

  async function submit(e: React.FormEvent<HTMLFormElement>) {
    e.preventDefault();
    setBusy(true); setMsg('');
    const f = new FormData(e.currentTarget);
    const firstName = String(f.get('firstName') ?? '').trim();
    const lastName = String(f.get('lastName') ?? '').trim();
    const name = `${firstName} ${lastName}`.trim();
    const email = String(f.get('email') ?? '').trim().toLowerCase();
    const passwordValue = String(f.get('password') ?? '');
    const confirmPassword = String(f.get('confirmPassword') ?? '');
    const agreed = f.get('agreeTerms') === 'on';
    if (!firstName) { setMsg(t('firstNameRequired')); setBusy(false); return; }
    if (!lastName) { setMsg(t('lastNameRequired')); setBusy(false); return; }
    if (!email || !email.includes('@')) { setMsg(t('validEmailRequired')); setBusy(false); return; }
    if (passwordValue.length < 6) { setMsg(t('passwordMin')); setBusy(false); return; }
    if (passwordValue !== confirmPassword) { setMsg(t('passwordsNoMatch')); setBusy(false); return; }
    if (!agreed) { setMsg(t('agreeTermsRequired')); setBusy(false); return; }
    try {
      const r: any = await apiClient.post('/auth/register', {
        firstName,
        lastName,
        name,
        email,
        password: passwordValue,
        role: 'Customer',
        source: 'WebCustomer'
      });
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
          <span className="auth-eyebrow">{t('customerAccount')}</span>
          <h1>{t('registerTitle')}</h1>
          <p className="auth-muted">{t('registerNoLocationHint')}</p>

          <div className="auth-two-v2">
            <label>{t('firstName')} <b>*</b><input name="firstName" required placeholder={t('firstNamePlaceholder')} autoComplete="given-name" /></label>
            <label>{t('lastName')} <b>*</b><input name="lastName" required placeholder={t('lastNamePlaceholder')} autoComplete="family-name" /></label>
          </div>

          <label>{t('email')} <b>*</b><input name="email" required type="email" placeholder={t('emailPlaceholder')} autoComplete="email" /></label>

          <div className="auth-two-v2">
            <label>{t('password')} <b>*</b>
              <div className="auth-password-wrap">
                <input name="password" required minLength={6} type={showPassword ? 'text' : 'password'} placeholder={t('createPassword')} autoComplete="new-password" value={password} onChange={(e) => setPassword(e.target.value)} />
                <button type="button" onClick={() => setShowPassword(v => !v)}>{showPassword ? t('hide') : t('show')}</button>
              </div>
            </label>
            <label>{t('confirmPassword')} <b>*</b><input name="confirmPassword" required minLength={6} type={showPassword ? 'text' : 'password'} placeholder={t('confirmPasswordPlaceholder')} autoComplete="new-password" /></label>
          </div>

          <div className="password-meter" aria-label={t('passwordStrength')}>
            <span className={passwordStrength >= 1 ? 'on' : ''}></span>
            <span className={passwordStrength >= 2 ? 'on' : ''}></span>
            <span className={passwordStrength >= 3 ? 'on' : ''}></span>
            <span className={passwordStrength >= 4 ? 'on' : ''}></span>
          </div>
          <p className="auth-muted small">{t('locationAskedWhenPosting')}</p>

          <label className="auth-check-v2"><input name="agreeTerms" type="checkbox" /> <span>{t('agreeTermsPrefix')} <Link href="/terms">{t('terms')}</Link> {t('and')} <Link href="/privacy">{t('privacy')}</Link>.</span></label>

          <button className="auth-primary-v2" type="submit" disabled={busy}>{busy ? t('creating') : t('createAccount')}</button>
          <div className="auth-divider-v2"><span>{t('orContinueWith')}</span></div>
          <div className="auth-social-v2">{providers.google?.enabled && <a href={`${appConfig.apiBaseUrl}/auth/external/google?returnUrl=${encodeURIComponent('/profile')}`}>Google</a>}{providers.facebook?.enabled && <a href={`${appConfig.apiBaseUrl}/auth/external/facebook?returnUrl=${encodeURIComponent('/profile')}`}>Facebook</a>}</div>
          <p className="auth-switch-v2">{t('alreadyHaveAccount')} <Link href="/login">{t('login')}</Link></p>
          {msg && <p className="form-error">{msg}</p>}
        </form>
      </section>
    </main>
  );
}
