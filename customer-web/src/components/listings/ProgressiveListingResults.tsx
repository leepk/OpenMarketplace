'use client';

import { useMemo, useState } from 'react';
import { ListingCard } from '@/components/listings/ListingCard';
import { ExternalListingCard } from '@/components/listings/ExternalListingCard';
import type { ExternalListingDto, ListingDto } from '@/lib/api/apiClient';

type Props = {
  localItems: ListingDto[];
  externalItems?: ExternalListingDto[];
  initialCount?: number;
  increment?: number;
  externalHeading?: boolean;
};

export function ProgressiveListingResults({
  localItems,
  externalItems = [],
  initialCount = 20,
  increment = 20,
  externalHeading = true,
}: Props) {
  const [visibleCount, setVisibleCount] = useState(initialCount);
  const combined = useMemo(
    () => [
      ...localItems.map((item) => ({ kind: 'local' as const, id: String(item.id), item })),
      ...externalItems.map((item) => ({ kind: 'external' as const, id: item.externalId, item })),
    ],
    [localItems, externalItems],
  );

  const visible = combined.slice(0, visibleCount);
  const visibleLocal = visible.filter((x) => x.kind === 'local');
  const visibleExternal = visible.filter((x) => x.kind === 'external');
  const hasMore = visibleCount < combined.length;

  return (
    <>
      <div className="listing-feed-list search-results-list">
        {visibleLocal.map(({ id, item }) => (
          <ListingCard key={id} listing={item} variant="row" />
        ))}
      </div>

      {visibleExternal.length > 0 && (
        <section className="external-results-section" aria-labelledby="external-results-heading">
          {externalHeading && (
            <div className="external-results-heading">
              <div>
                <span>PARTNER MARKETPLACE</span>
                <h2 id="external-results-heading">More results from eBay</h2>
                <p>External listings open on eBay. Vunoca listings are always shown first.</p>
              </div>
              <span className="external-results-count">{externalItems.length} results</span>
            </div>
          )}
          <div className="external-listing-grid">
            {visibleExternal.map(({ id, item }) => (
              <ExternalListingCard key={id} item={item} />
            ))}
          </div>
        </section>
      )}

      {hasMore && (
        <div className="load-more-wrap">
          <button type="button" className="load-more-button" onClick={() => setVisibleCount((count) => count + increment)}>
            More items
          </button>
        </div>
      )}
    </>
  );
}
