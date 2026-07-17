'use client';

import Link from 'next/link';
import { useEffect, useMemo, useState } from 'react';
import { ListingCard } from '@/components/listings/ListingCard';
import type { ListingDto } from '@/lib/api/apiClient';
import { useI18n } from '@/lib/i18n/client';

const VISIBLE_COUNT = 3;

function rotateItems(items: ListingDto[], start: number) {
  if (items.length <= VISIBLE_COUNT) return items;
  return Array.from({ length: VISIBLE_COUNT }, (_, index) => items[(start + index) % items.length]);
}

export function FeaturedListings({ items }: { items: ListingDto[] }) {
  const { t } = useI18n();
  const list = useMemo(() => (items ?? []).filter(Boolean), [items]);
  const [index, setIndex] = useState(0);
  const canSlide = list.length > VISIBLE_COUNT;
  const visibleItems = rotateItems(list, index);

  useEffect(() => {
    if (!canSlide) return;
    const timer = window.setInterval(() => {
      setIndex((current) => (current + 1) % list.length);
    }, 4500);
    return () => window.clearInterval(timer);
  }, [canSlide, list.length]);

  if (!list.length) return null;

  const goPrev = () => setIndex((current) => (current - 1 + list.length) % list.length);
  const goNext = () => setIndex((current) => (current + 1) % list.length);

  return (
    <section className="featured-section">
      <div className="section-head">
        <h2>🔥 {t('featuredListings')}</h2>
        <Link href="/search?featured=true">{t('viewAll')}</Link>
      </div>
      <div className="featured-carousel">
        {canSlide ? <button className="carousel-nav prev" type="button" aria-label={t('previousFeaturedListings')} onClick={goPrev}>‹</button> : null}
        {visibleItems.map((l) => <ListingCard key={`${l.id}-${index}`} listing={{ ...l, isFeatured: true }} variant="featured" />)}
        {canSlide ? <button className="carousel-nav next" type="button" aria-label={t('nextFeaturedListings')} onClick={goNext}>›</button> : null}
      </div>
      {canSlide ? (
        <div className="carousel-dots">
          {list.map((item, dotIndex) => <button key={item.id ?? dotIndex} type="button" className={dotIndex === index ? 'active' : ''} aria-label={`${t('featuredListingSlide')} ${dotIndex + 1}`} onClick={() => setIndex(dotIndex)} />)}
        </div>
      ) : null}
    </section>
  );
}
