'use client';

import { useMemo, useState } from 'react';
import { Icon } from '@/components/ui/Icon';
import { marketplaceApi } from '@/lib/api/apiClient';
import { useI18n } from '@/lib/i18n/client';
import { analytics } from '@/lib/analytics';

export function MessageSellerForm({ listingId }: { listingId: string }) {
  const { t } = useI18n();
  const quickMessages = useMemo(() => [t('msgDemo1'), t('msgDemo3'), t('searchThisArea')], [t]);
  const [message, setMessage] = useState(quickMessages[0]);
  const [status, setStatus] = useState('');

  async function send() {
    if (!message.trim()) return;
    setStatus(t('processing'));
    try {
      await marketplaceApi.messageSeller(listingId, message.trim());
      analytics.contactSeller({ listing_id: listingId, method: 'message' });
      setStatus(`${t('messages')} ${t('success').toLowerCase()}.`);
    } catch (err: any) {
      setStatus(err?.message ?? t('saveFailed'));
    }
  }

  return (
    <div className="seller-message-modern">
      <div className="quick-message-row">
        {quickMessages.map((item) => (
          <button key={item} type="button" className={message === item ? 'active' : ''} onClick={() => setMessage(item)}>{item}</button>
        ))}
      </div>
      <div className="message-compose-bar">
        <input value={message} onChange={(e) => setMessage(e.target.value)} placeholder={t('typeMessage')} />
        <button type="button" onClick={send}><Icon name="message" size={16} />{t('send')}</button>
      </div>
      <div className="seller-contact-actions">
        <a href="tel:+14081234567"><Icon name="phone" size={16} /> {t('phone')}</a>
        <a href="/messages"><Icon name="message" size={16} /> {t('open')} {t('messages').toLowerCase()}</a>
      </div>
      {status ? <p className="status-text compact-status">{status}</p> : null}
    </div>
  );
}
