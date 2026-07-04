import Link from 'next/link';
import { apiClient } from '@/lib/api/apiClient';
import { ListingCard } from '@/components/listings/ListingCard';
import { Icon } from '@/components/ui/Icon';

function getInitials(name?: string | null) {
  const parts = (name ?? 'Seller').trim().split(/\s+/).filter(Boolean);
  return parts.slice(0, 2).map((part) => part[0]?.toUpperCase()).join('') || 'SE';
}

function formatNumber(value: unknown) {
  const n = typeof value === 'number' ? value : Number(value ?? 0);
  return Number.isFinite(n) ? new Intl.NumberFormat('en-US').format(n) : '0';
}

function rating(value: unknown) {
  const n = typeof value === 'number' ? value : Number(value ?? 0);
  return Number.isFinite(n) && n > 0 ? n.toFixed(1) : 'New';
}

export default async function Page({ params }: { params: Promise<{ id: string }> }) {
  const { id } = await params;
  let data: any = null;

  try {
    data = await apiClient.get<any>(`/users/${id}/seller-profile`);
  } catch {
    data = null;
  }

  if (!data?.user) {
    return (
      <main className="seller-profile-shell shell-wide">
        <section className="seller-empty-modern">
          <span><Icon name="user" size={42} /></span>
          <h1>Seller not found</h1>
          <p>This seller profile is unavailable or has been removed.</p>
          <Link href="/search">Browse listings</Link>
        </section>
      </main>
    );
  }

  const user = data.user ?? {};
  const badges = data.badges ?? {};
  const listings = data.listings ?? [];
  const reviews = data.reviews ?? [];
  const sellerName = user.name ?? user.displayName ?? 'Local seller';
  const location = user.location ?? user.city ?? 'Local marketplace';
  const verifiedCount = [badges.phoneVerified, badges.emailVerified, badges.businessVerified, badges.identityVerified].filter(Boolean).length;

  return (
    <main className="seller-profile-shell shell-wide">
      <nav className="seller-breadcrumb">
        <Link href="/">Home</Link>
        <span>/</span>
        <Link href="/search">Marketplace</Link>
        <span>/</span>
        <b>{sellerName}</b>
      </nav>

      <section className="seller-profile-layout">
        <aside className="seller-profile-left">
          <section className="seller-identity-card">
            <div className="seller-cover-modern" />
            <div className="seller-avatar-large">{getInitials(sellerName)}</div>
            <h1>{sellerName}</h1>
            <p><Icon name="pin" size={15} /> {location}</p>
            <div className="seller-rating-row">
              <strong><Icon name="star" size={17} /> {rating(user.rating)}</strong>
              <span>{formatNumber(user.reviewCount)} reviews</span>
            </div>
            <div className="seller-cta-stack">
              <Link className="seller-primary-action" href={`/messages?seller=${user.id ?? id}`}><Icon name="message" size={18} /> Message seller</Link>
              <Link className="seller-secondary-action" href={`/search?sellerId=${user.id ?? id}`}><Icon name="list" size={18} /> View all listings</Link>
            </div>
          </section>

          <section className="seller-trust-card">
            <div className="seller-section-head"><span><Icon name="shield" size={18} /></span><div><strong>Trust & verification</strong><small>{verifiedCount} verification checks</small></div></div>
            <div className="trust-list-modern">
              <span className={badges.phoneVerified ? 'ok' : ''}><Icon name="check" size={15} /> Phone verified</span>
              <span className={badges.emailVerified ? 'ok' : ''}><Icon name="check" size={15} /> Email verified</span>
              <span className={badges.businessVerified ? 'ok' : ''}><Icon name="check" size={15} /> Business verified</span>
              <span className={badges.identityVerified ? 'ok' : ''}><Icon name="check" size={15} /> Identity verified</span>
            </div>
          </section>

          <section className="seller-safety-card">
            <h3>Safety tips</h3>
            <p>Meet in public places, inspect items before paying, and never share login codes or private information.</p>
          </section>
        </aside>

        <section className="seller-profile-main">
          <section className="seller-hero-modern">
            <div>
              <span className="seller-eyebrow">Verified marketplace seller</span>
              <h2>{sellerName}</h2>
              <p>{user.bio ?? user.about ?? 'Selling quality local items with fast replies and safe pickup options.'}</p>
              <div className="seller-quick-stats">
                <span><b>{formatNumber(listings.length || user.listingCount)}</b><small>Active listings</small></span>
                <span><b>{rating(user.rating)}</b><small>Seller rating</small></span>
                <span><b>{formatNumber(user.soldCount)}</b><small>Sold items</small></span>
                <span><b>{user.memberSince ? '2026' : 'New'}</b><small>Member since</small></span>
              </div>
            </div>
            <div className="seller-hero-panel">
              <Icon name="shield" size={34} />
              <strong>Safe local buying</strong>
              <p>Use marketplace messages to keep records and meet in a public place.</p>
            </div>
          </section>

          <section className="seller-listings-panel">
            <div className="seller-panel-title">
              <div>
                <span>Seller inventory</span>
                <h3>Active listings</h3>
              </div>
              <Link href={`/search?sellerId=${user.id ?? id}`}>See all</Link>
            </div>
            {listings.length ? (
              <div className="seller-listings-grid">
                {listings.map((listing: any) => <ListingCard key={listing.id} listing={listing} variant="featured" />)}
              </div>
            ) : (
              <div className="seller-empty-listings"><Icon name="list" size={34} /><strong>No active listings yet</strong><p>Check back later for new items from this seller.</p></div>
            )}
          </section>
        </section>

        <aside className="seller-profile-right">
          <section className="seller-mini-panel">
            <h3>Seller score</h3>
            <div className="seller-score-circle"><strong>{rating(user.rating)}</strong><span>/ 5</span></div>
            <p>Based on reviews, listing quality, and marketplace activity.</p>
          </section>

          <section className="seller-mini-panel">
            <h3>Recent reviews</h3>
            {reviews.length ? reviews.slice(0, 3).map((review: any) => (
              <div className="seller-review-item" key={review.id ?? review.createdAt}>
                <strong><Icon name="star" size={13} /> {rating(review.rating)}</strong>
                <p>{review.comment ?? review.body ?? 'Great seller and smooth transaction.'}</p>
              </div>
            )) : (
              <div className="seller-review-item empty"><strong>No reviews yet</strong><p>This seller has not received public reviews.</p></div>
            )}
          </section>

          <section className="seller-mini-panel seller-contact-panel">
            <h3>Quick contact</h3>
            <p>Ask about availability, pickup location, and payment options.</p>
            <Link href={`/messages?seller=${user.id ?? id}`}>Start conversation</Link>
          </section>
        </aside>
      </section>
    </main>
  );
}
