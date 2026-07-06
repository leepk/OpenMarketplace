import { useEffect, useMemo, useState } from 'react';
import { AdminDataTable, PageHero, StatCard, StatusBadge, type AdminColumn, normalizePagedRows } from '../components/common/AdminCommon';
import { AdminButton, AdminCheckbox, AdminIconButton, AdminSelect, AdminTextArea, AdminTextBox, AdminToolbar } from '../components/common/AdminControls';
import { useApi } from '../hooks/useApi';
import { apiClient } from '../lib/api/apiClient';
import { appConfig } from '../lib/config';

type AdForm = {
  id?: string;
  campaignName: string;
  title: string;
  description: string;
  placement: string;
  imageUrl: string;
  targetUrl: string;
  status: string;
  sortOrder: number;
  openInNewTab: boolean;
  maxImpressions: number;
  maxClicks: number;
  expiresAt: string;
};


function getApiAssetBaseUrl() {
  return appConfig.apiBaseUrl.replace(/\/api(?:\/v\d+)?$/i, '');
}

function resolveAssetUrl(url?: string) {
  const value = String(url || '').trim();
  if (!value) return '';
  if (/^(https?:)?\/\//i.test(value) || value.startsWith('data:') || value.startsWith('blob:')) return value;
  return `${getApiAssetBaseUrl()}${value.startsWith('/') ? value : `/${value}`}`;
}

function getAdImageUrl(ad: any) {
  return resolveAssetUrl(ad?.imageUrl || ad?.desktopImageUrl || ad?.mobileImageUrl);
}

const defaultPlacements = ['HOME_HERO', 'HOME_FEED', 'SIDEBAR'];
const statuses = ['Active', 'Inactive', 'Pending', 'Paused', 'Rejected', 'Expired'];

const emptyForm: AdForm = {
  campaignName: 'Admin Ads',
  title: '',
  description: '',
  placement: 'HOME_HERO',
  imageUrl: '', 
  targetUrl: '',
  status: 'Active',
  sortOrder: 10,
  openInNewTab: true,
  maxImpressions: 0,
  maxClicks: 0,
  expiresAt: '',
};

export function AdsPage() {
  const [tab, setTab] = useState<'ads' | 'placements'>('ads');
  const [placement, setPlacement] = useState('All');
  const [status, setStatus] = useState('All');
  const [adsPage, setAdsPage] = useState(1);
  const [adsPageSize, setAdsPageSize] = useState(10);
  const [placementPage, setPlacementPage] = useState(1);
  const [placementPageSize, setPlacementPageSize] = useState(10);
  const [editingAd, setEditingAd] = useState<AdForm | null>(null);
  const [saving, setSaving] = useState(false);
  const [uploadingImage, setUploadingImage] = useState(false);

  const adsApi = useApi<any>(`/admin/ads?placement=${encodeURIComponent(placement)}&status=${encodeURIComponent(status)}&page=${adsPage}&pageSize=${adsPageSize}`, { items: [], stats: {} });
  const placementApi = useApi<any>(`/admin/ad-placements?page=${placementPage}&pageSize=${placementPageSize}`, { items: [] });

  const { rows, total: adsTotal } = normalizePagedRows(adsApi.data);
  const { rows: placementRows, total: placementTotal } = normalizePagedRows(placementApi.data);
  const stats = adsApi.data.stats ?? {};

  const placementOptions = useMemo(() => {
    const fromApi = placementRows.map((p: any) => p.code || p.Code).filter(Boolean);
    return Array.from(new Set([...defaultPlacements, ...fromApi]));
  }, [placementRows]);

  useEffect(() => {
    if (editingAd && !placementOptions.includes(editingAd.placement)) {
      setEditingAd((prev) => prev ? { ...prev, placement: placementOptions[0] || 'HOME_HERO' } : prev);
    }
  }, [editingAd, placementOptions]);

  function openCreate() {
    setEditingAd({ ...emptyForm, placement: placementOptions[0] || 'HOME_HERO' });
  }

  function openEdit(ad: any) {
    setEditingAd({
      id: ad.id,
      campaignName: ad.campaign?.name || ad.campaignName || 'Admin Ads',
      title: ad.title || '',
      description: ad.description || '',
      placement: ad.placement || ad.placementCode || 'HOME_HERO',
      imageUrl: ad.imageUrl || ad.desktopImageUrl || ad.mobileImageUrl || '',
      targetUrl: ad.targetUrl || '',
      status: ad.status || 'Active',
      sortOrder: Number(ad.sortOrder ?? 10),
      openInNewTab: Boolean(ad.openInNewTab ?? true),
      maxImpressions: Number(ad.maxImpressions ?? 0),
      maxClicks: Number(ad.maxClicks ?? 0),
      expiresAt: ad.expiresAt || ad.endDate || ad.endAt || ad.expiredDate || '',
    });
  }

  async function uploadAdImage(file: File) {
    if (!editingAd) return;
    if (!file.type.startsWith('image/')) return alert('Please choose an image file.');
    try {
      setUploadingImage(true);
      const asset = await apiClient.uploadMedia<any>(file);
      const imageUrl = asset?.url || asset?.Url || asset?.data?.url || '';
      if (!imageUrl) throw new Error('Upload finished but API did not return image URL.');
      setEditingAd((prev) => prev ? { ...prev, imageUrl } : prev);
    } catch (e) {
      alert((e as Error).message);
    } finally {
      setUploadingImage(false);
    }
  }

  async function saveAd() {
    if (!editingAd) return;
    if (!editingAd.title.trim()) return alert('Title is required.');
    if (!editingAd.placement.trim()) return alert('Placement is required.');

    const payload = {
      campaignName: editingAd.campaignName.trim() || 'Admin Ads',
      title: editingAd.title.trim(),
      description: editingAd.description.trim(),
      placement: editingAd.placement,
      imageUrl: editingAd.imageUrl.trim(),
      targetUrl: editingAd.targetUrl.trim(),
      status: editingAd.status,
      sortOrder: Number(editingAd.sortOrder || 0),
      openInNewTab: editingAd.openInNewTab,
      maxImpressions: Number(editingAd.maxImpressions || 0),
      maxClicks: Number(editingAd.maxClicks || 0),
      expiresAt: editingAd.expiresAt || null,
      endDate: editingAd.expiresAt || null,
    };

    try {
      setSaving(true);
      if (editingAd.id) await apiClient.post(`/admin/ads/${editingAd.id}/update`, payload);
      else await apiClient.post('/admin/ads', payload);
      setEditingAd(null);
      adsApi.load();
    } catch (e) {
      alert((e as Error).message);
    } finally {
      setSaving(false);
    }
  }

  async function moderate(id: string, nextStatus: string) {
    try {
      await apiClient.post(`/admin/ads/${id}/moderate`, { status: nextStatus, reason: `Set ${nextStatus}` });
      adsApi.load();
    } catch (e) {
      alert((e as Error).message);
    }
  }

  async function toggleAdStatus(ad: any) {
    const current = String(ad.status || (ad.isActive ? 'Active' : 'Inactive')).toLowerCase();
    const nextStatus = current === 'active' ? 'Inactive' : 'Active';
    await moderate(ad.id, nextStatus);
  }

  async function togglePlacement(id: string, isActive: boolean) {
    try {
      await apiClient.post(`/admin/ad-placements/${id}/toggle`, { isActive });
      placementApi.load();
    } catch (e) {
      alert((e as Error).message);
    }
  }

  const adColumns: AdminColumn<any>[] = [
    {
      key: 'ad',
      header: 'Ad',
      render: (a) => (
        <div className="listing-cell">
          <div className="listing-thumb ad-thumb" style={getAdImageUrl(a) ? { backgroundImage: `url(${getAdImageUrl(a)})` } : undefined}>{!getAdImageUrl(a) && 'AD'}</div>
          <div><strong>{a.title || 'Untitled ad'}</strong><small>{a.description || a.targetUrl || '-'}</small></div>
        </div>
      ),
    },
    { key: 'placement', header: 'Placement', render: (a) => a.placement || a.placementCode || '-' },
    { key: 'campaign', header: 'Campaign', render: (a) => a.campaign?.name || a.campaignName || '-' },
    { key: 'metrics', header: 'Metrics', render: (a) => `${a.currentImpressions ?? a.impressions ?? 0} views · ${a.currentClicks ?? a.clicks ?? 0} clicks` },
    { key: 'expiresAt', header: 'Expired Date', render: (a) => { const d = a.expiresAt || a.endDate || a.endAt || a.expiredDate; return d ? new Date(d).toLocaleDateString() : '-'; } },
    { key: 'status', header: 'Status', render: (a) => <StatusBadge value={a.status || (a.isActive ? 'Active' : 'Pending')} /> },
    {
      key: 'actions',
      header: '',
      width: '128px',
      render: (a) => {
        const isActive = String(a.status || (a.isActive ? 'Active' : 'Inactive')).toLowerCase() === 'active';
        return (
          <div className="row-actions ad-row-actions">
            <AdminIconButton icon="edit" label="Edit ad" onClick={() => openEdit(a)} />
            <AdminIconButton icon={isActive ? 'toggleOn' : 'toggleOff'} label={isActive ? 'Set inactive' : 'Set active'} className={isActive ? 'success-action' : ''} onClick={() => toggleAdStatus(a)} />
          </div>
        );
      },
    },
  ];

  const placementColumns: AdminColumn<any>[] = [
    { key: 'preview', header: 'Customer Site Position', render: (p) => <PlacementPreview code={p.code || p.Code} /> },
    { key: 'code', header: 'Code', render: (p) => <strong>{p.code || p.Code}</strong> },
    { key: 'name', header: 'Name', render: (p) => p.name || p.Name || '-' },
    { key: 'insertEvery', header: 'Insert Every', render: (p) => p.insertEvery || p.InsertEvery || '-' },
    { key: 'status', header: 'Status', render: (p) => <StatusBadge value={(p.isActive ?? p.IsActive) ? 'Active' : 'Inactive'} /> },
    { key: 'actions', header: '', width: '64px', render: (p) => <AdminIconButton icon={(p.isActive ?? p.IsActive) ? 'pause' : 'play'} label={(p.isActive ?? p.IsActive) ? 'Disable placement' : 'Enable placement'} onClick={() => togglePlacement(p.id || p.Id, !(p.isActive ?? p.IsActive))} /> },
  ];

  return (
    <>
      <PageHero eyebrow="ADS MANAGEMENT" title="Ads & Banners" description="Manage campaigns and placements shown across the customer site." actions={<AdminButton variant="primary" onClick={openCreate}>+ Add New Ad</AdminButton>} />
      <div className="metric-grid four"><StatCard label="Total Ads" value={stats.total ?? adsTotal} /><StatCard label="Active" value={stats.active ?? 0} tone="success" /><StatCard label="Pending" value={stats.pending ?? 0} tone="warning" /><StatCard label="Clicks" value={stats.clicks ?? 0} /></div>
      <section className="panel admin-tabs-panel">
        <div className="admin-tabs"><button className={tab === 'ads' ? 'active' : ''} onClick={() => setTab('ads')}>Ads List</button><button className={tab === 'placements' ? 'active' : ''} onClick={() => setTab('placements')}>Ad Placements</button></div>
        {tab === 'ads' ? (
          <AdminDataTable
            title="Ads List"
            rows={rows}
            columns={adColumns}
            loading={adsApi.loading}
            error={adsApi.err}
            emptyText="No ads found."
            actions={<AdminToolbar><AdminButton variant="primary" onClick={openCreate}>+ Add New Ad</AdminButton><AdminSelect value={placement} onChange={(e) => { setPlacement(e.target.value); setAdsPage(1); }}><option>All</option>{placementOptions.map((p) => <option key={p}>{p}</option>)}</AdminSelect><AdminSelect value={status} onChange={(e) => { setStatus(e.target.value); setAdsPage(1); }} options={['All', ...statuses]} /><AdminIconButton icon="refresh" label="Refresh ads" onClick={adsApi.load} /></AdminToolbar>}
            paging={{ page: adsPage, pageSize: adsPageSize, total: adsTotal, onPageChange: setAdsPage, onPageSizeChange: (n) => { setAdsPageSize(n); setAdsPage(1); } }}
          />
        ) : (
          <AdminDataTable
            title="Ad Placements"
            subtitle="Template view shows where each placement appears on the customer site."
            rows={placementRows}
            columns={placementColumns}
            loading={placementApi.loading}
            error={placementApi.err}
            emptyText="No placements found."
            actions={<AdminToolbar><AdminIconButton icon="refresh" label="Refresh placements" onClick={placementApi.load} /></AdminToolbar>}
            paging={{ page: placementPage, pageSize: placementPageSize, total: placementTotal, onPageChange: setPlacementPage, onPageSizeChange: (n) => { setPlacementPageSize(n); setPlacementPage(1); } }}
          />
        )}
      </section>

      {editingAd && (
        <div className="admin-modal-backdrop" onClick={() => !saving && setEditingAd(null)}>
          <div className="admin-modal-card" onClick={(e) => e.stopPropagation()}>
            <div className="panel-head">
              <div><h2>{editingAd.id ? 'Edit Ad' : 'Add New Ad'}</h2><p>Use one image as the ad background. You can paste an image URL or upload a file.</p></div>
              <AdminIconButton icon="x" label="Close" onClick={() => setEditingAd(null)} disabled={saving} />
            </div>
            <div className="form-grid ad-form-grid">
              <AdminTextBox label="Title" value={editingAd.title} onChange={(e) => setEditingAd({ ...editingAd, title: e.target.value })} />
              <AdminTextBox label="Campaign" value={editingAd.campaignName} onChange={(e) => setEditingAd({ ...editingAd, campaignName: e.target.value })} />
              <AdminSelect label="Placement" value={editingAd.placement} onChange={(e) => setEditingAd({ ...editingAd, placement: e.target.value })}>{placementOptions.map((p) => <option key={p}>{p}</option>)}</AdminSelect>
              <AdminSelect label="Status" value={editingAd.status} onChange={(e) => setEditingAd({ ...editingAd, status: e.target.value })} options={statuses} />
              <div className="admin-field span2 ad-image-field">
                <span>Image</span>
                <div className="ad-image-row">
                  <input className="admin-input" value={editingAd.imageUrl} onChange={(e) => setEditingAd({ ...editingAd, imageUrl: e.target.value })} placeholder="https://client-domain.com/banner.jpg or /media/..." />
                  <label className={`admin-btn ${uploadingImage ? 'is-disabled' : ''}`}>
                    <input type="file" accept="image/*" hidden disabled={uploadingImage} onChange={(e) => { const file = e.target.files?.[0]; if (file) uploadAdImage(file); e.currentTarget.value = ''; }} />
                    {uploadingImage ? 'Uploading...' : 'Upload'}
                  </label>
                </div>
              </div>
              <AdminTextBox label="Target URL" wrapperClassName="span2" value={editingAd.targetUrl} onChange={(e) => setEditingAd({ ...editingAd, targetUrl: e.target.value })} placeholder="/search?category=services" />
              <AdminTextArea label="Description" wrapperClassName="span2" value={editingAd.description} onChange={(e) => setEditingAd({ ...editingAd, description: e.target.value })} />
              <AdminTextBox label="Sort Order" type="number" value={editingAd.sortOrder} onChange={(e) => setEditingAd({ ...editingAd, sortOrder: Number(e.target.value) })} />
              <AdminTextBox label="Max Impressions" type="number" value={editingAd.maxImpressions} onChange={(e) => setEditingAd({ ...editingAd, maxImpressions: Number(e.target.value) })} />
              <AdminTextBox label="Max Clicks" type="number" value={editingAd.maxClicks} onChange={(e) => setEditingAd({ ...editingAd, maxClicks: Number(e.target.value) })} />
              <AdminTextBox label="Expired Date" type="date" value={editingAd.expiresAt ? String(editingAd.expiresAt).slice(0, 10) : ''} onChange={(e) => setEditingAd({ ...editingAd, expiresAt: e.target.value })} />
              <AdminCheckbox label="Open target in new tab" checked={editingAd.openInNewTab} onChange={(e) => setEditingAd({ ...editingAd, openInNewTab: e.target.checked })} />
            </div>
            {editingAd.imageUrl && <div className="ad-form-preview" style={{ backgroundImage: `url(${resolveAssetUrl(editingAd.imageUrl)})` }} />}
            <div className="modal-actions"><AdminButton onClick={() => setEditingAd(null)} disabled={saving}>Cancel</AdminButton><AdminButton variant="primary" onClick={saveAd} disabled={saving}>{saving ? 'Saving...' : 'Save Ad'}</AdminButton></div>
          </div>
        </div>
      )}
    </>
  );
}

function PlacementPreview({ code }: { code: string }) {
  const normalized = String(code || '').toUpperCase();
  return <div className={`placement-template ${normalized.toLowerCase().replace(/_/g, '-')}`}><span>{normalized || 'PLACEMENT'}</span><i /><b /></div>;
}
