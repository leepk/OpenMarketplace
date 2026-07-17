'use client';

import { useEffect, useRef, useState } from 'react';
import Link from 'next/link';
import { marketplaceApi, type AdvertisementDto } from '@/lib/api/apiClient';
import { Icon } from '@/components/ui/Icon';
import { useI18n } from '@/lib/i18n/client';
import { mediaUrl } from '@/lib/media/url';

type Variant = 'hero' | 'wide' | 'inline' | 'rail' | 'footer';
const SLIDE_MS = 4200;

export function AdvertisementCarousel({ placement, variant = 'wide', titleKey }: { placement: string; variant?: Variant; titleKey?: string }) {
  const { t } = useI18n();
  const [items, setItems] = useState<AdvertisementDto[]>([]);
  const [index, setIndex] = useState(0);
  const [paused, setPaused] = useState(false);
  const pointerStartX = useRef<number | null>(null);

  useEffect(() => {
    let cancelled = false;
    marketplaceApi.ads(placement)
      .then((ads) => {
        if (cancelled) return;
        setItems(Array.isArray(ads) ? ads : []);
        setIndex(0);
      })
      .catch(() => {
        if (!cancelled) setItems([]);
      });
    return () => { cancelled = true; };
  }, [placement]);

  const canSlide = items.length > 1;

  useEffect(() => {
    if (!canSlide || paused) return;
    const timer = window.setInterval(() => {
      setIndex((current) => (current + 1) % items.length);
    }, SLIDE_MS);
    return () => window.clearInterval(timer);
  }, [canSlide, items.length, paused]);

  useEffect(() => {
    if (!canSlide) return;
    const onVisibilityChange = () => setPaused(document.hidden);
    document.addEventListener('visibilitychange', onVisibilityChange);
    return () => document.removeEventListener('visibilitychange', onVisibilityChange);
  }, [canSlide]);

  const activeId = items[index]?.id;
  useEffect(() => {
    if (activeId) marketplaceApi.adImpression(activeId).catch(() => undefined);
  }, [activeId]);

  const title = titleKey ? t(titleKey) : t('sponsored');
  if (items.length === 0) return null;

  const goTo = (nextIndex: number) => {
    if (!items.length) return;
    setIndex((nextIndex + items.length) % items.length);
  };

  const onPointerDown = (event: React.PointerEvent<HTMLElement>) => {
    if (!canSlide) return;
    pointerStartX.current = event.clientX;
    setPaused(true);
  };

  const onPointerUp = (event: React.PointerEvent<HTMLElement>) => {
    if (!canSlide || pointerStartX.current === null) return;
    const delta = event.clientX - pointerStartX.current;
    pointerStartX.current = null;
    setPaused(false);
    if (Math.abs(delta) < 36) return;
    goTo(delta < 0 ? index + 1 : index - 1);
  };

  return (
    <section
      className={`ad-carousel ad-carousel-${variant} ${canSlide ? 'has-slider' : 'single-ad'}`}
      aria-label={title}
      onMouseEnter={() => setPaused(true)}
      onMouseLeave={() => setPaused(false)}
      onPointerDown={onPointerDown}
      onPointerUp={onPointerUp}
      onPointerCancel={() => { pointerStartX.current = null; setPaused(false); }}
    >
      {titleKey && <div className="ad-carousel-title"><strong>{title}</strong></div>}

      <div className="ad-carousel-viewport">
        <div className="ad-carousel-track" style={{ transform: `translate3d(-${index * 100}%, 0, 0)` }}>
          {items.map((item) => (
            <AdSlide key={item.id} item={item} sponsoredLabel={t('sponsored')} />
          ))}
        </div>
      </div>

      {canSlide && (
        <div className="ad-dots" aria-label={t('advertisementSlides')}>
          {items.map((item, i) => (
            <button
              key={item.id}
              type="button"
              className={i === index ? 'active' : ''}
              onClick={() => goTo(i)}
              aria-label={`Ad ${i + 1}`}
            />
          ))}
        </div>
      )}
    </section>
  );
}

function AdSlide({ item, sponsoredLabel }: { item: AdvertisementDto; sponsoredLabel: string }) {
  const image = mediaUrl(item.desktopImageUrl || item.mobileImageUrl || '') ?? '';
  const targetUrl = item.targetUrl || '/search';

  const trackClick = () => {
    marketplaceApi.adClick(item.id).catch(() => undefined);
  };

  const hasImage = Boolean(image);

  return (
    <Link
      href={targetUrl}
      target={item.openInNewTab ? '_blank' : undefined}
      onClick={trackClick}
      className={`ad-carousel-card ${hasImage ? 'has-image' : 'no-image'}`}
      aria-label={item.title}
      draggable={false}
    >
      {hasImage ? (
        <div className="ad-carousel-visual" aria-hidden="true">
          <img src={image} alt="" draggable={false} />
        </div>
      ) : (
        <div className="ad-carousel-image" />
      )}
      <div className="ad-carousel-overlay" />
      <div className="ad-carousel-content">
        <span className="ad-eyebrow"><Icon name="sparkle" size={13} /> {sponsoredLabel}</span>
        <h3>{item.title}</h3>
        {item.description && <p>{item.description}</p>}
      </div>
    </Link>
  );
}
