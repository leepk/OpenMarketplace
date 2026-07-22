import type { Metadata } from 'next';
import Script from 'next/script';
import './globals.css';
import { SiteHeader } from '@/components/layout/SiteHeader';
import { SiteFooter } from '@/components/layout/SiteFooter';
import { MobileBottomNav } from '@/components/layout/MobileBottomNav';
import { SiteSettingsProvider } from '@/lib/site-settings';
import { appConfig } from '@/lib/config';

const GA_MEASUREMENT_ID = 'G-V1Y2G157JR';

const DEFAULT_TITLE = 'OpenMarketplace';
const DEFAULT_DESCRIPTION = 'Local classifieds marketplace';

type PublicSiteSettings = {
  siteName: string;
  seoTitle: string;
  seoDescription: string;
  faviconUrl: string;
  logoUrl: string;
};

function text(value: unknown): string {
  return typeof value === 'string' ? value.trim() : '';
}

function normalizePublicSiteSettings(payload: any): PublicSiteSettings {
  const root = payload?.success && payload?.data ? payload.data : payload;
  const branding = root?.branding ?? root?.data?.branding ?? root?.settings?.branding ?? {};
  const settings = root?.settings ?? root?.data?.settings ?? {};

  return {
    siteName: text(branding?.siteName) || text(settings?.['site.name']) || DEFAULT_TITLE,
    seoTitle: text(branding?.seoTitle) || text(settings?.['seo.title']),
    seoDescription: text(branding?.seoDescription) || text(settings?.['seo.description']),
    faviconUrl: text(branding?.faviconUrl) || text(settings?.['site.favicon_url']),
    logoUrl: text(branding?.logoUrl) || text(settings?.['site.logo_url']),
  };
}

async function getPublicSiteSettings(): Promise<PublicSiteSettings | null> {
  try {
    const response = await fetch(`${appConfig.apiBaseUrl}/site-settings`, {
      next: { revalidate: 60 },
      headers: { Accept: 'application/json' },
    });

    if (!response.ok) return null;
    return normalizePublicSiteSettings(await response.json());
  } catch {
    return null;
  }
}

export async function generateMetadata(): Promise<Metadata> {
  const settings = await getPublicSiteSettings();
  const title = settings?.seoTitle || settings?.siteName || DEFAULT_TITLE;
  const description = settings?.seoDescription || DEFAULT_DESCRIPTION;
  const icon = settings?.faviconUrl || settings?.logoUrl;

  return {
    title,
    description,
    applicationName: settings?.siteName || DEFAULT_TITLE,
    openGraph: {
      type: 'website',
      title,
      description,
      siteName: settings?.siteName || title,
    },
    twitter: {
      card: 'summary',
      title,
      description,
    },
    ...(icon
      ? {
          icons: {
            icon,
            shortcut: icon,
            apple: icon,
          },
        }
      : {}),
  };
}

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

        <Script
          src={`https://www.googletagmanager.com/gtag/js?id=${GA_MEASUREMENT_ID}`}
          strategy="afterInteractive"
        />
        <Script id="google-analytics" strategy="afterInteractive">
          {`
            window.dataLayer = window.dataLayer || [];
            function gtag(){dataLayer.push(arguments);}
            gtag('js', new Date());
            gtag('config', '${GA_MEASUREMENT_ID}');
          `}
        </Script>
      </body>
    </html>
  );
}
