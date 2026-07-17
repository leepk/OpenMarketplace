'use client';

import Link from 'next/link';
import { Icon } from '@/components/ui/Icon';
import { mediaUrl } from '@/lib/media/url';
import { useI18n } from '@/lib/i18n/client';

export type ListingCardData = {
  id?: string;
  title?: string;
  price?: number | string | null;
  location?: string | null;
  categoryName?: string | null;
  categoryCode?: string | null;
  categoryIconKey?: string | null;
  status?: string | null;
  isFeatured?: boolean;
  isUrgent?: boolean;
  isPinned?: boolean;
  packageCode?: string | null;
  packageStatus?: string | null;
  packageStartsAt?: string | null;
  packageEndsAt?: string | null;
  imageUrl?: string | null;
  thumbnailUrl?: string | null;
  coverImageUrl?: string | null;
  description?: string | null;
  createdAt?: string | null;
  viewCount?: number;
  favoriteCount?: number;
  likeCount?: number;
  commentCount?: number;
  sellerVerified?: boolean;
};

function price(value: ListingCardData['price'], freeLabel = 'Free', contactLabel = 'Contact') {
  if (value === null || value === undefined || value === '') return contactLabel;
  const n = typeof value === 'number' ? value : Number(value);
  if (Number.isNaN(n)) return String(value);
  return n === 0 ? freeLabel : new Intl.NumberFormat('en-US', { style: 'currency', currency: 'USD', maximumFractionDigits: 0 }).format(n);
}

function artKind(listing: ListingCardData) {
  const category = `${listing.categoryCode ?? ''} ${listing.categoryName ?? ''} ${listing.categoryIconKey ?? ''}`.toLowerCase();
  const title = (listing.title ?? '').toLowerCase();
  const text = `${category} ${title}`;

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

function firstImage(listing: ListingCardData) {
  return mediaUrl(listing.imageUrl ?? listing.thumbnailUrl ?? listing.coverImageUrl ?? null);
}

function packageCode(listing: ListingCardData) {
  return (listing.packageCode ?? (listing.isPinned ? 'PREMIUM' : listing.isUrgent ? 'URGENT' : listing.isFeatured ? 'FEATURED' : 'FREE')).toUpperCase();
}

function packageIsActive(listing: ListingCardData) {
  const status = (listing.packageStatus ?? 'Active').toLowerCase();
  if (status.includes('expired') || status.includes('pending') || status.includes('failed')) return false;
  if (listing.packageEndsAt) {
    const ends = new Date(listing.packageEndsAt).getTime();
    if (Number.isFinite(ends) && ends < Date.now()) return false;
  }
  return true;
}

function ListingImage({ listing }: { listing: ListingCardData }) {
  const { t, packageLabel, category } = useI18n();
  const pkg = packageCode(listing);
  const activePkg = packageIsActive(listing);
  const src = firstImage(listing);
  const fallbackLabel = category(listing.categoryCode ?? listing.categoryName) || t('featuredListings');
  return (
    <div className={`listing-image image-${artKind(listing)}`}>
      <div className="generated-art"><span>{fallbackLabel}</span></div>
      {src && (
        <img
          src={src}
          alt={listing.title ?? 'Listing'}
          onError={(event) => {
            event.currentTarget.remove();
          }}
        />
      )}
      {activePkg && pkg === 'FEATURED' && <span className="badge badge-featured"><Icon name="star" size={12} /> {packageLabel(pkg)}</span>}
      {activePkg && pkg === 'PREMIUM' && <span className="badge badge-premium">{packageLabel(pkg)}</span>}
      {activePkg && pkg === 'URGENT' && <span className="badge badge-urgent">{packageLabel(pkg)}</span>}
      {!activePkg && pkg !== 'FREE' && <span className="badge badge-pending">{t('packagePending')}</span>}
      <span className="save-bubble"><Icon name="heart" size={18} /></span>
    </div>
  );
}

export function ListingCard({ listing, variant = 'row' }: { listing: ListingCardData; variant?: 'row' | 'featured' | 'mini' | 'map' }) {
  const { t, packageLabel } = useI18n();
  const content = (
    <>
      <ListingImage listing={listing} />
      <div className="listing-content">
        <div className="listing-title-row">
          <h3>{listing.title ?? t('yourListingTitle')}</h3>
          {listing.sellerVerified && <span className="verified-pill">{t('verifiedSeller')}</span>}
        </div>
        <strong className="listing-price">{price(listing.price, t('free'), t('contact'))}</strong>
        <p className="listing-location"><Icon name="pin" size={13} /> {listing.location ?? 'San Jose, CA'} <span>•</span> {listing.createdAt ? t('recently') : t('recently')}</p>
        {packageCode(listing) !== 'FREE' && <p className={`listing-package-line ${packageIsActive(listing) ? 'active' : 'pending'}`}><Icon name="tag" size={13} /> {packageLabel(packageCode(listing))} · {packageIsActive(listing) ? t('packageActive') : t('packagePending')}</p>}
        {variant !== 'mini' && <p className="listing-description">{listing.description ?? t('noDescription')}</p>}
        {variant !== 'mini' && <div className="listing-stats"><span><Icon name="eye" size={15} /> {listing.viewCount ?? 0}</span><span><Icon name="heart" size={15} /> {listing.favoriteCount ?? listing.likeCount ?? 0}</span><span><Icon name="comment" size={15} /> {listing.commentCount ?? 0}</span><span className="stat-save"><Icon name="bookmark" size={18} /></span></div>}
      </div>
    </>
  );
  const pkg = packageCode(listing);
  const activePkg = packageIsActive(listing);
  const cls = `listing-card listing-${variant} ${activePkg && pkg === 'URGENT' ? 'is-urgent' : ''} ${activePkg && pkg === 'PREMIUM' ? 'is-premium' : ''} ${activePkg && pkg === 'FEATURED' ? 'is-featured' : ''} ${!activePkg && pkg !== 'FREE' ? 'is-package-pending' : ''}`;
  return listing.id ? <Link className={cls} href={`/listings/${listing.id}`}>{content}</Link> : <article className={cls}>{content}</article>;
}
