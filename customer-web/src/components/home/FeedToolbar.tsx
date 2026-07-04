'use client';

import Link from 'next/link';
import { getCategoryRouteValue } from '@/constants/categories';
import { Icon } from '@/components/ui/Icon';
import type { CategoryDto } from '@/lib/api/apiClient';
import { useI18n } from '@/lib/i18n/client';

export function FeedToolbar({ categories }: { categories: CategoryDto[] }) {
  const { t, category } = useI18n();
  return (
    <div className="feed-toolbar-modern">
      <div className="category-tabs-modern">
        <Link className="active" href="/search">{t('allListings')}</Link>
        {categories.slice(0, 5).map((c) => { const code = c.code ?? c.slug ?? c.name; return <Link key={c.slug ?? c.id} href={`/search?category=${encodeURIComponent(getCategoryRouteValue(code))}`}>{category(code)}</Link>; })}
      </div>
      <div className="feed-actions"><button type="button">{t('sortNewest')}</button><Link href="/map"><Icon name="map" size={18} /></Link><Link href="/search"><Icon name="filter" size={18} /></Link></div>
    </div>
  );
}
