import { useState } from 'react';
import { AdminDataTable, PageHero, StatusBadge, type AdminColumn, normalizePagedRows } from '../components/common/AdminCommon';
import { AdminActionGroup, AdminButton, AdminCheckbox, AdminIconButton, AdminTextBox, AdminToolbar } from '../components/common/AdminControls';
import { useApi } from '../hooks/useApi';
import { apiClient } from '../lib/api/apiClient';
import { formatPrice } from '../utils/format';

type PackageForm = { id?: string; name: string; price: string; durationDays: string; sortOrder: string; description: string; isActive: boolean };
const emptyPackage: PackageForm = { name: '', price: '', durationDays: '30', sortOrder: '100', description: '', isActive: true };
function packageStatus(x: any) { return x.isActive === false || String(x.status || '').toLowerCase().includes('inactive') ? 'Inactive' : 'Active'; }
function toForm(x: any): PackageForm { return { id: x.id, name: x.name || x.title || '', price: String(x.price ?? x.amount ?? ''), durationDays: String(x.durationDays ?? x.days ?? 30), sortOrder: String(x.sortOrder ?? x.order ?? 100), description: x.description || '', isActive: packageStatus(x) === 'Active' }; }

export function SettingsPage() {
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(10);
  const [form, setForm] = useState<PackageForm | null>(null);
  const packages = useApi<any>([`/admin/packages?page=${page}&pageSize=${pageSize}`, `/packages?page=${page}&pageSize=${pageSize}`, '/packages'], { items: [] });
  const { rows, total } = normalizePagedRows(packages.data);

  async function save() {
    if (!form?.name.trim()) return alert('Package name is required.');
    const body = { name: form.name, price: Number(form.price || 0), durationDays: Number(form.durationDays || 0), sortOrder: Number(form.sortOrder || 0), description: form.description, isActive: form.isActive, status: form.isActive ? 'Active' : 'Inactive' };
    try {
      if (form.id) await apiClient.post(`/admin/packages/${form.id}/update`, body).catch(() => apiClient.post(`/packages/${form.id}/update`, body));
      else await apiClient.post('/admin/packages', body).catch(() => apiClient.post('/packages', body));
      setForm(null); packages.load();
    } catch (e) { alert((e as Error).message); }
  }

  async function toggle(row: any) {
    const active = packageStatus(row) !== 'Active';
    try {
      await apiClient.post(`/admin/packages/${row.id}/status`, { isActive: active, status: active ? 'Active' : 'Inactive' }).catch(() => apiClient.post(`/packages/${row.id}/status`, { isActive: active, status: active ? 'Active' : 'Inactive' }));
      packages.load();
    } catch (e) { alert((e as Error).message); }
  }

  const columns: AdminColumn<any>[] = [
    { key: 'name', header: 'Package', render: (x) => <div className="message-listing"><strong>{x.name || x.title || '-'}</strong><small>{x.description || ''}</small></div> },
    { key: 'sortOrder', header: 'Order', render: (x) => x.sortOrder ?? x.order ?? '-' },
    { key: 'price', header: 'Price', render: (x) => formatPrice(x.currency || '$', x.price ?? x.amount) },
    { key: 'duration', header: 'Duration', render: (x) => `${x.durationDays ?? x.days ?? '-'} days` },
    { key: 'status', header: 'Status', render: (x) => <StatusBadge value={packageStatus(x)} /> },
    { key: 'actions', header: 'Actions', width: '120px', render: (x) => <AdminActionGroup><AdminIconButton icon="edit" label="Edit package" onClick={() => setForm(toForm(x))} /><AdminIconButton icon={packageStatus(x) === 'Active' ? 'toggleOn' : 'toggleOff'} label={packageStatus(x) === 'Active' ? 'Set inactive' : 'Set active'} className={packageStatus(x) === 'Active' ? 'success-action' : 'danger-action'} onClick={() => toggle(x)} /></AdminActionGroup> },
  ];

  return <><PageHero eyebrow="PACKAGE MANAGE" title="Package Manage" description="Manage listing packages, pricing and active status." actions={<AdminButton variant="primary" onClick={() => setForm(emptyPackage)}>+ Add Package</AdminButton>} /><AdminDataTable title="Package Management" rows={rows} columns={columns} loading={packages.loading} error={packages.err} emptyText="No packages found" actions={<AdminToolbar><AdminIconButton icon="refresh" label="Refresh" onClick={packages.load} /></AdminToolbar>} paging={{ page, pageSize, total, onPageChange: setPage, onPageSizeChange: (n) => { setPageSize(n); setPage(1); } }} />{form && <div className="admin-modal-backdrop"><section className="admin-modal-card"><h2>{form.id ? 'Edit Package' : 'Add Package'}</h2><div className="ad-form-grid"><AdminTextBox label="Name" value={form.name} onChange={(e) => setForm({ ...form, name: e.target.value })} /><AdminTextBox label="Price" type="number" value={form.price} onChange={(e) => setForm({ ...form, price: e.target.value })} /><AdminTextBox label="Duration Days" type="number" value={form.durationDays} onChange={(e) => setForm({ ...form, durationDays: e.target.value })} /><AdminTextBox label="Order" type="number" value={form.sortOrder} onChange={(e) => setForm({ ...form, sortOrder: e.target.value })} /><AdminCheckbox label="Active" checked={form.isActive} onChange={(e) => setForm({ ...form, isActive: e.target.checked })} /><AdminTextBox label="Description" wrapperClassName="span2" value={form.description} onChange={(e) => setForm({ ...form, description: e.target.value })} /></div><div className="modal-actions"><AdminButton onClick={() => setForm(null)}>Cancel</AdminButton><AdminButton variant="primary" onClick={save}>Save</AdminButton></div></section></div>}</>;
}
