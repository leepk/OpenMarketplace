'use client';

import Link from 'next/link';
import { MouseEvent, useEffect, useMemo, useState } from 'react';
import { AuthEmpty, useSessionUser } from '@/components/account/RequireAuth';
import { AccountShell } from '@/components/account/AccountShell';
import { marketplaceApi } from '@/lib/api/apiClient';
import { Icon } from '@/components/ui/Icon';
import { useI18n } from '@/lib/i18n/client';
import { mediaUrl } from '@/lib/media/url';

type NotificationTab = 'All' | 'Unread' | 'Listing' | 'Message' | 'Payment';

function iconName(type?: string) {
  const key = (type ?? '').toLowerCase();
  if (key.includes('message')) return 'message';
  if (key.includes('payment') || key.includes('billing')) return 'card';
  if (key.includes('listing')) return 'tag';
  return 'bell';
}

function tabLabel(tab: NotificationTab, t: (key: any) => string) {
  if (tab === 'All') return t('all');
  if (tab === 'Unread') return t('unread');
  if (tab === 'Listing') return t('listings');
  if (tab === 'Message') return t('messages');
  return t('paymentsInvoices');
}

export default function NotificationsPage(){
  const { t } = useI18n();
  const { user, ready } = useSessionUser();
  const [data,setData]=useState<any>({items:[],unread:0,totalItems:0});
  const [loading,setLoading]=useState(true);
  const [activeTab,setActiveTab]=useState<NotificationTab>('All');

  const params = useMemo(() => ({
    type: activeTab === 'All' || activeTab === 'Unread' ? undefined : activeTab,
    unreadOnly: activeTab === 'Unread'
  }), [activeTab]);

  const load = () => {
    if(!user?.id){setLoading(false);return;}
    setLoading(true);
    marketplaceApi.notifications(user.id, params)
      .then(r=>setData(r))
      .catch(()=>setData({items:[],unread:0,totalItems:0}))
      .finally(()=>setLoading(false));
  };

  useEffect(()=>{ load(); },[user?.id, params.type, params.unreadOnly]);

  async function markAllRead(){
    if(!user?.id) return;
    await marketplaceApi.markAllNotificationsRead(user.id).catch(()=>null);
    load();
    window.dispatchEvent(new Event('om-session-changed'));
  }

  async function openNotification(n:any){
    if(!user?.id || n.isRead) return;
    await marketplaceApi.markNotificationRead(n.id, user.id).catch(()=>null);
    window.dispatchEvent(new Event('om-session-changed'));
  }

  async function removeNotification(e: MouseEvent<HTMLButtonElement>, n:any){
    e.preventDefault();
    e.stopPropagation();
    if(!user?.id) return;
    await marketplaceApi.deleteNotification(n.id, user.id).catch(()=>null);
    load();
    window.dispatchEvent(new Event('om-session-changed'));
  }

  if(!ready) return null;
  if(!user) return <AuthEmpty title={t('notifications')} text={t('notificationsAuthText')} />;

  const tabs: NotificationTab[] = ['All','Unread','Listing','Message','Payment'];
  const items = data.items ?? [];

  return <AccountShell user={user} title={t('notifications')} subtitle={`${data.unread??0} ${(data.unread??0)===1?t('unreadUpdate'):t('unreadUpdates')}`}>
    <div className="notification-panel-v3">
      <div className="notification-toolbar-v3">
        <div className="account-tabs-v3">
          {tabs.map((tab)=><button key={tab} type="button" className={activeTab===tab?'active':''} onClick={()=>setActiveTab(tab)}>{tabLabel(tab,t)}</button>)}
        </div>
        <button className="mark-read-v3" type="button" onClick={markAllRead} disabled={(data.unread??0)<=0}>{t('markAllRead') || 'Mark all read'}</button>
      </div>
      {loading?<div className="empty-state-v3">{t('loadingNotifications')}</div>:null}
      {!loading && !items.length?<div className="empty-state-v3"><Icon name="bell" size={34}/><strong>{t('noNotifications')}</strong><p>{t('activityHere')}</p><Link className="primary-button" href="/search">{t('browseMarketplace')}</Link></div>:null}
      {items.map((n:any)=>{
        const image = mediaUrl(n.imageUrl);
        return <Link onClick={()=>openNotification(n)} className={`notification-row-v3 ${n.isRead?'':'unread'}`} href={n.url??'#'} key={n.id}>
          <span>{image ? <img src={image} alt="" /> : <Icon name={iconName(n.type)} size={18}/>}</span>
          <div>
            <strong>{n.title??t('notification')}</strong>
            <p>{n.body??''}</p>
            <small>{n.createdAt?new Date(n.createdAt).toLocaleString():''}</small>
          </div>
          <button type="button" aria-label={t('removeNotification')} onClick={(e)=>removeNotification(e,n)}>×</button>
        </Link>;
      })}
    </div>
  </AccountShell>;
}
