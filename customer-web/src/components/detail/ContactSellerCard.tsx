'use client';

import Link from 'next/link';
import { Icon } from '@/components/ui/Icon';
import { MessageSellerForm } from '@/components/detail/MessageSellerForm';
import { useI18n } from '@/lib/i18n/client';

export function ContactSellerCard({ listingId, seller, listingLocation }: { listingId: string; seller?: any; listingLocation?: string | null }) {
  const { t } = useI18n();
  const initials = seller?.name ? String(seller.name).slice(0, 2).toUpperCase() : 'SE';
  return (
    <section className="contact-seller-card-modern">
      <div className="contact-seller-top">
        <div className="contact-seller-avatar">{initials}</div>
        <div>
          <span className="seller-eyebrow-small">{t('contact')} {t('seller').toLowerCase()}</span>
          <h3>{seller?.name ?? t('localSeller')}</h3>
          <p><Icon name="pin" size={13} /> {seller?.location ?? listingLocation ?? t('locationDefault')}</p>
        </div>
      </div>
      <div className="seller-proof-grid">
        <span><b><Icon name="star" size={14} /> {seller?.rating ?? '—'}</b><small>{t('rating')}</small></span>
        <span><b>{seller?.reviewCount ?? 0}</b><small>{t('reviews')}</small></span>
        <span><b>{seller?.trustScore ?? 80}%</b><small>{t('trust')}</small></span>
      </div>
      <div className="seller-verify-row">
        <span className={seller?.emailVerified ? 'ok' : ''}><Icon name="mail" size={13}/> {t('email')}</span>
        <span className={seller?.phoneVerified ? 'ok' : ''}><Icon name="phone" size={13}/> {t('phone')}</span>
        <span className={seller?.idVerified ? 'ok' : ''}><Icon name="shield" size={13}/> {t('identity')}</span>
      </div>
      <MessageSellerForm listingId={listingId} />
      <Link className="seller-profile-link-modern" href={seller?.id ? `/seller/${seller.id}` : '/profile'}>
        {t('viewPublicProfile')} <Icon name="arrowRight" size={15} />
      </Link>
    </section>
  );
}
