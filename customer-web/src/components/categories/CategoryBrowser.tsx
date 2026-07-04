'use client';

import Link from 'next/link';
import { Icon, categoryIcon } from '@/components/ui/Icon';
import { getCategoryRouteValue } from '@/constants/categories';
import type { CategoryDto } from '@/lib/api/apiClient';
import { useI18n } from '@/lib/i18n/client';

export function CategoryBrowser({ categories, compact = false }: { categories: CategoryDto[]; compact?: boolean }) {
  const { t, category } = useI18n();
  const list = categories.slice(0, compact ? 8 : 12);
  return (
    <div className={compact ? 'category-browser left-menu-categories compact' : 'category-browser'}>
      {list.map((c) => {
        const categoryCode = c.code ?? c.slug ?? c.name;
        const slug = getCategoryRouteValue(categoryCode);
        return (
          <Link key={c.id ?? slug} href={`/search?category=${encodeURIComponent(slug)}`} className="category-browser-item">
            <span className="category-browser-icon"><Icon name={categoryIcon(c.iconKey ?? categoryCode)} size={compact ? 17 : 22} /></span>
            <span className="category-browser-copy">
              <strong>{category(categoryCode)}</strong>
              <small>{(c.count ?? 0).toLocaleString()} {t('listings')}</small>
            </span>
          </Link>
        );
      })}
    </div>
  );
}
