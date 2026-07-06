'use client';

import { createContext, useContext, useEffect, useMemo, useState, type ReactNode } from 'react';
import { appConfig } from '@/lib/config';
import { mediaUrl } from '@/lib/media/url';

export type SiteBranding = {
  siteName: string;
  logoUrl: string;
  faviconUrl: string;
  primaryColor: string;
  secondaryColor: string;
  facebookUrl: string;
  youtubeUrl: string;
  instagramUrl: string;
  contactEmail: string;
  contactPhone: string;
  contactAddress: string;
  footerText: string;
  seoTitle: string;
  seoDescription: string;
};

const DEFAULT_BRANDING: SiteBranding = {
  siteName: 'OpenMarketplace',
  logoUrl: '',
  faviconUrl: '',
  primaryColor: '#0969ff',
  secondaryColor: '#f59e0b',
  facebookUrl: '',
  youtubeUrl: '',
  instagramUrl: '',
  contactEmail: '',
  contactPhone: '',
  contactAddress: '',
  footerText: 'Modern local classifieds for buying, selling and discovering trusted listings nearby.',
  seoTitle: 'OpenMarketplace',
  seoDescription: 'Local classifieds marketplace',
};

const SiteSettingsContext = createContext<SiteBranding>(DEFAULT_BRANDING);

function normalizeBranding(data: any): SiteBranding {
  const branding = data?.branding ?? data?.data?.branding ?? data?.settings?.branding ?? {};
  const settings = data?.settings ?? data?.data?.settings ?? {};
  const value = (camel: keyof SiteBranding, key: string, fallback = '') => {
    const fromBranding = branding?.[camel];
    const fromSettings = settings?.[key];
    const raw = fromBranding ?? fromSettings ?? fallback;
    return typeof raw === 'string' ? raw.trim() : String(raw ?? '').trim();
  };
  return {
    siteName: value('siteName', 'site.name', DEFAULT_BRANDING.siteName),
    logoUrl: value('logoUrl', 'site.logo_url', DEFAULT_BRANDING.logoUrl),
    faviconUrl: value('faviconUrl', 'site.favicon_url', DEFAULT_BRANDING.faviconUrl),
    primaryColor: value('primaryColor', 'site.primary_color', DEFAULT_BRANDING.primaryColor),
    secondaryColor: value('secondaryColor', 'site.secondary_color', DEFAULT_BRANDING.secondaryColor),
    facebookUrl: value('facebookUrl', 'social.facebook_url', DEFAULT_BRANDING.facebookUrl),
    youtubeUrl: value('youtubeUrl', 'social.youtube_url', DEFAULT_BRANDING.youtubeUrl),
    instagramUrl: value('instagramUrl', 'social.instagram_url', DEFAULT_BRANDING.instagramUrl),
    contactEmail: value('contactEmail', 'contact.email', DEFAULT_BRANDING.contactEmail),
    contactPhone: value('contactPhone', 'contact.phone', DEFAULT_BRANDING.contactPhone),
    contactAddress: value('contactAddress', 'contact.address', DEFAULT_BRANDING.contactAddress),
    footerText: value('footerText', 'footer.text', DEFAULT_BRANDING.footerText),
    seoTitle: value('seoTitle', 'seo.title', DEFAULT_BRANDING.seoTitle),
    seoDescription: value('seoDescription', 'seo.description', DEFAULT_BRANDING.seoDescription),
  };
}

function setFavicon(url?: string | null) {
  const resolved = mediaUrl(url);
  if (!resolved || typeof document === 'undefined') return;
  let link = document.querySelector<HTMLLinkElement>('link[rel="icon"]');
  if (!link) {
    link = document.createElement('link');
    link.rel = 'icon';
    document.head.appendChild(link);
  }
  link.href = resolved;
}

function applyCssVariables(branding: SiteBranding) {
  if (typeof document === 'undefined') return;
  if (branding.primaryColor) document.documentElement.style.setProperty('--primary', branding.primaryColor);
  if (branding.primaryColor) document.documentElement.style.setProperty('--primary-dark', branding.primaryColor);
  if (branding.primaryColor) document.documentElement.style.setProperty('--primary-soft', `${branding.primaryColor}18`);
  if (branding.primaryColor) document.documentElement.style.setProperty('--brand-button-bg', `linear-gradient(180deg, ${branding.primaryColor}, ${branding.primaryColor})`);
  if (branding.secondaryColor) document.documentElement.style.setProperty('--secondary', branding.secondaryColor);
  if (branding.secondaryColor) document.documentElement.style.setProperty('--secondary-soft', `${branding.secondaryColor}18`);
  if (branding.secondaryColor) document.documentElement.style.setProperty('--amber', branding.secondaryColor);
}

export function SiteSettingsProvider({ children }: { children: ReactNode }) {
  const [branding, setBranding] = useState<SiteBranding>(DEFAULT_BRANDING);

  useEffect(() => {
    let cancelled = false;
    async function load() {
      try {
        const res = await fetch(`${appConfig.apiBaseUrl}/site-settings`, { cache: 'no-store' });
        const payload = await res.json().catch(() => null);
        if (!res.ok) throw new Error('Site settings request failed');
        const next = normalizeBranding(payload?.success && payload?.data ? payload.data : payload);
        if (!cancelled) setBranding(next);
      } catch {
        if (!cancelled) setBranding(DEFAULT_BRANDING);
      }
    }
    load();
    return () => { cancelled = true; };
  }, []);

  useEffect(() => {
    applyCssVariables(branding);
    setFavicon(branding.faviconUrl || branding.logoUrl);
    if (branding.seoTitle || branding.siteName) document.title = branding.seoTitle || branding.siteName;
    const description = branding.seoDescription;
    if (description) {
      let meta = document.querySelector<HTMLMetaElement>('meta[name="description"]');
      if (!meta) {
        meta = document.createElement('meta');
        meta.name = 'description';
        document.head.appendChild(meta);
      }
      meta.content = description;
    }
  }, [branding]);

  const value = useMemo(() => branding, [branding]);
  return <SiteSettingsContext.Provider value={value}>{children}</SiteSettingsContext.Provider>;
}

export function useSiteSettings() {
  return useContext(SiteSettingsContext);
}

export function resolveSiteImage(url?: string | null) {
  return mediaUrl(url);
}
