'use client';

import { useI18n } from '@/lib/i18n/client';

export type NotificationItem = { id?: string; title?: string; body?: string; type?: string; isRead?: boolean; createdAt?: string };

function iconFor(type?: string) {
  const key = (type ?? '').toLowerCase();
  if (key.includes('message')) return '✉';
  if (key.includes('payment') || key.includes('billing')) return '$';
  if (key.includes('listing')) return '▤';
  if (key.includes('security')) return '🛡';
  return '🔔';
}

function timeText(value?: string) {
  if (!value) return 'Just now';
  const dt = new Date(value);
  if (Number.isNaN(dt.getTime())) return value;
  return dt.toLocaleDateString(undefined, { month: 'short', day: 'numeric' });
}

export function NotificationCenter({ items, unread }: { items: NotificationItem[]; unread: number }) {
  const { t } = useI18n();
  const safeItems = items?.length ? items : [
    { id: 'demo-1', title: t('listingSubmitted'), body: t('readyForReviewText'), type: t('listingsTab'), isRead: false },
    { id: 'demo-2', title: t('messages'), body: t('msgDemo1'), type: t('messages'), isRead: false },
    { id: 'demo-3', title: t('receipt'), body: t('billingSubtitle'), type: t('payments'), isRead: true },
  ];
  return (
    <div className="notification-shell app-container">
      <div className="notification-header-card">
        <div><h1>{t('notifications')}</h1><p>{t('stayUpdated')}</p></div>
        <strong>{unread || safeItems.filter((x) => !x.isRead).length} {t('unread')}</strong>
      </div>
      <div className="notification-tabs"><span className="active">{t('all')}</span><span>{t('listingsTab')}</span><span>{t('messages')}</span><span>{t('payments')}</span><span>{t('security')}</span></div>
      <div className="notification-list">
        {safeItems.map((n) => (
          <article className={`notification-item ${n.isRead ? '' : 'unread'}`} key={n.id ?? n.title}>
            <div className="notification-icon">{iconFor(n.type)}</div>
            <div>
              <div className="notification-title-row"><strong>{n.title}</strong><span className="badge muted">{n.type ?? t('notification')}</span></div>
              <p>{n.body}</p>
            </div>
            <span className="notification-time">{timeText(n.createdAt)}</span>
          </article>
        ))}
      </div>
    </div>
  );
}
