'use client';
import Link from 'next/link';
import { useEffect, useState } from 'react';
import { AuthEmpty, useSessionUser } from '@/components/account/RequireAuth';
import { AccountShell } from '@/components/account/AccountShell';
import { marketplaceApi } from '@/lib/api/apiClient';
import { Icon } from '@/components/ui/Icon';
import { useI18n } from '@/lib/i18n/client';

export default function NotificationsPage(){
  const { t } = useI18n();
  const { user, ready } = useSessionUser();
  const [data,setData]=useState<any>({items:[],unread:0}); const [loading,setLoading]=useState(true);
  useEffect(()=>{ if(!user?.id){setLoading(false);return;} let off=false; marketplaceApi.notifications(user.id).then(r=>!off&&setData(r)).catch(()=>!off&&setData({items:[],unread:0})).finally(()=>!off&&setLoading(false)); return()=>{off=true};},[user?.id]);
  if(!ready) return null;
  if(!user) return <AuthEmpty title={t('notifications')} text={t('notificationsAuthText')} />;
  return <AccountShell user={user} title={t('notifications')} subtitle={`${data.unread??0} ${(data.unread??0)===1?t('unreadUpdate'):t('unreadUpdates')}`}>
    <div className="notification-panel-v3">
      <div className="account-tabs-v3"><button className="active">{t('all')}</button><button>{t('unread')}</button><button>{t('listings')}</button><button>{t('messages')}</button><button>{t('paymentsInvoices')}</button></div>
      {loading?<div className="empty-state-v3">{t('loadingNotifications')}</div>:null}
      {!loading && !(data.items??[]).length?<div className="empty-state-v3"><Icon name="bell" size={34}/><strong>{t('noNotifications')}</strong><p>{t('activityHere')}</p><Link className="primary-button" href="/search">{t('browseMarketplace')}</Link></div>:null}
      {(data.items??[]).map((n:any)=><a className={`notification-row-v3 ${n.isRead?'':'unread'}`} href={n.url??'#'} key={n.id}><span><Icon name={n.type==='Message'?'message':n.type==='Payment'?'card':'bell'} size={18}/></span><div><strong>{n.title??t('notification')}</strong><p>{n.body??''}</p><small>{n.createdAt?new Date(n.createdAt).toLocaleString():''}</small></div></a>)}
    </div>
  </AccountShell>;
}
