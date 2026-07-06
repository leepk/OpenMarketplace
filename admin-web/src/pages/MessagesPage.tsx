import { useEffect, useState } from 'react';
import { AdminDataTable, EmptyState, ErrorState, PageHero, PanelHeader, StatusBadge, type AdminColumn, normalizePagedRows } from '../components/common/AdminCommon';
import { AdminIconButton, AdminSearchBox, AdminToolbar } from '../components/common/AdminControls';
import { useApi } from '../hooks/useApi';
import { apiClient } from '../lib/api/apiClient';

export function MessagesPage() {
  const [selected, setSelected] = useState('');
  const [search, setSearch] = useState('');
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(10);
  const path = `/admin/messages/conversations?q=${encodeURIComponent(search)}&page=${page}&pageSize=${pageSize}`;
  const { data, loading, err, load } = useApi<any>(path, { items: [] });
  const { rows: conversations, total } = normalizePagedRows(data);

  useEffect(() => {
    if (!selected && conversations[0]?.id) setSelected(conversations[0].id);
  }, [selected, conversations]);

  const columns: AdminColumn<any>[] = [
    { key: 'listing', header: 'Listing', render: (c) => <div className="message-listing"><strong>{c.listingTitle || c.listing?.title || 'Listing conversation'}</strong><small>{c.lastMessagePreview || 'No preview'}</small></div> },
    { key: 'buyer', header: 'Buyer', render: (c) => c.buyer?.name || c.buyerName || '-' },
    { key: 'seller', header: 'Seller', render: (c) => c.seller?.name || c.sellerName || '-' },
    { key: 'unread', header: 'Unread', render: (c) => <StatusBadge value={`${(c.unreadBuyerCount || 0) + (c.unreadSellerCount || 0)} unread`} /> },
    { key: 'updated', header: 'Updated', render: (c) => c.lastMessageAt ? new Date(c.lastMessageAt).toLocaleString() : '-' },

  ];

  return (
    <>
      <PageHero eyebrow="MESSAGE MODERATION" title="User Messages" description="Review conversations between buyers and sellers, then flag or hide unsafe content." />
      <div className="messages-layout">
        <AdminDataTable
          title="Conversations"
          rows={conversations}
          columns={columns}
          loading={loading}
          error={err}
          emptyText="No conversations found"
          actions={<AdminToolbar><AdminSearchBox value={search} onChange={(e) => { setSearch(e.target.value); setPage(1); }} placeholder="Search user, listing or message..." /><AdminIconButton icon="refresh" label="Refresh conversations" onClick={load} /></AdminToolbar>}
          paging={{ page, pageSize, total, onPageChange: setPage, onPageSizeChange: (n) => { setPageSize(n); setPage(1); } }}
          onRowClick={(row) => setSelected(row.id)}
        />
        {selected ? <MessageThread id={selected} /> : <section className="panel"><EmptyState text="Select a conversation" /></section>}
      </div>
    </>
  );
}

function MessageThread({ id }: { id: string }) {
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(25);
  const { data, err, loading, load } = useApi<any>(`/admin/messages/conversations/${id}?page=${page}&pageSize=${pageSize}`, { messages: [], participants: [], listing: null });
  const messagePayload = Array.isArray(data.messages) ? { items: data.messages, total: data.total ?? data.messages.length, page, pageSize } : data.messages ?? data;
  const { rows: messages, total } = normalizePagedRows(messagePayload);

  async function moderate(messageId: string, status: string, hide = false) {
    try {
      await apiClient.post(`/admin/messages/${messageId}/moderate`, { status, hideMessage: hide });
      load();
    } catch (e) {
      alert((e as Error).message);
    }
  }

  function isHiddenMessage(m: any) {
    const status = String(m.moderationStatus || m.status || '').toLowerCase();
    return status.includes('hidden') || m.isHidden === true || m.hidden === true;
  }

  async function toggleMessage(m: any) {
    const nextHidden = !isHiddenMessage(m);
    await moderate(m.id, nextHidden ? 'Hidden' : 'Allowed', nextHidden);
  }

  const userName = (uid: string) => (data.users ?? data.participants)?.find((p: any) => String(p.id).toLowerCase() === String(uid).toLowerCase())?.name ?? 'User';
  const columns: AdminColumn<any>[] = [
    { key: 'sender', header: 'Sender', render: (m) => <strong>{userName(m.senderId)}</strong> },
    { key: 'message', header: 'Message', render: (m) => <div className="message-content"><span>{m.body || m.content || m.message || '-'}</span><small>{m.createdAt ? new Date(m.createdAt).toLocaleString() : ''}</small></div> },
    { key: 'status', header: 'Status', render: (m) => <StatusBadge value={m.moderationStatus ?? 'Allowed'} /> },
    { key: 'actions', header: 'Actions', width: '96px', render: (m) => { const hidden = isHiddenMessage(m); return <div className="row-actions nowrap-actions"><AdminIconButton icon="flag" label="Flag message" onClick={() => moderate(m.id, 'Flagged')} /><AdminIconButton icon={hidden ? 'toggleOff' : 'toggleOn'} label={hidden ? 'Show message' : 'Hide message'} className={hidden ? 'danger-action' : 'success-action'} onClick={() => toggleMessage(m)} /></div>; } },
  ];

  return (
    <AdminDataTable
      title={data.listing?.title ?? 'Thread Detail'}
      subtitle="Message detail uses backend paging."
      rows={messages}
      columns={columns}
      loading={loading}
      error={err}
      emptyText="No messages found"
      actions={<AdminToolbar><AdminIconButton icon="refresh" label="Reload thread" onClick={load} /></AdminToolbar>}
      paging={{ page, pageSize, total, onPageChange: setPage, onPageSizeChange: (n) => { setPageSize(n); setPage(1); }, pageSizeOptions: [25, 50, 100] }}
    />
  );
}
