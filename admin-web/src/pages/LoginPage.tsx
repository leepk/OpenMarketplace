import { type FormEvent, useState } from 'react';
import { Icon } from '../components/common/Icon';
import { AdminButton, AdminCheckbox, AdminTextBox } from '../components/common/AdminControls';
import { apiClient, type AdminSessionUser } from '../lib/api/apiClient';
import { appConfig } from '../lib/config';
import type { Theme } from '../types';

export function LoginPage({ onLogin, theme, setTheme }: { onLogin: (s: { token: string; user: AdminSessionUser }) => void; theme: Theme; setTheme: (t: Theme) => void }) {
  const [email, setEmail] = useState(appConfig.defaultAdminEmail);
  const [password, setPassword] = useState(appConfig.defaultAdminPassword);
  const [showPassword, setShowPassword] = useState(false);
  const [remember, setRemember] = useState(true);
  const [loading, setLoading] = useState(false);
  const [err, setErr] = useState('');
  const hasDevDefault = Boolean(appConfig.defaultAdminEmail || appConfig.defaultAdminPassword);

  async function submit(e?: FormEvent) {
    e?.preventDefault();
    setLoading(true);
    setErr('');
    try {
      const result = await apiClient.login(email.trim(), password, remember);
      const allowedRoles = ['superadmin', 'admin', 'moderator', 'support', 'system'];
      if (!allowedRoles.includes(String(result.user.role || '').toLowerCase())) {
        apiClient.logout();
        throw new Error('Access denied. This portal is only for admin users.');
      }
      onLogin({ token: result.token || result.accessToken || '', user: result.user });
    } catch (e) {
      setErr((e as Error).message || 'Login failed');
    } finally {
      setLoading(false);
    }
  }

  return (
    <div className="login-shell centered-login-shell">
      <AdminButton className="login-theme-button" variant="icon" type="button" onClick={() => setTheme(theme === 'dark' ? 'light' : 'dark')} aria-label="Toggle theme">
        <Icon name={theme === 'dark' ? 'sun' : 'moon'} />
      </AdminButton>
      <section className="login-card centered-admin-login-card" aria-label="Admin login">
        <div className="login-brand centered-login-brand">
          <span>
            <Icon name="dashboard" />
          </span>
          <div>
            <b>OpenMarketplace</b>
            <small>Administration Portal</small>
          </div>
        </div>
        <div className="login-title-row centered-login-title">
          <div>
            <h1>Welcome back</h1>
            <p>Sign in with an active admin account from the database.</p>
          </div>
        </div>
        {hasDevDefault && <div className="dev-login-note">Development login is prefilled from the seeded admin account.</div>}
        <form onSubmit={submit} className="login-form">
          <AdminTextBox label="Email" value={email} onChange={(e) => setEmail(e.target.value)} type="email" autoComplete="username" placeholder="Email address" required autoFocus />
          <label>
            Password
            <div className="password-field">
              <AdminTextBox value={password} onChange={(e) => setPassword(e.target.value)} type={showPassword ? 'text' : 'password'} autoComplete="current-password" placeholder="Password" required />
              <AdminButton type="button" onClick={() => setShowPassword((v) => !v)}>
                {showPassword ? 'Hide' : 'Show'}
              </AdminButton>
            </div>
          </label>
          <div className="login-options">
            <AdminCheckbox label="Remember me" checked={remember} onChange={(e) => setRemember(e.target.checked)} />
            <AdminButton type="button" variant="ghost" className="text-btn">
              Forgot password?
            </AdminButton>
          </div>
          {err && <div className="error-box">{err}</div>}
          <AdminButton className="login-submit" variant="primary" size="lg" disabled={loading}>
            {loading ? 'Signing in...' : 'Sign in'}
          </AdminButton>
        </form>
        <div className="login-foot">
          <span>Version 1.0.0</span>
          <span>© OpenMarketplace</span>
        </div>
      </section>
    </div>
  );
}
