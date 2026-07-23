'use client';

export const GA_MEASUREMENT_ID = 'G-V1Y2G157JR';

type EventParams = Record<string, string | number | boolean | null | undefined>;

declare global {
  interface Window {
    dataLayer?: unknown[];
    gtag?: (...args: unknown[]) => void;
  }
}

function clean(params: EventParams = {}) {
  return Object.fromEntries(Object.entries(params).filter(([, value]) => value !== undefined && value !== null && value !== ''));
}

export function trackEvent(name: string, params: EventParams = {}) {
  if (typeof window === 'undefined' || typeof window.gtag !== 'function') return;
  window.gtag('event', name, clean(params));
}

export function trackPageView(url: string) {
  if (typeof window === 'undefined' || typeof window.gtag !== 'function') return;
  window.gtag('config', GA_MEASUREMENT_ID, {
    page_path: url,
    page_location: window.location.href,
    page_title: document.title,
  });
}

export const analytics = {
  search: (term: string) => trackEvent('search', { search_term: term }),
  viewItem: (params: EventParams) => trackEvent('view_item', params),
  selectItem: (params: EventParams) => trackEvent('select_item', params),
  login: (method: string) => trackEvent('login', { method }),
  signUp: (method: string) => trackEvent('sign_up', { method }),
  affiliateClick: (params: EventParams) => trackEvent('affiliate_click', params),
  postListing: (params: EventParams) => trackEvent('post_listing', params),
  contactSeller: (params: EventParams) => trackEvent('contact_seller', params),
  favorite: (params: EventParams) => trackEvent('favorite', params),
};
