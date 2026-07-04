import { CustomerAccountSidebar } from '@/components/layout/CustomerAccountSidebar';
import { FeaturedListings } from '@/components/home/FeaturedListings';
import { FeedToolbar } from '@/components/home/FeedToolbar';
import { RightRail } from '@/components/home/RightRail';
import { AdvertisementCarousel } from '@/components/ads/AdvertisementCarousel';
import { ListingCard } from '@/components/listings/ListingCard';
import { marketplaceApi, type CategoryDto, type HomeFeed, type ListingDto } from '@/lib/api/apiClient';

const fallbackCategories: CategoryDto[] = [
  { id: 'vehicles', name: 'Vehicles', slug: 'vehicles', count: 12540 },
  { id: 'property-rentals', name: 'Property Rentals', slug: 'property-rentals', count: 8732 },
  { id: 'for-sale', name: 'For Sale', slug: 'for-sale', count: 15986 },
  { id: 'jobs', name: 'Jobs', slug: 'jobs', count: 6421 },
  { id: 'services', name: 'Services', slug: 'services', count: 4231 },
  { id: 'electronics', name: 'Electronics', slug: 'electronics', count: 866 },
  { id: 'home-garden', name: 'Home & Garden', slug: 'home-garden', count: 746 },
  { id: 'community', name: 'Community', slug: 'community', count: 664 },
];

const fallbackListings: ListingDto[] = [
  { id: 'sample-home', title: 'Modern 4BR Home in Willow Glen', price: 1295000, location: 'San Jose, CA', categoryName: 'Property Rentals', isFeatured: true, isPinned: true, sellerVerified: true, description: 'Spacious, bright family home with premium finishes and a quiet neighborhood.', viewCount: 2450, favoriteCount: 28, commentCount: 12 },
  { id: 'sample-bmw', title: '2024 BMW X5 xDrive40i — Low Miles', price: 49900, location: 'San Jose, CA', categoryName: 'Vehicles', isFeatured: true, isPinned: true, description: 'Excellent condition, low miles, one owner. Clean title.', viewCount: 3120, favoriteCount: 42, commentCount: 15 },
  { id: 'sample-iphone', title: 'iPhone 15 Pro Max 256GB', price: 950, location: 'San Jose, CA', categoryName: 'Electronics', isFeatured: true, description: 'Battery 98%. Comes with original box and charger.', viewCount: 1856, favoriteCount: 23, commentCount: 9 },
  { id: 'sample-macbook', title: 'MacBook Air M2 13” — Like New', price: 750, location: 'Sunnyvale, CA', categoryName: 'Electronics', isUrgent: true, description: 'Like new condition. Used 3 months only. Comes with charger and original box.', viewCount: 320, favoriteCount: 8, commentCount: 2 },
  { id: 'sample-sofa', title: 'Sectional Sofa — Excellent Condition', price: 450, location: 'Campbell, CA', categoryName: 'Home & Garden', isPinned: true, description: 'Very comfortable gray sectional sofa. No stains or tears.', viewCount: 210, favoriteCount: 5, commentCount: 1 },
  { id: 'sample-bike', title: 'Trek Marlin 6 Mountain Bike', price: 400, location: 'Sunnyvale, CA', categoryName: 'Sports & Outdoors', sellerVerified: true, description: 'Great condition. Size M/L. Perfect for trails and commuting.', viewCount: 180, favoriteCount: 3, commentCount: 0 },
];

async function getHomeFeed(): Promise<HomeFeed> {
  try { return await marketplaceApi.home(); }
  catch { return { listings: fallbackListings, featuredListings: fallbackListings.slice(0, 3), recentListings: fallbackListings.slice(3), categories: fallbackCategories }; }
}

export default async function HomePage() {
  const feed = await getHomeFeed();
  const categories = feed.categories?.length ? feed.categories : fallbackCategories;
  const allListings = [...(feed.featuredListings ?? []), ...(feed.recentListings ?? []), ...(feed.listings ?? [])];
  const listings = allListings.length ? Array.from(new Map(allListings.map(x => [x.id, x])).values()) : fallbackListings;
  const featured = (feed.featuredListings?.length ? feed.featuredListings : listings).slice(0, 3);
  const rows = (feed.recentListings?.length ? feed.recentListings : listings.slice(3)).slice(0, 6);

  return (
    <main className="market-home shell-wide">
      <CustomerAccountSidebar categories={categories} />
      <section className="main-feed">
        <AdvertisementCarousel placement="HOME_HERO" variant="hero" />
        <FeaturedListings items={featured} />
        <FeedToolbar categories={categories} />
        <div className="listing-feed-list">
          {rows.map((listing, idx) => <ListingCard key={listing.id ?? idx} listing={listing} variant="row" />)}
          <AdvertisementCarousel placement="HOME_FEED" variant="inline" />
        </div>
      </section>
      <RightRail categories={categories} promoted={listings[1] ?? listings[0]} />
    </main>
  );
}
