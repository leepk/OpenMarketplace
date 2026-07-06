import { useState } from 'react';
import { AdminDataTable, PageHero, PanelHeader, StatusBadge, type AdminColumn, normalizePagedRows } from '../components/common/AdminCommon';
import { AdminCheckbox, AdminIconButton, AdminSelect, AdminTextArea, AdminTextBox, AdminToolbar } from '../components/common/AdminControls';
import { useApi } from '../hooks/useApi';
import { apiClient } from '../lib/api/apiClient';

export function NotificationsPage() {
  const [userPage, setUserPage] = useState(1);
  const [userPageSize, setUserPageSize] = useState(50);
  const [sentPage, setSentPage] = useState(1);
  const [sentPageSize, setSentPageSize] = useState(10);
  const users = useApi<any>(`/admin/users?page=${userPage}&pageSize=${userPageSize}`, { items: [] });
  const sent = useApi<any>(`/admin/notifications?page=${sentPage}&pageSize=${sentPageSize}`, { items: [] });
  const [sendToAll, setSendToAll] = useState(false);
  const [userId, setUserId] = useState('');
  const [type, setType] = useState('Admin');
  const [title, setTitle] = useState('');
  const [body, setBody] = useState('');
  const [url, setUrl] = useState('/notifications');
  const [status, setStatus] = useState('');
  const [tab, setTab] = useState<'send' | 'list'>('send');
  const { rows: userRows } = normalizePagedRows(users.data);
  const { rows: notifications, total } = normalizePagedRows(sent.data);

  async function submit() {
    if (!sendToAll && !userId) { setStatus('Select a user or Send to all.'); return; }
    try {
      const r: any = await apiClient.post('/admin/notifications/send', { sendToAll, userIds: userId ? [userId] : [], type, title, body, url });
      setStatus(`Sent to ${r.sent ?? r.count ?? 0} user(s).`);
      setTitle('');
      setBody('');
      sent.load();
    } catch (e) {
      setStatus((e as Error).message);
    }
  }

  const columns: AdminColumn<any>[] = [
    { key: 'title', header: 'Title', render: (n) => <div className="message-listing"><strong>{n.title}</strong><small>{n.body}</small></div> },
    { key: 'type', header: 'Type', render: (n) => <StatusBadge value={n.type || 'Admin'} /> },
    { key: 'user', header: 'User', render: (n) => n.user?.email || n.userEmail || n.userId || 'Broadcast' },
    { key: 'status', header: 'Status', render: (n) => <StatusBadge value={n.isRead ? 'Read' : 'Unread'} /> },
    { key: 'createdAt', header: 'Created', render: (n) => n.createdAt ? new Date(n.createdAt).toLocaleString() : '-' },
    { key: 'actions', header: '', width: '64px', render: (n) => <AdminIconButton icon="view" label="Open target" disabled={!n.url} onClick={() => n.url && window.open(n.url, '_blank')} /> },
  ];

  return (
    <>
      <PageHero eyebrow="NOTIFICATIONS" title="Send Notifications" description="Send direct or broadcast notifications to marketplace users." />
      <div className="admin-tabs-panel">
        <div className="admin-tabs"><button className={tab === 'send' ? 'active' : ''} onClick={() => setTab('send')}>Send notifies to user</button><button className={tab === 'list' ? 'active' : ''} onClick={() => setTab('list')}>List notify</button></div>
        {tab === 'send' && <section className="panel">
          <PanelHeader title="Send notification to users" action={<AdminIconButton icon="refresh" label="Refresh users" onClick={users.load} />} />
          <div className="form-grid">
            <AdminCheckbox label="Send to all active users" checked={sendToAll} onChange={(e) => setSendToAll(e.target.checked)} />
            <AdminSelect label="User" disabled={sendToAll} value={userId} onChange={(e) => setUserId(e.target.value)}>
              <option value="">Select user...</option>
              {userRows.map((u) => <option key={u.id} value={u.id}>{u.name || u.email} — {u.email}</option>)}
            </AdminSelect>
            <AdminSelect label="Type" value={type} onChange={(e) => setType(e.target.value)} options={['Admin', 'System', 'Promotion', 'Safety', 'Listing']} />
            <AdminTextBox label="URL" value={url} onChange={(e) => setUrl(e.target.value)} placeholder="/notifications" />
            <AdminTextBox label="Title" wrapperClassName="span2" value={title} onChange={(e) => setTitle(e.target.value)} placeholder="Important marketplace update" />
            <AdminTextArea label="Message" wrapperClassName="span2" value={body} onChange={(e) => setBody(e.target.value)} placeholder="Write the message users will see..." />
            <AdminIconButton icon="bell" label="Send notification" className="success-action large-icon-action" onClick={submit} />
            {status && <p className="muted">{status}</p>}
          </div>
        </section>}
        {tab === 'list' && <AdminDataTable
          title="Sent Notifications"
          rows={notifications}
          columns={columns}
          loading={sent.loading}
          error={sent.err}
          emptyText="No notifications yet"
          actions={<AdminToolbar><AdminIconButton icon="refresh" label="Refresh notifications" onClick={sent.load} /></AdminToolbar>}
          paging={{ page: sentPage, pageSize: sentPageSize, total, onPageChange: setSentPage, onPageSizeChange: (n) => { setSentPageSize(n); setSentPage(1); } }}
        />}
      </div>
    </>
  );
}
