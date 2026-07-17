'use client';

import Link from 'next/link';
import { useEffect, useState } from 'react';
import { saveSession } from '@/lib/api/session';

function decodeUser(token: string) {
  try {
    const payload = token.split('.')[1];
    const normalized = payload.replace(/-/g, '+').replace(/_/g, '/');
    const claims = JSON.parse(atob(normalized.padEnd(Math.ceil(normalized.length / 4) * 4, '=')));
    return {
      id: claims.sub || claims['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier'],
      name: claims.name || claims['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name'] || claims.email || 'Customer',
      email: claims.email || claims['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress'],
    };
  } catch {
    return null;
  }
}

function safeReturnUrl(value: string | null) {
  if (!value || !value.startsWith('/') || value.startsWith('//')) return '/profile';
  return value;
}

export default function ExternalAuthCallbackPage() {
  const [error, setError] = useState('');

  useEffect(() => {
    const params = new URLSearchParams(window.location.search);
    const oauthError = params.get('error');
    if (oauthError) {
      setError(oauthError);
      return;
    }

    const token = params.get('token');
    if (!token) {
      setError('The login provider did not return a valid session.');
      return;
    }

    const user = decodeUser(token);
    if (!user?.id) {
      setError('The returned login session is invalid.');
      return;
    }

    saveSession({ token, user });
    window.location.replace(safeReturnUrl(params.get('returnUrl')));
  }, []);

  return (
    <main className="auth-page-v2">
      <section className="auth-form-wrap-v2">
        <div className="auth-card-v2">
          <h1>{error ? 'Unable to sign in' : 'Signing you in...'}</h1>
          <p className={error ? 'form-error' : 'auth-muted'}>{error || 'Please wait while your account is being prepared.'}</p>
          {error && <p className="auth-switch-v2"><Link href="/login">Return to login</Link></p>}
        </div>
      </section>
    </main>
  );
}
