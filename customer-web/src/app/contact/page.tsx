import type { Metadata } from 'next';
import Link from 'next/link';

export const metadata: Metadata = {
  title: 'Contact Us | Vunoca',
  description: 'Contact Vunoca customer support.',
};

export default function ContactPage() {
  return (
    <div className="legal-page-shell">
      <section className="contact-card">
        <div className="legal-heading">
          <span>Support</span>
          <h1>Contact Vunoca</h1>
          <p>Questions about your account, a listing, privacy, or a technical problem? Send us a message and include enough detail for us to investigate.</p>
        </div>
        <div className="contact-grid">
          <div><strong>Email support</strong><a href="mailto:support@vunoca.com">support@vunoca.com</a><p>Best for account, listing, safety, and technical questions.</p></div>
          <div><strong>Website</strong><a href="https://vunoca.com">https://vunoca.com</a><p>Browse listings and manage your Vunoca account.</p></div>
          <div><strong>Privacy requests</strong><Link href="/data-deletion">Data deletion instructions</Link><p>Request deletion of your account and associated personal data.</p></div>
          <div><strong>Response time</strong><span>Usually within 2–5 business days</span><p>Urgent safety concerns should be clearly identified in the subject line.</p></div>
        </div>
      </section>
    </div>
  );
}
