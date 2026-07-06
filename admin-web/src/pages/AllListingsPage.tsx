import { useState } from 'react';
import { AdminDataTable, PageHero, StatusBadge, type AdminColumn, normalizePagedRows } from '../components/common/AdminCommon';
import { AdminIconButton, AdminSearchBox, AdminSelect, AdminToolbar } from '../components/common/AdminControls';
import { useApi } from '../hooks/useApi';
import { apiClient } from '../lib/api/apiClient';
import { appConfig } from '../lib/config';
import { categoryInitial, formatPrice } from '../utils/format';

function listingImage(item: any) {
  return item.imageUrl || item.ImageUrl || item.thumbnailUrl || item.coverImageUrl || item.media?.[0]?.url || item.mediaAssets?.[0]?.url || '';
}

function isActiveListing(row: any) {
  const value = String(row.status || row.moderationStatus || '').toLowerCase();
  return value.includes('active') || value.includes('approved') || value.includes('published');
}

function listingPublicUrl(id: string) {
  const base = appConfig.apiBaseUrl.replace(/\/api\/v\d+\/?$/, '').replace(/\/api\/?$/, '');
  return `${base}/listings/${id}`;
}

export function AllListingsPage() {
  const [status, setStatus] = useState('All');
  const [q, setQ] = useState('');
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(10);
  const paths = status !== 'All' || q.trim()
    ? [`/admin/listings?status=${encodeURIComponent(status)}&q=${encodeURIComponent(q)}&page=${page}&pageSize=${pageSize}`, `/admin/listings/review?status=${encodeURIComponent(status)}&q=${encodeURIComponent(q)}&page=${page}&pageSize=${pageSize}`]
    : [`/admin/listings?page=${page}&pageSize=${pageSize}`, `/admin/listings/review?status=All&page=${page}&pageSize=${pageSize}`];
  const { data, loading, err, load } = useApi<any>(paths, { items: [] });
  const { rows, total } = normalizePagedRows(data);

  async function toggleListing(row: any) {
    const next = isActiveListing(row) ? 'Inactive' : 'Approved';
    try {
      await apiClient.post(`/admin/listings/${row.id}/status`, { status: next, reason: next === 'Inactive' ? 'Inactivated by admin' : 'Activated by admin' }).catch(async () => {
        await apiClient.post(`/admin/listings/${row.id}/moderate`, { decision: next === 'Inactive' ? 'Rejected' : 'Approved', reason: next === 'Inactive' ? 'Inactivated by admin' : 'Activated by admin' });
      });
      load();
    } catch (e) { alert((e as Error).message); }
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
    { key: 'createdAt', header: 'Created', render: (x) => x.createdAt ? new Date(x.createdAt).toLocaleDateString() : '-' },
    { key: 'status', header: 'Status', render: (x) => <StatusBadge value={x.moderationStatus || x.status || 'Unknown'} /> },
    { key: 'actions', header: 'Actions', width: '118px', render: (x) => <div className="row-actions nowrap-actions"><AdminIconButton icon={isActiveListing(x) ? 'toggleOn' : 'toggleOff'} label={isActiveListing(x) ? 'Set inactive' : 'Set active'} className={isActiveListing(x) ? 'success-action' : 'danger-action'} onClick={() => toggleListing(x)} /><AdminIconButton icon="view" label="View listing" onClick={() => window.open(listingPublicUrl(x.id), '_blank')} /></div> },
  ];

  return (
    <>
      <PageHero eyebrow="LISTING MANAGEMENT" title="All Listings" description="Search and manage all marketplace listings in one place." />
      <AdminDataTable
        title="Listings"
        rows={rows}
        columns={columns}
        loading={loading}
        error={err}
        emptyText="No listings found."
        actions={
          <AdminToolbar>
            <AdminSearchBox value={q} onChange={(e) => { setQ(e.target.value); setPage(1); }} placeholder="Search listings..." />
            <AdminSelect value={status} onChange={(e) => { setStatus(e.target.value); setPage(1); }} options={['All', 'Pending', 'Published', 'Approved', 'Rejected', 'Expired']} />
            <AdminIconButton icon="refresh" label="Refresh" onClick={load} />
          </AdminToolbar>
        }
        paging={{ page, pageSize, total, onPageChange: setPage, onPageSizeChange: (n) => { setPageSize(n); setPage(1); } }}
      />
    </>
  );
}
