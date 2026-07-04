'use client';
import Link from 'next/link';
import { useEffect, useMemo, useState } from 'react';
import { AuthEmpty, useSessionUser } from '@/components/account/RequireAuth';
import { AccountShell } from '@/components/account/AccountShell';
import { marketplaceApi, type ListingDto } from '@/lib/api/apiClient';
import { ListingCard } from '@/components/listings/ListingCard';
import { Icon } from '@/components/ui/Icon';
import { useI18n } from '@/lib/i18n/client';
import { PackageName } from '@/components/i18n/PackageName';

export default function MyListingsPage(){
  const { t } = useI18n();
  const { user, ready } = useSessionUser();
  const [items, setItems] = useState<ListingDto[]>([]);
  const [loading, setLoading] = useState(true);
  useEffect(()=>{ if(!user?.id){setLoading(false);return;} let off=false; marketplaceApi.myListings(user.id).then(r=>!off&&setItems(r.items??[])).catch(()=>!off&&setItems([])).finally(()=>!off&&setLoading(false)); return()=>{off=true};},[user?.id]);
  const groups = useMemo(()=>({ active: items.filter(x=>x.status==='Published'), pending: items.filter(x=>x.status==='Pending'||x.moderationStatus==='Pending'), sold: items.filter(x=>x.status==='Sold'), all: items }),[items]);
  if(!ready) return null;
  if(!user) return <AuthEmpty title={t('myListings')} text={t('myListingsAuthText')} />;
  return <AccountShell user={user} title={t('myListings')} subtitle={t('myListingsSubtitle')} action={<Link href="/post" className="primary-button"><Icon name="plus" size={16}/> {t('postNewAd')}</Link>}>
    <div className="metric-grid-v3"><span><b>{groups.active.length}</b><small>{t('active')}</small></span><span><b>{groups.pending.length}</b><small>{t('pending')}</small></span><span><b>{groups.sold.length}</b><small>{t('sold')}</small></span><span><b>{items.reduce((s,x)=>s+(x.viewCount??0),0)}</b><small>{t('totalViews')}</small></span></div>
    <div className="account-tabs-v3"><button className="active">{t('all')} ({groups.all.length})</button><button>{t('active')} ({groups.active.length})</button><button>{t('pending')} ({groups.pending.length})</button><button>{t('sold')} ({groups.sold.length})</button></div>
    {loading ? <div className="empty-state-v3">{t('loadingYourListings')}</div> : null}
    {!loading && !items.length ? <div className="empty-state-v3"><Icon name="list" size={34}/><strong>{t('noListingsYet')}</strong><p>{t('firstItemHint')}</p><Link className="primary-button" href="/post">{t('postListing')}</Link></div> : null}
    <div className="account-list-v3">{items.map(l=><div className="account-row-card-v3" key={l.id}><ListingCard listing={l} variant="row"/><div className="row-actions-v3"><span className={`status-pill-v3 ${String(l.moderationStatus??l.status).toLowerCase()}`}>{l.moderationStatus??l.status}</span>{l.packageCode && l.packageCode !== 'FREE' ? <span className={`status-pill-v3 ${String(l.packageStatus ?? 'Active').toLowerCase()}`}><PackageName code={l.packageCode} /> · {(l.packageStatus ?? 'Active')}</span> : null}<Link className="row-action-view-v3" href={`/listings/${l.id}`}>{t('view')}</Link><button className="row-action-promote-v3" type="button">{t('promote')}</button></div></div>)}</div>
  </AccountShell>;
}
