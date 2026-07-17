import { useEffect, useMemo, useState } from 'react';
import { AdminDataTable, PageHero, StatusBadge, type AdminColumn, normalizePagedRows } from '../components/common/AdminCommon';
import { AdminButton, AdminIconButton, AdminSearchBox, AdminSelect, AdminTextBox, AdminToolbar } from '../components/common/AdminControls';
import { useApi } from '../hooks/useApi';
import { apiClient } from '../lib/api/apiClient';
import { categoryInitial, formatPrice } from '../utils/format';

function listingImage(item: any) {
  return item.imageUrl || item.ImageUrl || item.thumbnailUrl || item.coverImageUrl || item.media?.[0]?.url || item.mediaAssets?.[0]?.url || '';
}
function isActiveListing(row: any) {
  const value = String(row.status || '').toLowerCase();
  return value === 'published' || value === 'active' || value === 'approved';
}
function dateInput(value: any) {
  if (!value) return '';
  const d = new Date(value);
  if (Number.isNaN(d.getTime())) return '';
  const local = new Date(d.getTime() - d.getTimezoneOffset() * 60000);
  return local.toISOString().slice(0, 16);
}
function displayDate(value: any) { return value ? new Date(value).toLocaleString() : '-'; }

type EditForm = { status: string; packageCode: string; packageStartsAt: string; packageEndsAt: string; expiresAt: string };

export function AllListingsPage() {
  const [status, setStatus] = useState('All');
  const [q, setQ] = useState('');
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(10);
  const [selected, setSelected] = useState<any | null>(null);
  const [detail, setDetail] = useState<any | null>(null);
  const [form, setForm] = useState<EditForm | null>(null);
  const [saving, setSaving] = useState(false);

  const paths = status !== 'All' || q.trim()
    ? [`/admin/listings?status=${encodeURIComponent(status)}&q=${encodeURIComponent(q)}&page=${page}&pageSize=${pageSize}`]
    : [`/admin/listings?page=${page}&pageSize=${pageSize}`];
  const { data, loading, err, load } = useApi<any>(paths, { items: [] });
  const packagesApi = useApi<any>(['/admin/packages?page=1&pageSize=100&status=All'], { items: [] });
  const { rows, total } = normalizePagedRows(data);
  const packageRows = useMemo(() => normalizePagedRows(packagesApi.data).rows, [packagesApi.data]);
  const packageOptions = packageRows.map((p: any) => ({ label: `${p.name} (${p.code})`, value: p.code }));

  useEffect(() => {
    if (!selected) { setDetail(null); setForm(null); return; }
    apiClient.get<any>(`/admin/listings/${selected.id}`).then((d) => {
      setDetail(d);
      const x = d.listing || selected;
      setForm({
        status: x.status || 'Pending',
        packageCode: x.packageCode || 'FREE',
        packageStartsAt: dateInput(x.packageStartsAt),
        packageEndsAt: dateInput(x.packageEndsAt),
        expiresAt: dateInput(x.expiresAt),
      });
    }).catch((e) => alert((e as Error).message));
  }, [selected]);

  async function toggleListing(row: any) {
    const next = isActiveListing(row) ? 'Inactive' : 'Published';
    try {
      await apiClient.post(`/admin/listings/${row.id}/status`, { status: next, reason: next === 'Inactive' ? 'Inactivated by admin' : 'Activated by admin' });
      load();
    } catch (e) { alert((e as Error).message); }
  }

  async function saveListing() {
    if (!selected || !form) return;
    setSaving(true);
    try {
      await apiClient.post(`/admin/listings/${selected.id}/admin-update`, {
        status: form.status,
        packageCode: form.packageCode,
        packageStartsAt: form.packageStartsAt ? new Date(form.packageStartsAt).toISOString() : null,
        packageEndsAt: form.packageEndsAt ? new Date(form.packageEndsAt).toISOString() : null,
        expiresAt: form.expiresAt ? new Date(form.expiresAt).toISOString() : null,
      });
      setSelected(null);
      await load();
    } catch (e) { alert((e as Error).message); }
    finally { setSaving(false); }
  }

  function packageChanged(code: string) {
    if (!form) return;
    const pkg = packageRows.find((x: any) => x.code === code);
    const start = form.packageStartsAt || dateInput(new Date());
    let end = form.packageEndsAt;
    if (pkg?.durationDays) {
      const d = new Date(start);
      d.setDate(d.getDate() + Number(pkg.durationDays));
      end = dateInput(d);
    }
    setForm({ ...form, packageCode: code, packageStartsAt: start, packageEndsAt: end, expiresAt: end || form.expiresAt });
  }

  const columns: AdminColumn<any>[] = [
    { key: 'listing', header: 'Listing', render: (x) => { const image = listingImage(x); const category = x.category?.name || 'Listing'; return <div className="listing-cell"><div className="listing-thumb" style={image ? { backgroundImage: `url(${image})` } : undefined}>{!image && categoryInitial(category)}</div><div><strong>{x.title || 'Untitled listing'}</strong><small>{formatPrice(x.currency, x.price)} · {x.city || '-'} {x.state || ''}</small></div></div>; } },
    { key: 'category', header: 'Category', render: (x) => x.category?.name || '-' },
    { key: 'seller', header: 'Seller', render: (x) => x.seller?.name || '-' },
    { key: 'package', header: 'Package', render: (x) => <div><strong>{x.packageCode || 'FREE'}</strong><small>{x.packageStatus || '-'}</small></div> },
    { key: 'expiresAt', header: 'Expired Date', render: (x) => { const d = x.expiresAt || x.packageEndsAt; return d ? new Date(d).toLocaleDateString() : '-'; } },
    { key: 'status', header: 'Status', render: (x) => <StatusBadge value={x.status || 'Unknown'} /> },
    { key: 'actions', header: 'Actions', width: '150px', render: (x) => <div className="row-actions nowrap-actions"><AdminIconButton icon={isActiveListing(x) ? 'toggleOn' : 'toggleOff'} label={isActiveListing(x) ? 'Set inactive' : 'Set active'} className={isActiveListing(x) ? 'success-action' : 'danger-action'} onClick={() => toggleListing(x)} /><AdminIconButton icon="view" label="View details" onClick={() => setSelected(x)} /><AdminIconButton icon="edit" label="Edit package and expiration" onClick={() => setSelected(x)} /></div> },
  ];

  const x = detail?.listing;
  return <>
    <PageHero eyebrow="LISTING MANAGEMENT" title="All Listings" description="View listing details, enable or disable listings, and manage package and expiration dates." />
    <AdminDataTable title="Listings" rows={rows} columns={columns} loading={loading} error={err} emptyText="No listings found." actions={<AdminToolbar><AdminSearchBox value={q} onChange={(e) => { setQ(e.target.value); setPage(1); }} placeholder="Search listings..." /><AdminSelect value={status} onChange={(e) => { setStatus(e.target.value); setPage(1); }} options={['All', 'Pending', 'Published', 'Inactive', 'Rejected', 'Expired']} /><AdminIconButton icon="refresh" label="Refresh" onClick={load} /></AdminToolbar>} paging={{ page, pageSize, total, onPageChange: setPage, onPageSizeChange: (n) => { setPageSize(n); setPage(1); } }} />

    {selected && <div className="admin-modal-backdrop" onMouseDown={(e) => { if (e.currentTarget === e.target) setSelected(null); }}><section className="admin-modal-card">
      <div className="review-top"><div><h2>Listing Detail</h2><small>{selected.title}</small></div><AdminButton onClick={() => setSelected(null)}>Close</AdminButton></div>
      {!detail || !form ? <p>Loading...</p> : <>
        <div className="listing-detail-grid">
          <div><b>Title</b><p>{x.title}</p></div><div><b>Price</b><p>{formatPrice(x.currency, x.price)}</p></div>
          <div><b>Seller</b><p>{detail.seller?.name || '-'}<br/><small>{detail.seller?.email || ''}</small></p></div><div><b>Category</b><p>{detail.category?.name || '-'}</p></div>
          <div><b>Location</b><p>{x.addressLine || ''} {x.city}, {x.state} {x.postalCode}</p></div><div><b>Created</b><p>{displayDate(x.createdAt)}</p></div>
          <div className="span2"><b>Description</b><p style={{whiteSpace:'pre-wrap'}}>{x.description || '-'}</p></div>
          {detail.media?.length > 0 && <div className="span2"><b>Images</b><div className="admin-listing-images">{detail.media.map((m:any)=><img key={m.id} src={m.url} alt="Listing" />)}</div></div>}
        </div>
        <hr/>
        <div className="ad-form-grid">
          <AdminSelect label="Status" value={form.status} onChange={(e) => setForm({ ...form, status: e.target.value })} options={['Published', 'Inactive', 'Pending', 'Rejected', 'Expired', 'Sold']} />
          <AdminSelect label="Package" value={form.packageCode} onChange={(e) => packageChanged(e.target.value)} options={packageOptions.length ? packageOptions : [{label: form.packageCode, value: form.packageCode}]} />
          <AdminTextBox label="Package Start" type="datetime-local" value={form.packageStartsAt} onChange={(e) => setForm({ ...form, packageStartsAt: e.target.value })} />
          <AdminTextBox label="Package End" type="datetime-local" value={form.packageEndsAt} onChange={(e) => setForm({ ...form, packageEndsAt: e.target.value })} />
          <AdminTextBox label="Listing Expired Date" type="datetime-local" value={form.expiresAt} onChange={(e) => setForm({ ...form, expiresAt: e.target.value })} />
        </div>
        <div className="modal-actions"><AdminButton onClick={() => setSelected(null)}>Cancel</AdminButton><AdminButton variant="primary" disabled={saving} onClick={saveListing}>{saving ? 'Saving...' : 'Save Changes'}</AdminButton></div>
      </>}
    </section></div>}
  </>;
}
