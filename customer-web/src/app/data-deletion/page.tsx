import type { Metadata } from 'next';
import Link from 'next/link';

export const metadata: Metadata = {
  title: 'Data Deletion Instructions | Vunoca',
  description: 'Instructions for deleting a Vunoca account and data associated with Facebook Login.',
};

export default function DataDeletionPage() {
  return (
    <div className="legal-page-shell">
      <article className="legal-card">
        <div className="legal-heading">
          <span>Account & privacy</span>
          <h1>Data Deletion Instructions</h1>
          <p>You may request deletion of your Vunoca account and personal data, including data associated with Facebook Login.</p>
          <small>Last updated: July 22, 2026</small>
        </div>
        <div className="deletion-steps">
          <section><b>1</b><div><h2>Send your request</h2><p>Email <a href="mailto:support@vunoca.com?subject=Vunoca%20Data%20Deletion%20Request">support@vunoca.com</a> from the email address associated with your Vunoca account. Use the subject “Vunoca Data Deletion Request.”</p></div></section>
          <section><b>2</b><div><h2>Include account details</h2><p>Include your name, account email, and whether you signed in using Facebook, Google, or email. Do not send your password or social-login access token.</p></div></section>
          <section><b>3</b><div><h2>Complete verification</h2><p>For security, we may ask you to verify ownership of the account before processing the request.</p></div></section>
          <section><b>4</b><div><h2>Deletion processing</h2><p>After verification, we will delete or anonymize eligible account information within 30 days, except information that must be retained for legal, fraud-prevention, security, or transaction-record purposes.</p></div></section>
        </div>
        <div className="legal-help-box"><strong>Facebook removal</strong><span>You can also remove Vunoca from Facebook under Settings & privacy → Settings → Apps and websites. Removing the Facebook connection does not automatically delete your Vunoca account, so submit the request above for full deletion.</span><Link href="/privacy">Read Privacy Policy</Link></div>
      </article>
    </div>
  );
}
