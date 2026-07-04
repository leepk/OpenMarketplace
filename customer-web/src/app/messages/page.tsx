'use client';

import Link from 'next/link';
import { FormEvent, useEffect, useMemo, useRef, useState } from 'react';
import { AuthEmpty, useSessionUser } from '@/components/account/RequireAuth';
import { AccountShell } from '@/components/account/AccountShell';
import { marketplaceApi } from '@/lib/api/apiClient';
import { Icon } from '@/components/ui/Icon';
import { useI18n } from '@/lib/i18n/client';

type ConversationItem = {
  id: string;
  listingId?: string;
  subject?: string;
  lastMessageAt?: string;
  unreadCount?: number;
  listing?: { title?: string; imageUrl?: string | null; price?: number | string | null; currency?: string | null } | null;
  otherUser?: { displayName?: string; email?: string; avatarUrl?: string | null } | null;
  lastMessage?: { body?: string; createdAt?: string; isMine?: boolean } | null;
};

type ChatMessage = { id: string; senderId: string; body: string; createdAt?: string; isMine?: boolean; isRead?: boolean };

function formatTime(value?: string) {
  if (!value) return '';
  const date = new Date(value);
  const today = new Date();
  const sameDay = date.toDateString() === today.toDateString();
  return date.toLocaleString(undefined, sameDay ? { hour: 'numeric', minute: '2-digit' } : { month: 'short', day: 'numeric', hour: 'numeric', minute: '2-digit' });
}

export default function MessagesPage() {
  const { t } = useI18n();
  const { user, ready } = useSessionUser();
  const [items, setItems] = useState<ConversationItem[]>([]);
  const [loading, setLoading] = useState(true);
  const [activeId, setActiveId] = useState<string>('');
  const [thread, setThread] = useState<any>(null);
  const [messages, setMessages] = useState<ChatMessage[]>([]);
  const [draft, setDraft] = useState('');
  const [sending, setSending] = useState(false);
  const [query, setQuery] = useState('');
  const bottomRef = useRef<HTMLDivElement | null>(null);

  const active = useMemo(() => items.find((item) => item.id === activeId) ?? null, [items, activeId]);
  const filteredItems = useMemo(() => {
    const q = query.trim().toLowerCase();
    if (!q) return items;
    return items.filter((item) => `${item.subject ?? ''} ${item.otherUser?.displayName ?? ''} ${item.lastMessage?.body ?? ''}`.toLowerCase().includes(q));
  }, [items, query]);

  async function loadConversations(selectFirst = false) {
    if (!user?.id) return;
    const data = await marketplaceApi.conversations(user.id);
    const nextItems = data.items ?? [];
    setItems(nextItems);
    if (selectFirst && !activeId) setActiveId(nextItems[0]?.id ?? '');
  }

  useEffect(() => {
    if (!user?.id) { setLoading(false); return; }
    let off = false;
    setLoading(true);
    marketplaceApi.conversations(user.id)
      .then((data) => {
        if (off) return;
        const nextItems = data.items ?? [];
        setItems(nextItems);
        const params = new URLSearchParams(window.location.search);
        setActiveId(params.get('conversationId') || nextItems[0]?.id || '');
      })
      .catch(() => !off && setItems([]))
      .finally(() => !off && setLoading(false));
    return () => { off = true; };
  }, [user?.id]);

  useEffect(() => {
    if (!user?.id) return;
    const timer = window.setInterval(() => loadConversations(false).catch(() => undefined), 15000);
    return () => window.clearInterval(timer);
  }, [user?.id, activeId]);

  useEffect(() => {
    if (!activeId || !user?.id) { setThread(null); setMessages([]); return; }
    let off = false;
    marketplaceApi.conversationThread(activeId, user.id)
      .then(async (data) => {
        if (off) return;
        setThread(data);
        setMessages(data.messages ?? []);
        await marketplaceApi.markConversationRead(activeId, user.id).catch(() => undefined);
        await loadConversations(false).catch(() => undefined);
      })
      .catch(() => { if (!off) { setThread(null); setMessages([]); } });
    return () => { off = true; };
  }, [activeId, user?.id]);

  useEffect(() => { bottomRef.current?.scrollIntoView({ behavior: 'smooth' }); }, [messages.length, activeId]);

  async function submitMessage(e: FormEvent) {
    e.preventDefault();
    const body = draft.trim();
    if (!activeId || !body || sending) return;
    setSending(true);
    try {
      const response = await marketplaceApi.sendConversationMessage(activeId, body, user?.id ?? '');
      const created = response.message;
      setMessages((current) => [...current, { ...created, isMine: true }]);
      setDraft('');
      await loadConversations(false);
    } finally {
      setSending(false);
    }
  }

  if (!ready) return null;
  if (!user) return <AuthEmpty title={t('messages')} text={t('messagesAuthText')} />;

  return (
    <AccountShell user={user} title={t('messages')} subtitle={t('messagesSubtitle')}>
      <div className="messages-layout-v3">
        <aside className="conversation-list-v3">
          <div className="mini-search-v3"><Icon name="search" size={15} /><input value={query} onChange={(e) => setQuery(e.target.value)} placeholder={t('searchMessages')} /></div>
          {loading ? <p className="muted-line-v3">{t('loading')}</p> : filteredItems.length ? filteredItems.map((conversation) => (
            <button key={conversation.id} onClick={() => setActiveId(conversation.id)} className={activeId === conversation.id ? 'active' : ''}>
              <span><Icon name="message" size={16} /></span>
              <strong>{conversation.otherUser?.displayName || conversation.subject || t('conversation')}</strong>
              <small>{conversation.subject || conversation.listing?.title || t('listingConversation')}</small>
              <em>{conversation.lastMessage?.body || t('noMessagesYet')}</em>
              {conversation.unreadCount ? <b>{conversation.unreadCount}</b> : null}
            </button>
          )) : <p className="muted-line-v3">{t('noConversationsYet')}</p>}
        </aside>

        <section className="chat-panel-v3">
          {active ? (
            <>
              <div className="chat-head-v3">
                <div>
                  <span>{active.otherUser?.displayName || t('listingConversation')}</span>
                  <h2>{active.subject || active.listing?.title}</h2>
                </div>
                <Link href={active.listingId ? `/listings/${active.listingId}` : '#'}>{t('viewListingLower')}</Link>
              </div>

              {thread?.listing ? (
                <div className="chat-listing-strip-v3">
                  {thread.listing.imageUrl ? <img src={thread.listing.imageUrl} alt="" /> : <span><Icon name="tag" size={18} /></span>}
                  <div><strong>{thread.listing.title}</strong><small>{thread.listing.price ? `${thread.listing.currency ?? '$'}${thread.listing.price}` : t('listingConversation')}</small></div>
                </div>
              ) : null}

              <div className="chat-body-v3">
                {messages.length ? messages.map((message) => (
                  <div key={message.id} className={`bubble-v3 ${message.isMine ? 'right' : 'left'}`}>
                    <p>{message.body}</p>
                    <small>{formatTime(message.createdAt)}</small>
                  </div>
                )) : <p className="muted-line-v3">{t('noMessagesYet')}</p>}
                <div ref={bottomRef} />
              </div>

              <form className="chat-compose-v3" onSubmit={submitMessage}>
                <input value={draft} onChange={(e) => setDraft(e.target.value)} placeholder={t('typeMessage')} />
                <button type="submit" disabled={sending || !draft.trim()}>{sending ? t('processing') : t('send')}</button>
              </form>
            </>
          ) : <div className="empty-state-v3"><Icon name="message" size={34} /><strong>{t('selectConversation')}</strong><p>{t('conversationAppear')}</p></div>}
        </section>
      </div>
    </AccountShell>
  );
}
