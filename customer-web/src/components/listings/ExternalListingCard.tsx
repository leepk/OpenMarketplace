'use client';

import Link from 'next/link';
import { Icon } from '@/components/ui/Icon';
import type { ExternalListingDto } from '@/lib/api/apiClient';
import { analytics } from '@/lib/analytics';

function formattedPrice(value?: number | null, currency = 'USD') {
  if (value === null || value === undefined) return 'View price';
  try {
    return new Intl.NumberFormat('en-US', {
      style: 'currency',
      currency: currency || 'USD',
      maximumFractionDigits: 2,
    }).format(value);
  } catch {
    return `${value} ${currency || 'USD'}`;
  }
}

function providerName(source?: string | null) {
  const value = (source || 'Partner').trim();
  if (!value) return 'Partner';
  if (value.toLowerCase() === 'ebay') return 'eBay';
  if (value.toLowerCase() === 'walmart') return 'Walmart';
  return value;
}

export function ExternalListingCard({ item }: { item: ExternalListingDto }) {
  const provider = providerName(item.source);
  const params = new URLSearchParams({
    title: item.title,
    url: item.itemUrl,
    source: provider,
  });

  if (item.imageUrl) params.set('image', item.imageUrl);
  if (item.price !== null && item.price !== undefined) params.set('price', String(item.price));
  if (item.currency) params.set('currency', item.currency);
  if (item.condition) params.set('condition', item.condition);
  if (item.location) params.set('location', item.location);
  if (item.seller) params.set('seller', item.seller);

  const detailUrl = `/partner/${encodeURIComponent(provider.toLowerCase())}/${encodeURIComponent(item.externalId)}?${params.toString()}`;

  return (
    <Link
      className="external-listing-card"
      href={detailUrl}
      aria-label={`View ${item.title} details on Vunoca`}
      onClick={() => analytics.selectItem({ item_id: item.externalId, item_name: item.title, item_brand: provider, item_category: 'partner_product', value: item.price ?? undefined, currency: item.currency || 'USD' })}
    >
      <div className="external-listing-image">
        {item.imageUrl ? (
          <img
            src={item.imageUrl}
            alt={item.title}
            loading="lazy"
            referrerPolicy="no-referrer"
            onError={(event) => event.currentTarget.remove()}
          />
        ) : (
          <div className="external-listing-image-fallback"><Icon name="image" size={28} /></div>
        )}
        <span className="external-source-badge">{provider}</span>
      </div>
      <div className="external-listing-content">
        <h3>{item.title}</h3>
        <strong>{formattedPrice(item.price, item.currency)}</strong>
        <p><Icon name="pin" size={13} /> {item.location || `Available on ${provider}`}</p>
        <div className="external-listing-meta">
          <span>{item.condition || 'See condition'}</span>
          <span className="external-listing-action">View details <Icon name="arrowRight" size={15} /></span>
        </div>
      </div>
    </Link>
  );
}
