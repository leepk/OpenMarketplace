'use client';

import { Icon } from '@/components/ui/Icon';
import { analytics } from '@/lib/analytics';

export function AffiliateButton({ href, provider, itemId, title, price, currency }: { href: string; provider: string; itemId: string; title: string; price?: string; currency?: string }) {
  return (
    <a
      className="partner-buy-button"
      href={href}
      target="_blank"
      rel="noopener noreferrer sponsored"
      onClick={() => analytics.affiliateClick({
        provider,
        item_id: itemId,
        item_name: title,
        value: Number(price) || undefined,
        currency: currency || 'USD',
      })}
    >
      Buy on {provider} <Icon name="arrowRight" size={18} />
    </a>
  );
}
