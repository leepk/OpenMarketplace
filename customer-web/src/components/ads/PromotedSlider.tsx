'use client';

import { ListingCard, type ListingCardData } from '@/components/listings/ListingCard';
import { Icon } from '@/components/ui/Icon';
import { useI18n } from '@/lib/i18n/client';

export function PromotedSlider({ items = [], titleKey, title }: { items?: ListingCardData[]; titleKey?: string; title?: string }) {
  const { t } = useI18n();
  const list = items.filter(Boolean).slice(0, 6);
  if (!list.length) return null;
  return (
    <section className="promoted-slider-modern">
      <div className="promoted-slider-head">
        <div>
          <span>{t('sponsored')}</span>
          <h2>{titleKey ? t(titleKey) : (title ?? t('promotedAds'))}</h2>
        </div>
        <a href="/billing"><Icon name="rocket" size={16} /> {t('promoteYours')}</a>
      </div>
      <div className="promoted-slider-track">
        {list.map((item, index) => (
          <div className="promoted-slide" key={item.id ?? index}>
            <ListingCard listing={{ ...item, isPinned: true }} variant="mini" />
          </div>
        ))}
      </div>
    </section>
  );
}
