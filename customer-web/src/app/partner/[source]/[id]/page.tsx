import Link from 'next/link';
import { notFound } from 'next/navigation';
import { Icon } from '@/components/ui/Icon';
import { AffiliateButton } from '@/components/analytics/AffiliateButton';

type SearchParams = {
  title?: string;
  image?: string;
  price?: string;
  currency?: string;
  condition?: string;
  location?: string;
  seller?: string;
  url?: string;
  source?: string;
};

function providerName(value?: string) {
  const source = (value || 'Partner').trim();
  if (source.toLowerCase() === 'ebay') return 'eBay';
  if (source.toLowerCase() === 'walmart') return 'Walmart';
  return source || 'Partner';
}

function safePartnerUrl(value?: string) {
  if (!value) return null;
  try {
    const url = new URL(value);
    if (url.protocol !== 'https:' && url.protocol !== 'http:') return null;
    return url.toString();
  } catch {
    return null;
  }
}

function formattedPrice(value?: string, currency = 'USD') {
  if (!value) return 'View price on partner site';
  const number = Number(value);
  if (!Number.isFinite(number)) return value;
  try {
    return new Intl.NumberFormat('en-US', {
      style: 'currency',
      currency: currency || 'USD',
      maximumFractionDigits: 2,
    }).format(number);
  } catch {
    return `${number} ${currency || 'USD'}`;
  }
}

export default async function PartnerProductPage({
  params,
  searchParams,
}: {
  params: Promise<{ source: string; id: string }>;
  searchParams: Promise<SearchParams>;
}) {
  const route = await params;
  const query = await searchParams;
  const provider = providerName(query.source || route.source);
  const partnerUrl = safePartnerUrl(query.url);
  const title = query.title?.trim();

  if (!title || !partnerUrl) notFound();

  return (
    <main className="partner-detail-page shell-wide">
      <nav className="detail-breadcrumb-v2 partner-breadcrumb">
        <Link href="/">Home</Link><span>/</span>
        <Link href="/search">Search</Link><span>/</span>
        <b>{provider}</b>
      </nav>

      <section className="partner-detail-card">
        <div className="partner-detail-media">
          {query.image ? (
            <img src={query.image} alt={title} referrerPolicy="no-referrer" />
          ) : (
            <div className="partner-detail-image-fallback"><Icon name="image" size={52} /></div>
          )}
          <span className="external-source-badge partner-detail-badge">{provider}</span>
        </div>

        <article className="partner-detail-content">
          <span className="search-eyebrow">PARTNER MARKETPLACE</span>
          <h1>{title}</h1>
          <strong className="partner-detail-price">{formattedPrice(query.price, query.currency)}</strong>

          <div className="partner-detail-facts">
            <span><b>Condition</b>{query.condition || 'See partner listing'}</span>
            <span><b>Location</b>{query.location || `Available on ${provider}`}</span>
            {query.seller && <span><b>Seller</b>{query.seller}</span>}
            <span><b>Product ID</b>{route.id}</span>
          </div>

          <div className="partner-disclosure">
            <Icon name="shield" size={18} />
            <p>Vunoca displays this product from a partner marketplace. Price, availability, shipping, returns, and checkout are handled by {provider}.</p>
          </div>

          <AffiliateButton
            href={partnerUrl}
            provider={provider}
            itemId={route.id}
            title={title}
            price={query.price}
            currency={query.currency}
          />
          <Link className="partner-back-link" href="/search">Continue browsing Vunoca</Link>
        </article>
      </section>
    </main>
  );
}
