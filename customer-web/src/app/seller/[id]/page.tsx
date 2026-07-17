import Link from 'next/link';
import { apiClient } from '@/lib/api/apiClient';
import { ListingCard } from '@/components/listings/ListingCard';
import { Icon } from '@/components/ui/Icon';
import { T } from '@/components/i18n/T';

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
          <h1><T k="sellerNotFound" /></h1>
          <p><T k="sellerUnavailable" /></p>
          <Link href="/search"><T k="browseListings" /></Link>
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
        <Link href="/"><T k="home" /></Link>
        <span>/</span>
        <Link href="/search"><T k="marketplace" /></Link>
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
              <span>{formatNumber(user.reviewCount)} <T k="reviewsLower" /></span>
            </div>
            <div className="seller-cta-stack">
              <Link className="seller-primary-action" href={`/messages?seller=${user.id ?? id}`}><Icon name="message" size={18} /> <T k="messageSeller" /></Link>
              <Link className="seller-secondary-action" href={`/search?sellerId=${user.id ?? id}`}><Icon name="list" size={18} /> <T k="viewAllListings" /></Link>
            </div>
          </section>

          <section className="seller-trust-card">
            <div className="seller-section-head"><span><Icon name="shield" size={18} /></span><div><strong><T k="trustVerification" /></strong><small>{verifiedCount} <T k="verificationChecks" /></small></div></div>
            <div className="trust-list-modern">
              <span className={badges.phoneVerified ? 'ok' : ''}><Icon name="check" size={15} /> <T k="phoneVerifiedLabel" /></span>
              <span className={badges.emailVerified ? 'ok' : ''}><Icon name="check" size={15} /> <T k="emailVerifiedLabel" /></span>
              <span className={badges.businessVerified ? 'ok' : ''}><Icon name="check" size={15} /> <T k="businessVerifiedLabel" /></span>
              <span className={badges.identityVerified ? 'ok' : ''}><Icon name="check" size={15} /> <T k="identityVerifiedLabel" /></span>
            </div>
          </section>

          <section className="seller-safety-card">
            <h3><T k="safetyTips" /></h3>
            <p><T k="sellerSafetyText" /></p>
          </section>
        </aside>

        <section className="seller-profile-main">
          <section className="seller-hero-modern">
            <div>
              <span className="seller-eyebrow"><T k="verifiedMarketplaceSeller" /></span>
              <h2>{sellerName}</h2>
              <p>{user.bio ?? user.about ?? <T k="sellerDefaultBio" />}</p>
              <div className="seller-quick-stats">
                <span><b>{formatNumber(listings.length || user.listingCount)}</b><small><T k="activeListings" /></small></span>
                <span><b>{rating(user.rating)}</b><small><T k="sellerRating" /></small></span>
                <span><b>{formatNumber(user.soldCount)}</b><small><T k="soldItems" /></small></span>
                <span><b>{user.memberSince ? '2026' : 'New'}</b><small><T k="memberSince" /></small></span>
              </div>
            </div>
            <div className="seller-hero-panel">
              <Icon name="shield" size={34} />
              <strong><T k="safeLocalBuying" /></strong>
              <p><T k="safeLocalBuyingText" /></p>
            </div>
          </section>

          <section className="seller-listings-panel">
            <div className="seller-panel-title">
              <div>
                <span><T k="sellerInventory" /></span>
                <h3><T k="activeListings" /></h3>
              </div>
              <Link href={`/search?sellerId=${user.id ?? id}`}><T k="seeAll" /></Link>
            </div>
            {listings.length ? (
              <div className="seller-listings-grid">
                {listings.map((listing: any) => <ListingCard key={listing.id} listing={listing} variant="featured" />)}
              </div>
            ) : (
              <div className="seller-empty-listings"><Icon name="list" size={34} /><strong><T k="noActiveListingsYet" /></strong><p><T k="checkBackSellerListings" /></p></div>
            )}
          </section>
        </section>

        <aside className="seller-profile-right">
          <section className="seller-mini-panel">
            <h3><T k="sellerScore" /></h3>
            <div className="seller-score-circle"><strong>{rating(user.rating)}</strong><span>/ 5</span></div>
            <p><T k="sellerScoreText" /></p>
          </section>

          <section className="seller-mini-panel">
            <h3><T k="recentReviews" /></h3>
            {reviews.length ? reviews.slice(0, 3).map((review: any) => (
              <div className="seller-review-item" key={review.id ?? review.createdAt}>
                <strong><Icon name="star" size={13} /> {rating(review.rating)}</strong>
                <p>{review.comment ?? review.body ?? <T k="defaultReviewText" />}</p>
              </div>
            )) : (
              <div className="seller-review-item empty"><strong><T k="noReviewsYet" /></strong><p><T k="sellerNoPublicReviews" /></p></div>
            )}
          </section>

          <section className="seller-mini-panel seller-contact-panel">
            <h3><T k="quickContact" /></h3>
            <p><T k="quickContactText" /></p>
            <Link href={`/messages?seller=${user.id ?? id}`}><T k="startConversation" /></Link>
          </section>
        </aside>
      </section>
    </main>
  );
}
