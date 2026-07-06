import type { Metadata } from 'next';
import './globals.css';
import { SiteHeader } from '@/components/layout/SiteHeader';
import { SiteFooter } from '@/components/layout/SiteFooter';
import { MobileBottomNav } from '@/components/layout/MobileBottomNav';
import { SiteSettingsProvider } from '@/lib/site-settings';

export const metadata: Metadata = {
  title: 'OpenMarketplace',
  description: 'Local classifieds marketplace',
};

export default function RootLayout({ children }: { children: React.ReactNode }) {
  return (
    <html lang="en">
      <body>
        <SiteSettingsProvider>
          <SiteHeader />
          <main className="page-shell">{children}</main>
          <MobileBottomNav />
          <SiteFooter />
        </SiteSettingsProvider>
      </body>
    </html>
  );
}
