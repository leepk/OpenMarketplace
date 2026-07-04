"use client";

import Link from "next/link";
import { useEffect, useMemo, useState } from "react";
import { Icon } from "@/components/ui/Icon";
import { ListingCard } from "@/components/listings/ListingCard";
import { mediaUrl } from "@/lib/media/url";
import type { ListingDto } from "@/lib/api/apiClient";
import { useI18n } from "@/lib/i18n/client";

type UserPosition = { lat: number; lng: number };

function numberFrom(value: unknown): number | null {
  const n = typeof value === "number" ? value : Number(value);
  return Number.isFinite(n) ? n : null;
}

function coords(item: ListingDto): UserPosition | null {
  const lat = numberFrom(item.latitude ?? item.lat);
  const lng = numberFrom(item.longitude ?? item.lng);
  if (lat === null || lng === null) return null;
  return { lat, lng };
}

function distanceMiles(a: UserPosition, b: UserPosition) {
  const R = 3958.8;
  const dLat = ((b.lat - a.lat) * Math.PI) / 180;
  const dLng = ((b.lng - a.lng) * Math.PI) / 180;
  const lat1 = (a.lat * Math.PI) / 180;
  const lat2 = (b.lat * Math.PI) / 180;
  const h =
    Math.sin(dLat / 2) ** 2 +
    Math.cos(lat1) * Math.cos(lat2) * Math.sin(dLng / 2) ** 2;
  return 2 * R * Math.asin(Math.sqrt(h));
}

function money(value?: number | string | null) {
  if (value == null || value === "") return "$0";
  const n = Number(value);
  if (!Number.isFinite(n)) return String(value);
  return n === 0 ? "Free" : `$${n.toLocaleString()}`;
}

function firstImage(item: ListingDto) {
  const source = item as ListingDto & { thumbnailUrl?: string | null; coverImageUrl?: string | null };
  return mediaUrl(source.imageUrl ?? source.thumbnailUrl ?? source.coverImageUrl ?? item.images?.[0]?.url ?? null);
}

function artKind(item: ListingDto) {
  const text = `${item.categoryCode ?? ''} ${item.categoryName ?? ''} ${item.categoryIconKey ?? ''} ${item.title ?? ''}`.toLowerCase();
  if (text.includes('vehicle') || text.includes('car') || text.includes('auto') || text.includes('bmw') || text.includes('toyota') || text.includes('honda')) return 'car';
  if (text.includes('phone') || text.includes('mobile') || text.includes('iphone') || text.includes('pixel')) return 'phone';
  if (text.includes('computer') || text.includes('electronics') || text.includes('macbook') || text.includes('laptop')) return 'laptop';
  if (text.includes('real estate') || text.includes('housing') || text.includes('rental') || text.includes('apartment') || text.includes('home') || text.includes('house') || text.includes('furniture')) return 'home';
  if (text.includes('bike') || text.includes('bicycle') || text.includes('sports') || text.includes('outdoor')) return 'bike';
  if (text.includes('job') || text.includes('career') || text.includes('service')) return 'service';
  if (text.includes('pet') || text.includes('animal')) return 'pet';
  if (text.includes('fashion') || text.includes('clothing') || text.includes('baby') || text.includes('kids')) return 'fashion';
  return 'default';
}

function MapBottomListingCard({ item }: { item: ListingDto }) {
  const { t } = useI18n();
  const src = firstImage(item);
  const fallbackLabel = item.categoryName ?? t('featuredListings');
  return (
    <article className="map-bottom-card-v3">
      <Link href={`/listings/${item.id}`} className={`map-bottom-image-v3 image-${artKind(item)}`}>
        <div className="generated-art"><span>{fallbackLabel}</span></div>
        {src && (
          <img
            src={src}
            alt={item.title ?? 'Listing'}
            onError={(event) => {
              event.currentTarget.remove();
            }}
          />
        )}
      </Link>
      <Link href={`/listings/${item.id}`} className="map-bottom-info-v3">
        <strong>{item.title ?? t('yourListingTitle')}</strong>
        <b>{money(item.price)}</b>
        <span>{item.location ?? 'San Jose, CA'}</span>
      </Link>
      <a className="map-bottom-directions-v3" href={directionsUrl(item)} target="_blank" rel="noreferrer">Directions</a>
    </article>
  );
}

function directionsUrl(item: ListingDto) {
  const point = coords(item);
  const destination = point ? `${point.lat},${point.lng}` : item.location ?? '';
  return `https://www.google.com/maps/dir/?api=1&destination=${encodeURIComponent(destination)}`;
}

function project(
  point: UserPosition,
  bounds: { minLat: number; maxLat: number; minLng: number; maxLng: number },
) {
  const lngRange = Math.max(bounds.maxLng - bounds.minLng, 0.01);
  const latRange = Math.max(bounds.maxLat - bounds.minLat, 0.01);
  const x = ((point.lng - bounds.minLng) / lngRange) * 82 + 9;
  const y = (1 - (point.lat - bounds.minLat) / latRange) * 72 + 12;
  return {
    left: `${Math.max(5, Math.min(95, x))}%`,
    top: `${Math.max(8, Math.min(88, y))}%`,
  };
}

export function MapExperience({ listings }: { listings: ListingDto[] }) {
  const { t } = useI18n();
  const [userPosition, setUserPosition] = useState<UserPosition | null>(null);
  const [geoMessage, setGeoMessage] = useState(t("useYourLocation"));
  const [bottomIndex, setBottomIndex] = useState(0);
  const located = useMemo(
    () =>
      listings
        .map((item) => ({ item, point: coords(item) }))
        .filter((x): x is { item: ListingDto; point: UserPosition } =>
          Boolean(x.point),
        ),
    [listings],
  );
  const missingCount = listings.length - located.length;
  const bounds = useMemo(() => {
    const points = [
      ...located.map((x) => x.point),
      ...(userPosition ? [userPosition] : []),
    ];
    if (!points.length)
      return { minLat: 37.22, maxLat: 37.45, minLng: -122.12, maxLng: -121.82 };
    return {
      minLat: Math.min(...points.map((p) => p.lat)) - 0.025,
      maxLat: Math.max(...points.map((p) => p.lat)) + 0.025,
      minLng: Math.min(...points.map((p) => p.lng)) - 0.035,
      maxLng: Math.max(...points.map((p) => p.lng)) + 0.035,
    };
  }, [located, userPosition]);
  const sorted = useMemo(() => {
    if (!userPosition) return listings;
    return [...listings].sort((a, b) => {
      const ac = coords(a),
        bc = coords(b);
      if (!ac && !bc) return 0;
      if (!ac) return 1;
      if (!bc) return -1;
      return distanceMiles(userPosition, ac) - distanceMiles(userPosition, bc);
    });
  }, [listings, userPosition]);

  const bottomCanSlide = sorted.length > 4;
  const bottomItems = useMemo(() => {
    if (!bottomCanSlide) return sorted.slice(0, 4);
    return Array.from({ length: 4 }, (_, index) => sorted[(bottomIndex + index) % sorted.length]);
  }, [bottomCanSlide, bottomIndex, sorted]);

  useEffect(() => {
    if (!bottomCanSlide) return;
    const timer = window.setInterval(() => {
      setBottomIndex((current) => (current + 1) % sorted.length);
    }, 5000);
    return () => window.clearInterval(timer);
  }, [bottomCanSlide, sorted.length]);

  function goBottomPrev() {
    if (!sorted.length) return;
    setBottomIndex((current) => (current - 1 + sorted.length) % sorted.length);
  }

  function goBottomNext() {
    if (!sorted.length) return;
    setBottomIndex((current) => (current + 1) % sorted.length);
  }

  function askLocation() {
    if (!navigator.geolocation) {
      setGeoMessage(t("locationNotAvailable"));
      return;
    }
    setGeoMessage(t("loading"));
    navigator.geolocation.getCurrentPosition(
      (pos) => {
        setUserPosition({
          lat: pos.coords.latitude,
          lng: pos.coords.longitude,
        });
        setGeoMessage(t("nearYou"));
      },
      () => setGeoMessage(t("useYourLocation")),
      { enableHighAccuracy: true, timeout: 10000 },
    );
  }

  return (
    <section className="map-v2 shell-wide">
      <aside className="map-v2-list">
        <div className="map-v2-toolbar">
          <Link href="/search">{t("list")}</Link>
          <Link href="/map" className="active">
            {t("map")}
          </Link>
          <button>
            <Icon name="filter" size={16} /> {t("filters")}
          </button>
        </div>
        <div className="location-permission-card">
          <span>
            <Icon name="pin" size={20} />
          </span>
          <div>
            <strong>{t("useYourLocation")}</strong>
            <p>{geoMessage}</p>
            {missingCount > 0 && (
              <small>
                {missingCount} {t("listingsNoCoords")}
              </small>
            )}
          </div>
          <button onClick={askLocation}>{t("enable")}</button>
        </div>
        <h2>
          {sorted.length} {t("listings")} {t("nearYou")}
        </h2>
        <div className="map-v2-results">
          {sorted.slice(0, 8).map((item) => (
            <div key={item.id} className="map-result-with-direction-v2">
              <ListingCard listing={item} variant="mini" />
              <a href={directionsUrl(item)} target="_blank" rel="noreferrer">Directions</a>
            </div>
          ))}
          {!sorted.length && (
            <div className="empty-state-modern">
              <strong>{t("noListingsYetMap")}</strong>
              <p>{t("seedCoordinatesHint")}</p>
            </div>
          )}
        </div>
      </aside>
      <div className="map-v2-canvas">
        <div className="map-v2-search">
          <Icon name="search" size={18} />
          <input placeholder={t("searchThisArea")} />
          <button>{t("search")}</button>
        </div>
        <div className="map-road horizontal one" />
        <div className="map-road horizontal two" />
        <div className="map-road vertical one" />
        <div className="map-road vertical two" />
        <div className="map-water" />
        <div className="map-park" />
        {userPosition && (
          <div
            className="user-location-marker"
            style={project(userPosition, bounds)}
          >
            <span />
          </div>
        )}
        {located.map(({ item, point }, idx) => (
          <Link
            key={item.id}
            href={`/listings/${item.id}`}
            className={`map-price-marker tone-${idx % 5}`}
            style={project(point, bounds)}
          >
            {money(item.price)}
          </Link>
        ))}
        {!located.length && (
          <div className="map-empty-note">
            <Icon name="map" size={32} />
            <strong>{t("noCoordinates")}</strong>
            <p>{t("coordinateHint")}</p>
          </div>
        )}
        <div className="map-v2-controls">
          <button>+</button>
          <button>−</button>
          <button onClick={askLocation}>
            <Icon name="pin" size={18} />
          </button>
        </div>
        <div className="map-v2-bottom">
          <div className="map-bottom-head-v2">
            <h3>
              {sorted.length} {t("listings")}
            </h3>
            {bottomCanSlide && (
              <div className="map-bottom-nav-v2">
                <button type="button" aria-label="Previous listings" onClick={goBottomPrev}>‹</button>
                <button type="button" aria-label="Next listings" onClick={goBottomNext}>›</button>
              </div>
            )}
          </div>
          <div className="map-bottom-track-v3">
            {bottomItems.map((item) => (
              <MapBottomListingCard key={`${item.id}-${bottomIndex}`} item={item} />
            ))}
          </div>
          {bottomCanSlide && (
            <div className="map-bottom-dots-v2">
              {sorted.map((item, dotIndex) => (
                <button key={item.id ?? dotIndex} type="button" className={dotIndex === bottomIndex ? 'active' : ''} aria-label={`Go to listing ${dotIndex + 1}`} onClick={() => setBottomIndex(dotIndex)} />
              ))}
            </div>
          )}
        </div>
      </div>
    </section>
  );
}
