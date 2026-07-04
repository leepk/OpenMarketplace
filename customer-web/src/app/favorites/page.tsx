'use client';
import Link from 'next/link';
import { useEffect, useState } from 'react';
import { AuthEmpty, useSessionUser } from '@/components/account/RequireAuth';
import { AccountShell } from '@/components/account/AccountShell';
import { marketplaceApi, type ListingDto } from '@/lib/api/apiClient';
import { ListingCard } from '@/components/listings/ListingCard';
import { Icon } from '@/components/ui/Icon';
import { useI18n } from '@/lib/i18n/client';

export default function FavoritesPage(){
  const { t } = useI18n();
  const { user, ready } = useSessionUser();
  const [items,setItems]=useState<ListingDto[]>([]); const [loading,setLoading]=useState(true);
  useEffect(()=>{ if(!user?.id){setLoading(false);return;} let off=false; marketplaceApi.favorites(user.id).then(r=>!off&&setItems(r.items??[])).catch(()=>!off&&setItems([])).finally(()=>!off&&setLoading(false)); return()=>{off=true};},[user?.id]);
  if(!ready) return null;
  if(!user) return <AuthEmpty title={t('savedListings')} text={t('savedAuthText')} />;
  return <AccountShell user={user} title={t('savedListings')} subtitle={t('savedAuthText')}>
    {loading ? <div className="empty-state-v3">{t('loadingSavedListings')}</div> : null}
    {!loading && !items.length ? <div className="empty-state-v3"><Icon name="heart" size={34}/><strong>{t('noSavedListings')}</strong><p>{t('saveHint')}</p><Link className="primary-button" href="/search">{t('browseListings')}</Link></div> : null}
    <div className="saved-grid-v3">{items.map(l=><ListingCard key={l.id} listing={l}/>)}</div>
  </AccountShell>;
}
