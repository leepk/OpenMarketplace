'use client';

import { Icon } from '@/components/ui/Icon';
import type { ExternalListingDto } from '@/lib/api/apiClient';

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

export function ExternalListingCard({ item }: { item: ExternalListingDto }) {
  return (
    <a
      className="external-listing-card"
      href={item.itemUrl}
      target="_blank"
      rel="noopener noreferrer sponsored"
      aria-label={`View ${item.title} on eBay`}
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
        <span className="external-source-badge">eBay</span>
      </div>
      <div className="external-listing-content">
        <h3>{item.title}</h3>
        <strong>{formattedPrice(item.price, item.currency)}</strong>
        <p><Icon name="pin" size={13} /> {item.location || 'Available on eBay'}</p>
        <div className="external-listing-meta">
          <span>{item.condition || 'See condition'}</span>
          <span className="external-listing-action">View on eBay <Icon name="arrowRight" size={15} /></span>
        </div>
      </div>
    </a>
  );
}
