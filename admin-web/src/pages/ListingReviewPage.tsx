import { useMemo, useState } from 'react';
import { AdminDataTable, PageHero, StatusBadge, type AdminColumn, normalizePagedRows } from '../components/common/AdminCommon';
import { AdminActionGroup, AdminIconButton, AdminSearchBox, AdminSelect, AdminToolbar } from '../components/common/AdminControls';
import { useApi } from '../hooks/useApi';
import { apiClient } from '../lib/api/apiClient';
import { appConfig } from '../lib/config';
import { categoryInitial, formatPrice } from '../utils/format';

function listingImage(item: any) {
  return item.imageUrl || item.ImageUrl || item.thumbnailUrl || item.coverImageUrl || item.media?.[0]?.url || item.mediaAssets?.[0]?.url || '';
}

function rowStatus(row: any) {
  return String(row.moderationStatus || row.ModerationStatus || row.status || row.Status || 'Pending');
}

function isActiveListing(row: any) {
  const value = rowStatus(row).toLowerCase();
  return value.includes('active') || value.includes('approved') || value.includes('published');
}

function listingPublicUrl(id: string) {
  const base = appConfig.apiBaseUrl.replace(/\/api\/v\d+\/?$/, '').replace(/\/api\/?$/, '');
  return `${base}/listings/${id}`;
}

function isPendingReview(row: any) {
  const value = rowStatus(row).toLowerCase();
  return value.includes('pending') || value.includes('review') || value.includes('draft');
}

function statusMatches(row: any, selected: string) {
  if (selected === 'All') return true;
  if (selected === 'Pending') return isPendingReview(row);
  const value = rowStatus(row).toLowerCase();
  return value.includes(selected.toLowerCase());
}

export function ListingReviewPage() {
  const [q, setQ] = useState('');
  const [status, setStatus] = useState('Pending');
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(10);
  const paths = [
    `/admin/listings/review?status=All&q=${encodeURIComponent(q)}&page=${page}&pageSize=${pageSize}`,
    `/admin/listings?status=Pending&q=${encodeURIComponent(q)}&page=${page}&pageSize=${pageSize}`,
    `/admin/listings?moderationStatus=PendingApproval&q=${encodeURIComponent(q)}&page=${page}&pageSize=${pageSize}`,
    `/admin/listings?status=All&q=${encodeURIComponent(q)}&page=${page}&pageSize=${pageSize}`,
  ];
  const { data, loading, err, load } = useApi<any>(paths, { items: [] });
  const { rows: allRows, total } = normalizePagedRows(data);
  const rows = useMemo(() => allRows.filter((row: any) => statusMatches(row, status)), [allRows, status]);

  async function moderate(row: any, decision: string) {
    const reason = decision === 'Approved' ? 'Approved by admin' : 'Rejected by admin';
    try {
      await apiClient.post(`/admin/listings/${row.id}/moderate`, { decision, reason }).catch(async () => {
        await apiClient.post(`/admin/listings/${row.id}/status`, { status: decision, reason });
      });
      load();
    } catch (e) {
      alert((e as Error).message);
    }
  }

  async function toggleListing(row: any) {
    await moderate(row, isActiveListing(row) ? 'Rejected' : 'Approved');
  }

  const columns: AdminColumn<any>[] = [
    {
      key: 'listing',
      header: 'Listing',
      render: (x) => {
        const image = listingImage(x);
        const category = x.category?.name || x.categoryName || x.category || 'Listing';
        return (
          <div className="listing-cell">
            <div className="listing-thumb" style={image ? { backgroundImage: `url(${image})` } : undefined}>{!image && categoryInitial(category)}</div>
            <div>
              <strong>{x.title || 'Untitled listing'}</strong>
              <small>{formatPrice(x.currency, x.price)} · {x.city || x.location || '-'} {x.state || ''}</small>
            </div>
          </div>
        );
      },
    },
    { key: 'category', header: 'Category', render: (x) => x.category?.name || x.categoryName || x.category || '-' },
    { key: 'seller', header: 'Seller', render: (x) => x.seller?.name || x.sellerName || x.seller || x.user?.name || '-' },
    { key: 'expiresAt', header: 'Expired Date', render: (x) => { const d = x.expiresAt || x.packageEndsAt || x.expiredDate; return d ? new Date(d).toLocaleDateString() : '-'; } },
    { key: 'submitted', header: 'Submitted', render: (x) => x.createdAt ? new Date(x.createdAt).toLocaleString() : '-' },
    { key: 'status', header: 'Status', render: (x) => <StatusBadge value={rowStatus(x)} /> },
    {
      key: 'actions',
      header: 'Actions',
      width: '132px',
      render: (x) => (
        <AdminActionGroup>
          <AdminIconButton icon={isActiveListing(x) ? 'toggleOn' : 'toggleOff'} label={isActiveListing(x) ? 'Set inactive' : 'Set active'} className={isActiveListing(x) ? 'success-action' : 'danger-action'} onClick={() => toggleListing(x)} />
          <AdminIconButton icon="view" label="Preview listing" onClick={() => window.open(listingPublicUrl(x.id), '_blank')} />
        </AdminActionGroup>
      ),
    },
  ];

  return (
    <>
      <PageHero eyebrow="LISTING MANAGEMENT" title="Pending Approval" description="Review and approve marketplace listings before they go live." />
      <AdminDataTable
        title="Review Queue"
        subtitle="Real data from the admin listing review API. The Pending filter also includes draft/review statuses."
        rows={rows}
        columns={columns}
        loading={loading}
        error={err}
        emptyText="No listings waiting for review."
        actions={
          <AdminToolbar>
            <AdminSearchBox value={q} onChange={(e) => { setQ(e.target.value); setPage(1); }} placeholder="Search listing, seller, city..." />
            <AdminSelect value={status} onChange={(e) => { setStatus(e.target.value); setPage(1); }} options={['Pending', 'Rejected', 'Approved', 'Published', 'All']} />
            <AdminIconButton icon="refresh" label="Refresh" onClick={load} />
          </AdminToolbar>
        }
        paging={{ page, pageSize, total, onPageChange: setPage, onPageSizeChange: (n) => { setPageSize(n); setPage(1); } }}
      />
    </>
  );
}
