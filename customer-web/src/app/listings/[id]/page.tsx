import Link from "next/link";
import { Icon } from "@/components/ui/Icon";
import { marketplaceApi } from "@/lib/api/apiClient";
import { mediaUrl } from "@/lib/media/url";
import { ContactSellerCard } from "@/components/detail/ContactSellerCard";
import { AdvertisementCarousel } from "@/components/ads/AdvertisementCarousel";
import { T } from "@/components/i18n/T";
import { CategoryName } from "@/components/i18n/CategoryName";
import { PackageName } from "@/components/i18n/PackageName";

function formatPrice(price: unknown) {
  const value = typeof price === "number" ? price : Number(price);
  if (!price && value !== 0) return "Contact";
  if (Number.isNaN(value)) return String(price ?? "Contact");
  return new Intl.NumberFormat("en-US", {
    style: "currency",
    currency: "USD",
    maximumFractionDigits: 0,
  }).format(value);
}

function getCoord(value: unknown) {
  const n = typeof value === "number" ? value : Number(value);
  return Number.isFinite(n) ? n : null;
}

export default async function Page({
  params,
}: {
  params: Promise<{ id: string }>;
}) {
  const { id } = await params;
  const data = await marketplaceApi.listing(id);
  const listing = data.listing;
  let related: any[] = [];
  try {
    related = (
      await marketplaceApi.listings({
        category: listing.categoryName ?? undefined,
        pageSize: 8,
      })
    ).items.filter((x) => x.id !== listing.id);
  } catch {}
  const images =
    (data.media?.map((m) => mediaUrl(m.url)).filter(Boolean) as string[]) ?? [];
  const mainImage = images[0] ?? mediaUrl(listing.imageUrl ?? null);
  const gallery = mainImage
    ? [mainImage, ...images.filter((x) => x !== mainImage)].slice(0, 6)
    : images.slice(0, 6);
  const seller = data.seller;
  const lat = getCoord((listing as any).latitude ?? (listing as any).lat);
  const lng = getCoord((listing as any).longitude ?? (listing as any).lng);
  const publicLocation = (listing as any).location ?? [((listing as any).city), ((listing as any).state), ((listing as any).postalCode)].filter(Boolean).join(', ');
  const destination = lat !== null && lng !== null ? `${lat},${lng}` : publicLocation;
  const directionsUrl = `https://www.google.com/maps/dir/?api=1&destination=${encodeURIComponent(destination || '')}`;
  const packageCode = ((listing as any).packageCode ?? (listing.isPinned ? 'PREMIUM' : listing.isUrgent ? 'URGENT' : listing.isFeatured ? 'FEATURED' : 'FREE')) as string;
  const packageStatus = ((listing as any).packageStatus ?? 'Active') as string;
  const packageActive = packageStatus.toLowerCase().includes('active');

  return (
    <main className="listing-detail-v2 shell-wide">
      <section className="detail-v2-main">
        <nav className="detail-breadcrumb-v2">
          <Link href="/">
            <T k="homeBreadcrumb" />
          </Link>
          <span>/</span>
          <Link href="/search">
            <T k="listingBreadcrumb" />
          </Link>
          <span>/</span>
          <b>
            {(listing.categoryName ?? data.category?.name) ? (
              <CategoryName
                name={listing.categoryName ?? data.category?.name}
              />
            ) : (
              <T k="detailBreadcrumb" />
            )}
          </b>
        </nav>
        <section className="detail-gallery-v2">
          <div className="detail-hero-image-v2">
            {mainImage ? (
              <img src={mainImage} alt={listing.title} />
            ) : (
              <div className="detail-generated-art">
                <Icon name="category" size={48} />
              </div>
            )}
            <div className="detail-gallery-badges">
              {packageActive && packageCode === 'FEATURED' && (
                <span className="badge badge-featured">
                  <Icon name="star" size={12} /> <PackageName code={packageCode} />
                </span>
              )}
              {packageActive && packageCode === 'PREMIUM' && (
                <span className="badge badge-premium">
                  <PackageName code={packageCode} />
                </span>
              )}
              {packageActive && packageCode === 'URGENT' && (
                <span className="badge badge-urgent">
                  <PackageName code={packageCode} />
                </span>
              )}
              {!packageActive && packageCode !== 'FREE' && (
                <span className="badge badge-pending"><T k="packagePending" /></span>
              )}
            </div>
          </div>
          <div className="detail-thumbs-v2">
            {gallery.slice(1, 5).map((url) => (
              <img key={url} src={url} alt="" />
            ))}
            {!gallery.slice(1).length && (
              <>
                <span />
                <span />
                <span />
                <span />
              </>
            )}
          </div>
        </section>
        <article className="detail-content-card-v2">
          <div className="detail-title-row-v2">
            <div>
              <span className="detail-category-v2">
                {(listing.categoryName ?? data.category?.name) ? (
                  <CategoryName
                    name={listing.categoryName ?? data.category?.name}
                  />
                ) : (
                  <T k="marketplace" />
                )}
              </span>
              <h1>{listing.title}</h1>
            </div>
            <button className="detail-save-v2">
              <Icon name="heart" size={19} /> <T k="save" />
            </button>
          </div>
          <strong className="detail-price-v2">
            {formatPrice(listing.price)}
          </strong>
          <div className="detail-meta-v2">
            <span>
              <Icon name="pin" size={16} />{" "}
              {listing.location ?? <T k="locationDefault" />}
            </span>
            <span>
              <Icon name="eye" size={16} /> {listing.viewCount ?? 0}{" "}
              <T k="viewsLower" />
            </span>
            <span>
              <Icon name="heart" size={16} /> {listing.favoriteCount ?? 0}{" "}
              <T k="saved" />
            </span>
          </div>
          {packageCode !== 'FREE' && (
            <div className={`detail-package-status-v2 ${packageActive ? 'active' : 'pending'}`}>
              <span><Icon name="tag" size={18} /></span>
              <div>
                <b><PackageName code={packageCode} /></b>
                <small>{packageActive ? <T k="packageActive" /> : <T k="packagePending" />}</small>
              </div>
            </div>
          )}
          <div className="detail-section-v2">
            <h3>
              <T k="description" />
            </h3>
            <p>{listing.description ?? <T k="noDescription" />}</p>
          </div>
          <div className="detail-stats-grid-v2">
            <span>
              <b>{listing.viewCount ?? 0}</b>
              <small>
                <T k="views" />
              </small>
            </span>
            <span>
              <b>{listing.favoriteCount ?? 0}</b>
              <small>
                <T k="saves" />
              </small>
            </span>
            <span>
              <b>{listing.commentCount ?? 0}</b>
              <small>
                <T k="comments" />
              </small>
            </span>
            <span>
              <b>{seller?.rating ?? "—"}</b>
              <small>
                <T k="sellerRating" />
              </small>
            </span>
          </div>
          <div className="detail-section-v2">
            <h3>
              <T k="details" />
            </h3>
            <div className="detail-spec-grid">
              <span>
                <b>
                  <T k="category" />
                </b>
                {(listing.categoryName ?? data.category?.name) ? (
                  <CategoryName
                    name={listing.categoryName ?? data.category?.name}
                  />
                ) : (
                  "—"
                )}
              </span>
              <span>
                <b>
                  <T k="status" />
                </b>
                {listing.status ?? <T k="statusActive" />}
              </span>
              <span>
                <b><T k="packageStep" /></b>
                <PackageName code={packageCode} />
              </span>
              <span>
                <b>
                  <T k="posted" />
                </b>
                {listing.createdAt ? <T k="recently" /> : "—"}
              </span>
              <span>
                <b>
                  <T k="seller" />
                </b>
                {seller?.name ?? <T k="localSeller" />}
              </span>
            </div>
          </div>
          <div className="detail-section-v2 safety-box-v2">
            <h3>
              <T k="safetyTips" />
            </h3>
            <p>
              <Icon name="shield" size={17} /> <T k="safetySearchText" />
            </p>
          </div>
        </article>
        <AdvertisementCarousel placement="LISTING_DETAIL" variant="wide" titleKey="morePromotedListings" />
      </section>
      <aside className="detail-v2-rail">
        <AdvertisementCarousel placement="SIDEBAR" variant="rail" />
        <ContactSellerCard
          listingId={listing.id}
          seller={seller}
          listingLocation={listing.location}
        />
        <section className="detail-map-card-v2">
          <h3>
            <T k="location" />
          </h3>
          <div className="mini-map-v2 listing-location-map-v2">
            <Icon name="map" size={32} />
            <span>{publicLocation || <T k="locationNotAvailable" />}</span>
            {lat !== null && lng !== null ? <small>{lat.toFixed(4)}, {lng.toFixed(4)}</small> : null}
          </div>
          <div className="detail-map-actions-v2">
            <Link href={`/map?listing=${listing.id}`}>
              <T k="openInMap" />
            </Link>
            <a href={directionsUrl} target="_blank" rel="noreferrer">
              Directions
            </a>
          </div>
        </section>
      </aside>
    </main>
  );
}
