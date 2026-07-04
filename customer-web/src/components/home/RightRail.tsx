'use client';

import Link from 'next/link';
import { Icon, categoryIcon } from '@/components/ui/Icon';
import { getCategoryRouteValue } from '@/constants/categories';
import type { CategoryDto, ListingDto } from '@/lib/api/apiClient';
import { ListingCard } from '@/components/listings/ListingCard';
import { AdvertisementCarousel } from '@/components/ads/AdvertisementCarousel';
import { useI18n } from '@/lib/i18n/client';

export function RightRail({ categories, promoted }: { categories: CategoryDto[]; promoted?: ListingDto }) {
  const { t, category } = useI18n();
  return (
    <aside className="right-rail">
      <section className="rail-panel">
        <div className="rail-panel-head"><h3>{t('promotedAds')}</h3><Link href="/search?promoted=true">{t('seeAll')}</Link></div>
        <AdvertisementCarousel placement="SIDEBAR" variant="rail" />
      </section>
      <section className="upgrade-modern"><strong>{t('upgradeYourListing')}</strong><p>{t('upgradeYourListingText')}</p><Link href="/billing">{t('upgradeNow')}</Link><span>🚀</span></section>
      <section className="rail-panel">
        <div className="rail-panel-head"><h3>{t('topCategories')}</h3><Link href="/search">{t('seeAll')}</Link></div>
        <div className="top-category-list">
          {categories.slice(0, 5).map((c) => { const code = c.code ?? c.slug ?? c.name; return <Link key={c.slug ?? c.id} href={`/search?category=${encodeURIComponent(getCategoryRouteValue(code))}`}><span><Icon name={categoryIcon(c.iconKey ?? code)} size={17} /></span><div><strong>{category(code)}</strong><small>{(c.count ?? 0).toLocaleString()} {t('listings')}</small></div></Link>; })}
        </div>
      </section>
      <section className="rail-panel safety-modern">
        <div className="rail-panel-head"><h3>{t('safetyTips')}</h3><Link href="/help">{t('seeAll')}</Link></div>
        <p><Icon name="community" size={17} /> {t('meetPublic')}</p>
        <p><Icon name="shield" size={17} /> {t('neverSharePersonal')}</p>
        <p><Icon name="star" size={17} /> {t('trustInstincts')}</p>
      </section>
    </aside>
  );
}
