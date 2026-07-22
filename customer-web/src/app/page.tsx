'use client';

import { useEffect, useMemo, useState } from 'react';
import { CustomerAccountSidebar } from '@/components/layout/CustomerAccountSidebar';
import { FeaturedListings } from '@/components/home/FeaturedListings';
import { FeedToolbar } from '@/components/home/FeedToolbar';
import { RightRail } from '@/components/home/RightRail';
import { AdvertisementCarousel } from '@/components/ads/AdvertisementCarousel';
import { ProgressiveListingResults } from '@/components/listings/ProgressiveListingResults';
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

const fallbackListings: ListingDto[] = [];
const emptyFeed: HomeFeed = { listings: [], featuredListings: [], recentListings: [], categories: [], external: null };

export default function HomePage() {
  const [feed, setFeed] = useState<HomeFeed>(emptyFeed);

  useEffect(() => {
    let cancelled = false;

    const loadNewest = async () => {
      try {
        const data = await marketplaceApi.home({ page: 1, pageSize: 100 });
        if (!cancelled) setFeed(data);
      } catch {
        if (!cancelled) setFeed({ ...emptyFeed, categories: fallbackCategories });
      }
    };

    const loadNearby = () => {
      if (!navigator.geolocation) {
        void loadNewest();
        return;
      }

      navigator.geolocation.getCurrentPosition(
        async (position) => {
          try {
            const data = await marketplaceApi.home({
              latitude: position.coords.latitude,
              longitude: position.coords.longitude,
              page: 1,
              pageSize: 100,
            });
            if (!cancelled) setFeed(data);
          } catch {
            await loadNewest();
          }
        },
        () => { void loadNewest(); },
        { enableHighAccuracy: false, timeout: 7000, maximumAge: 10 * 60 * 1000 }
      );
    };

    loadNearby();
    return () => { cancelled = true; };
  }, []);

  const categories = feed.categories?.length ? feed.categories : fallbackCategories;
  const allListings = [...(feed.featuredListings ?? []), ...(feed.recentListings ?? []), ...(feed.listings ?? [])];
  const listings = allListings.length ? Array.from(new Map(allListings.map(x => [x.id, x])).values()) : fallbackListings;
  const featured = (feed.featuredListings?.length ? feed.featuredListings : listings).slice(0, 3);
  const rows = useMemo(() => (feed.recentListings?.length ? feed.recentListings : listings), [feed.recentListings, listings]);
  const externalItems = feed.external?.items ?? [];

  return (
    <main className="market-home shell-wide">
      <CustomerAccountSidebar categories={categories} />
      <section className="main-feed">
        <AdvertisementCarousel placement="HOME_HERO" variant="hero" />
        {featured.length > 0 && <FeaturedListings items={featured} />}
        <FeedToolbar categories={categories} />
        <ProgressiveListingResults
          localItems={rows}
          externalItems={externalItems}
          initialCount={20}
          increment={20}
        />
        <AdvertisementCarousel placement="HOME_FEED" variant="inline" />
      </section>
      <RightRail categories={categories} promoted={listings[1] ?? listings[0]} />
    </main>
  );
}
