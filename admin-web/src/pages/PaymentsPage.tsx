import { useState } from 'react';
import { AdminDataTable, PageHero, StatusBadge, type AdminColumn, normalizePagedRows } from '../components/common/AdminCommon';
import { AdminButton, AdminIconButton, AdminToolbar } from '../components/common/AdminControls';
import { useApi } from '../hooks/useApi';
import { formatPrice } from '../utils/format';

export function PaymentsPage() {
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(10);
  const [selected, setSelected] = useState<any | null>(null);
  const { data, loading, err, load } = useApi<any>(`/admin/payments?page=${page}&pageSize=${pageSize}`, { items: [] });
  const { rows, total } = normalizePagedRows(data);
  const columns: AdminColumn<any>[] = [
    { key: 'payment', header: 'Payment', render: (p) => <div className="message-listing"><strong>{p.description || p.packageName || p.id}</strong><small>{p.stripePaymentIntentId || p.transactionId || p.reference || ''}</small></div> },
    { key: 'user', header: 'User', render: (p) => p.user?.email || p.userEmail || p.customerEmail || '-' },
    { key: 'amount', header: 'Amount', render: (p) => formatPrice(p.currency || 'USD', p.amount ?? p.total ?? p.price ?? 0) },
    { key: 'status', header: 'Status', render: (p) => <StatusBadge value={p.status || p.paymentStatus || 'Unknown'} /> },
    { key: 'createdAt', header: 'Date', render: (p) => p.createdAt ? new Date(p.createdAt).toLocaleString() : '-' },
    { key: 'actions', header: '', width: '56px', render: (p) => <AdminIconButton icon="view" label="View payment" onClick={() => setSelected(p)} /> },
  ];
  return <><PageHero eyebrow="FINANCE" title="Payments & Transactions" description="Review package purchases and marketplace transactions." /><AdminDataTable title="Payments & Transactions" rows={rows} columns={columns} loading={loading} error={err} emptyText="No payments found" actions={<AdminToolbar><AdminIconButton icon="refresh" label="Refresh" onClick={load} /></AdminToolbar>} paging={{ page, pageSize, total, onPageChange: setPage, onPageSizeChange: (n) => { setPageSize(n); setPage(1); } }} />{selected && <div className="admin-modal-backdrop"><section className="admin-modal-card"><h2>Payment Detail</h2><div className="detail-grid">{Object.entries(selected).map(([k,v]) => <div key={k}><span>{k}</span><strong>{typeof v === 'object' ? JSON.stringify(v) : String(v ?? '-')}</strong></div>)}</div><div className="modal-actions"><AdminButton onClick={() => setSelected(null)}>Close</AdminButton></div></section></div>}</>;
}
