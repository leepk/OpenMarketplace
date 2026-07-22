import type { Metadata } from 'next';
import { LegalPage } from '@/components/legal/LegalPage';

export const metadata: Metadata = {
  title: 'Privacy Policy | Vunoca',
  description: 'Learn how Vunoca collects, uses, protects, and shares information.',
};

export default function PrivacyPage() {
  return <LegalPage eyebrow="Legal" title="Privacy Policy" updated="July 22, 2026" intro="This Privacy Policy explains how Vunoca handles information when you browse, create an account, post listings, communicate with other users, or use third-party sign-in services." sections={[
    { title: 'Information we collect', items: ['Account information such as your name, email address, profile details, and login identifiers.', 'Listing information, photos, location details, messages, saved items, and activity on Vunoca.', 'Technical information such as browser type, device information, IP address, cookies, and usage logs.', 'Information provided by Google or Facebook when you choose social sign-in, limited to the permissions you approve.'] },
    { title: 'How we use information', items: ['Provide and secure your account and marketplace features.', 'Display listings, facilitate communication, and personalize search results.', 'Prevent fraud, abuse, prohibited activity, and technical issues.', 'Send service messages and, where permitted, product or promotional updates.', 'Improve Vunoca, measure performance, and comply with legal obligations.'] },
    { title: 'Sharing and third parties', paragraphs: ['We do not sell your personal information. We may share limited information with service providers that help operate hosting, authentication, analytics, payments, email, security, and customer support. Listings and profile information you choose to publish may be visible to other users.'], items: ['Google and Meta may process information under their own privacy policies when you use their login services.', 'External marketplace links, including affiliate links, lead to third-party websites governed by their own policies.', 'Information may be disclosed where required by law or necessary to protect users, Vunoca, or the public.'] },
    { title: 'Cookies and analytics', paragraphs: ['Vunoca may use essential cookies for sessions and security, plus optional analytics or preference technologies. Browser controls can limit cookies, although some features may stop working correctly.'] },
    { title: 'Data retention and security', paragraphs: ['We retain information only as long as reasonably necessary for the purposes described above, legal compliance, dispute resolution, and fraud prevention. We use reasonable administrative and technical safeguards, but no online service can guarantee absolute security.'] },
    { title: 'Your choices and rights', items: ['Review or update account information from your profile.', 'Request account and personal data deletion.', 'Unsubscribe from non-essential communications.', 'Contact us to request access, correction, or other rights available under applicable law.'] },
    { title: 'Children', paragraphs: ['Vunoca is not intended for children under 13, and we do not knowingly collect personal information from children under 13.'] },
    { title: 'Changes to this policy', paragraphs: ['We may update this policy as Vunoca changes. The updated date will be shown on this page, and significant changes may be communicated through the service or email.'] },
  ]} />;
}
