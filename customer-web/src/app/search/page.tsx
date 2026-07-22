import Link from "next/link";
import { redirect } from "next/navigation";
import {
  marketplaceApi,
  type CategoryDto,
  type PagedListings,
} from "@/lib/api/apiClient";
import { ProgressiveListingResults } from "@/components/listings/ProgressiveListingResults";
import { CategoryBrowser } from "@/components/categories/CategoryBrowser";
import { Icon } from "@/components/ui/Icon";
import { AdvertisementCarousel } from "@/components/ads/AdvertisementCarousel";
import { T } from "@/components/i18n/T";
import { CategoryName } from "@/components/i18n/CategoryName";

type SearchParams = {
  q?: string;
  category?: string;
  page?: string;
  sort?: string;
};

const fallbackCategories: CategoryDto[] = [
  { id: "vehicles", code: "vehicles", iconKey: "vehicle", slug: "vehicles", count: 12540 },
  {
    id: "property_rentals",
    code: "property_rentals",
    iconKey: "rental",
    slug: "property-rentals",
    count: 8732,
  },
  { id: "for_sale", code: "for_sale", iconKey: "sale", slug: "for-sale", count: 15986 },
  { id: "jobs", code: "jobs", iconKey: "jobs", slug: "jobs", count: 6421 },
  { id: "services", code: "services", iconKey: "services", slug: "services", count: 4231 },
  { id: "electronics", code: "electronics", iconKey: "electronics", slug: "electronics", count: 866 },
];

export default async function SearchPage({
  searchParams,
}: {
  searchParams: Promise<SearchParams>;
}) {
  const params = await searchParams;
  const normalizedQuery = (params.q ?? "").trim().replace(/\s+/g, " ");

  if ((params.q ?? "") !== normalizedQuery) {
    const normalizedParams = new URLSearchParams();
    if (normalizedQuery) normalizedParams.set("q", normalizedQuery);
    if (params.category) normalizedParams.set("category", params.category);
    if (params.page && params.page !== "1") normalizedParams.set("page", params.page);
    if (params.sort) normalizedParams.set("sort", params.sort);
    redirect(`/search${normalizedParams.toString() ? `?${normalizedParams}` : ""}`);
  }

  let data: PagedListings = { items: [], totalItems: 0 };
  let categories: CategoryDto[] = [];
  try {
    [data, categories] = await Promise.all([
      marketplaceApi.listings({
        q: normalizedQuery || undefined,
        category: params.category,
        page: Number(params.page ?? 1),
        pageSize: 100,
        sort: params.sort,
      }),
      marketplaceApi.categories(),
    ]);
    if (data.items?.length)
      data.items = data.items.map((x) => ({
        ...x,
        isPinned: x.isPinned,
        isFeatured: x.isFeatured,
      }));
  } catch {
    categories = fallbackCategories;
  }
  if (!categories.length) categories = fallbackCategories;

  const currentCategory = categories.find((c) => (c.code ?? c.slug) === params.category || c.slug === params.category);

  // A single /listings request returns local results plus any supplemental
  // external results selected by the backend. Customer never calls eBay directly.
  const externalItems = data.external?.items ?? [];

  return (
    <main className="search-v2 shell-wide">
      <aside className="search-v2-left">
        <div className="search-left-card">
          <div className="search-card-title">
            <span>
              <Icon name="grid" size={18} />
            </span>
            <T k="browseCategories" />
          </div>
          <CategoryBrowser categories={categories} compact />
        </div>
        <div className="search-left-card">
          <div className="search-card-title">
            <span>
              <Icon name="filter" size={18} />
            </span>
            <T k="refineSearch" />
          </div>
          <div className="filter-stack">
            <label>
              <T k="priceRange" />
              <select defaultValue="">
                <option value="">
                  <T k="anyPrice" />
                </option>
                <option>$0 - $100</option>
                <option>$100 - $500</option>
                <option>$500+</option>
              </select>
            </label>
            <label>
              <T k="condition" />
              <select defaultValue="">
                <option value="">
                  <T k="anyCondition" />
                </option>
                <option>
                  <T k="new" />
                </option>
                <option>
                  <T k="likeNew" />
                </option>
                <option>
                  <T k="used" />
                </option>
              </select>
            </label>
            <label>
              <T k="postedFilter" />
              <select defaultValue="">
                <option value="">
                  <T k="anyTime" />
                </option>
                <option>
                  <T k="today" />
                </option>
                <option>
                  <T k="thisWeek" />
                </option>
                <option>
                  <T k="thisMonth" />
                </option>
              </select>
            </label>
          </div>
        </div>
        <div className="search-upgrade-card">
          <span>🚀</span>
          <strong>
            <T k="sellFaster" />
          </strong>
          <p>
            <T k="sellFasterText" />
          </p>
          <Link href="/billing">
            <T k="upgradeListing" />
          </Link>
        </div>
      </aside>

      <section className="search-v2-main">
        <div className="search-hero-card">
          <div>
            <span className="search-eyebrow">
              <T k="localMarketplace" />
            </span>
            <h1>
              {currentCategory ? (
                <CategoryName name={currentCategory.code ?? currentCategory.name} />
              ) : (
                <T k="findNeed" />
              )}
            </h1>
            <p>
              <T k="searchLocalText" />
            </p>
          </div>
          <form className="search-v2-form" action="/search">
            <label className="search-input-big">
              <Icon name="search" size={19} />
              <input
                name="q"
                defaultValue={normalizedQuery}
                placeholder={"Search listings, categories, cities..."}
              />
            </label>
            <select name="category" defaultValue={params.category ?? ""}>
              <option value="">
                <T k="allCategories" />
              </option>
              {categories.map((c) => (
                <option key={c.id} value={c.code ?? c.slug ?? c.id}>
                  <CategoryName name={c.code ?? c.name} />
                </option>
              ))}
            </select>
            <select name="sort" defaultValue={params.sort ?? "newest"}>
              <option value="newest">
                <T k="newest" />
              </option>
              <option value="price-low">
                <T k="priceLow" />
              </option>
              <option value="price-high">
                <T k="priceHigh" />
              </option>
              <option value="popular">
                <T k="mostPopular" />
              </option>
            </select>
            <button type="submit">
              <Icon name="search" size={18} /> <T k="search" />
            </button>
          </form>
        </div>

        <AdvertisementCarousel placement="HOME_HERO" variant="hero" />

        <div className="search-result-toolbar">
          <div>
            <strong>
              {data.totalItems || data.items.length} <T k="results" />
            </strong>
            <span>
              {normalizedQuery ? (
                <>{normalizedQuery}</>
              ) : currentCategory ? (
                <><T k="category" />: <CategoryName name={currentCategory.code ?? currentCategory.name} /></>
              ) : (
                <T k="nearYou" />
              )}
            </span>
          </div>
          <div className="search-toggle-tabs">
            <Link
              className="active"
              href={`/search${params.category ? `?category=${encodeURIComponent(params.category)}` : ""}`}
            >
              <Icon name="list" size={16} /> <T k="list" />
            </Link>
            <Link href="/map">
              <Icon name="map" size={16} /> <T k="map" />
            </Link>
          </div>
        </div>

        <ProgressiveListingResults
          localItems={data.items}
          externalItems={externalItems}
          initialCount={20}
          increment={20}
        />
        {!data.items.length && !externalItems.length && (
          <div className="empty-state-modern">
            <strong>
              <T k="noListingsFound" />
            </strong>
            <span>
              <T k="noListingsFoundText" />
            </span>
          </div>
        )}
      </section>

      <aside className="search-v2-right">
        <div className="rail-panel trend-panel">
          <div className="rail-panel-head">
            <strong>
              <T k="trendingNow" />
            </strong>
          </div>
          <Link href="/search?q=iphone">iPhone 15 Pro</Link>
          <Link href="/search?q=toyota">Toyota Camry</Link>
          <Link href="/search?q=macbook">MacBook Air</Link>
          <Link href="/search?q=apartment"><T k="apartmentRentals" /></Link>
        </div>
        <AdvertisementCarousel placement="SIDEBAR" variant="rail" titleKey="featuredAds" />
        <div className="rail-panel safety-panel-v2">
          <div className="rail-panel-head">
            <strong>
              <T k="safetyTips" />
            </strong>
          </div>
          <p>
            <T k="safetySearchText" />
          </p>
        </div>
      </aside>
    </main>
  );
}
