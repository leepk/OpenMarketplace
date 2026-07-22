import type { Metadata } from 'next';
import { LegalPage } from '@/components/legal/LegalPage';

export const metadata: Metadata = {
  title: 'Terms of Service | Vunoca',
  description: 'Terms governing the use of the Vunoca marketplace and related services.',
};

export default function TermsPage() {
  return <LegalPage eyebrow="Legal" title="Terms of Service" updated="July 22, 2026" intro="These Terms govern your access to and use of Vunoca. By creating an account or using the service, you agree to these Terms and our Privacy Policy." sections={[
    { title: 'Using Vunoca', items: ['You must provide accurate information and keep your account secure.', 'You are responsible for activity performed through your account.', 'You must be legally able to enter into transactions and comply with applicable laws.', 'Vunoca may limit, suspend, or terminate access where necessary to protect the marketplace.'] },
    { title: 'Listings and transactions', paragraphs: ['Vunoca provides tools that help people discover, advertise, buy, and sell items or services. Unless expressly stated otherwise, Vunoca is not the buyer, seller, owner, manufacturer, or guarantor of user listings.'], items: ['Sellers are responsible for listing accuracy, legality, condition, pricing, delivery, and fulfillment.', 'Buyers must evaluate listings and sellers before completing a transaction.', 'Users are responsible for taxes, licenses, warranties, returns, and transaction disputes.'] },
    { title: 'Prohibited activity', items: ['Illegal, stolen, counterfeit, unsafe, recalled, or prohibited goods and services.', 'Fraud, impersonation, misleading claims, spam, harassment, or manipulation of ratings and engagement.', 'Malware, scraping that violates our controls, unauthorized automated access, or attempts to disrupt the service.', 'Content that infringes intellectual property, privacy, publicity, or other legal rights.'] },
    { title: 'Payments and promotions', paragraphs: ['Paid listing packages, promotions, or other services may be processed by third-party payment providers. Prices, duration, eligibility, refund terms, and applicable taxes will be presented before purchase.'] },
    { title: 'External marketplace content', paragraphs: ['Vunoca may display products or links from external marketplaces. Prices, inventory, shipping, returns, and product details are controlled by those marketplaces and may change without notice. Vunoca may earn a commission from qualifying affiliate purchases.'] },
    { title: 'Content and license', paragraphs: ['You retain ownership of content you submit. You grant Vunoca a non-exclusive, worldwide, royalty-free license to host, reproduce, format, display, and distribute that content as needed to operate and promote the service.'] },
    { title: 'Disclaimers and liability', paragraphs: ['Vunoca is provided on an “as is” and “as available” basis. To the extent permitted by law, Vunoca disclaims implied warranties and is not responsible for user conduct, listing accuracy, third-party products, transaction losses, or indirect and consequential damages.'] },
    { title: 'Changes and termination', paragraphs: ['We may modify the service or these Terms. Continued use after updated Terms take effect constitutes acceptance. You may stop using Vunoca at any time and may request account deletion.'] },
  ]} />;
}
