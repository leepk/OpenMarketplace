'use client';

import Link from 'next/link';
import { useEffect, useState } from 'react';
import { apiClient } from '@/lib/api/apiClient';
import { saveSession } from '@/lib/api/session';

function safeReturnUrl(value: unknown) {
  return typeof value === 'string' && value.startsWith('/') && !value.startsWith('//') ? value : '/profile';
}

export default function CompleteExternalProfilePage() {
  const [ticket, setTicket] = useState('');
  const [email, setEmail] = useState('');
  const [message, setMessage] = useState('');
  const [busy, setBusy] = useState(false);

  useEffect(() => {
    setTicket(new URLSearchParams(window.location.search).get('ticket') ?? '');
  }, []);

  async function submit(event: React.FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setMessage('');
    if (!ticket) {
      setMessage('This Facebook sign-in request is missing or expired. Please return to login and try again.');
      return;
    }
    if (!email.trim()) {
      setMessage('Please enter your email address.');
      return;
    }

    setBusy(true);
    try {
      const result: any = await apiClient.post('/auth/external/complete-profile', {
        ticket,
        email: email.trim().toLowerCase(),
      });
      saveSession(result);
      window.location.replace(safeReturnUrl(result.returnUrl));
    } catch (error: any) {
      setMessage(error?.message ?? 'Unable to complete Facebook sign-in.');
    } finally {
      setBusy(false);
    }
  }

  return (
    <main className="auth-page-v2">
      <section className="auth-form-wrap-v2">
        <div className="auth-card-v2">
          <span className="auth-eyebrow">Facebook Login</span>
          <h1>Complete your account</h1>
          <p className="auth-muted">Facebook did not share an email address. Enter the email you want to use for Vunoca.</p>
          <form onSubmit={submit}>
            <label>Email <b>*</b>
              <input
                type="email"
                value={email}
                onChange={(event) => setEmail(event.target.value)}
                placeholder="you@example.com"
                autoComplete="email"
                required
              />
            </label>
            <button className="auth-primary-v2" type="submit" disabled={busy || !ticket}>
              {busy ? 'Creating account...' : 'Continue'}
            </button>
          </form>
          <p className="auth-muted">Your email will be marked unverified until email verification is completed.</p>
          {message && <p className="form-error">{message}</p>}
          <p className="auth-switch-v2"><Link href="/login">Return to login</Link></p>
        </div>
      </section>
    </main>
  );
}
