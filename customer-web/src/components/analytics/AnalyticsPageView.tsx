'use client';

import { useEffect, useRef } from 'react';
import { usePathname, useSearchParams } from 'next/navigation';
import { analytics, trackPageView } from '@/lib/analytics';

export function AnalyticsPageView() {
  const pathname = usePathname();
  const searchParams = useSearchParams();
  const lastUrl = useRef('');
  const query = searchParams.toString();
  const url = query ? `${pathname}?${query}` : pathname;

  useEffect(() => {
    if (!url || lastUrl.current === url) return;
    lastUrl.current = url;
    trackPageView(url);

    if (pathname === '/search') {
      const term = searchParams.get('q')?.trim();
      if (term) analytics.search(term);
    }

    const partnerMatch = pathname.match(/^\/partner\/([^/]+)\/([^/]+)/);
    if (partnerMatch) {
      analytics.viewItem({
        item_id: decodeURIComponent(partnerMatch[2]),
        item_brand: decodeURIComponent(partnerMatch[1]),
        item_name: searchParams.get('title') || undefined,
        item_category: 'partner_product',
        value: Number(searchParams.get('price')) || undefined,
        currency: searchParams.get('currency') || 'USD',
      });
    }

    const listingMatch = pathname.match(/^\/listings\/([^/]+)/);
    if (listingMatch) analytics.viewItem({ item_id: listingMatch[1], item_category: 'local_listing' });
  }, [pathname, searchParams, url]);

  return null;
}
