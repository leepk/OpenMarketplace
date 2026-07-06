import { useState } from 'react';
import { AdminDataTable, PageHero, StatusBadge, type AdminColumn, normalizePagedRows } from '../components/common/AdminCommon';
import { AdminActionGroup, AdminButton, AdminCheckbox, AdminIconButton, AdminSearchBox, AdminTextBox, AdminToolbar } from '../components/common/AdminControls';
import { useApi } from '../hooks/useApi';
import { apiClient } from '../lib/api/apiClient';

type CategoryForm = { id?: string; name: string; slug: string; description: string; isActive: boolean };
const emptyForm: CategoryForm = { name: '', slug: '', description: '', isActive: true };

function categoryStatus(x: any) { return x.isActive === false || String(x.status || '').toLowerCase().includes('inactive') ? 'Inactive' : 'Active'; }
function toForm(x: any): CategoryForm { return { id: x.id, name: x.name || '', slug: x.slug || '', description: x.description || '', isActive: categoryStatus(x) === 'Active' }; }

export function CategoriesPage() {
  const [q, setQ] = useState('');
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(10);
  const [form, setForm] = useState<CategoryForm | null>(null);
  const paths = [`/admin/categories?q=${encodeURIComponent(q)}&page=${page}&pageSize=${pageSize}`, `/categories?q=${encodeURIComponent(q)}&page=${page}&pageSize=${pageSize}`, '/categories'];
  const { data, loading, err, load } = useApi<any>(paths, { items: [] });
  const { rows, total } = normalizePagedRows(data);

  async function save() {
    if (!form?.name.trim()) return alert('Category name is required.');
    const body = { name: form.name, slug: form.slug || form.name.toLowerCase().replace(/[^a-z0-9]+/g, '-').replace(/^-|-$/g, ''), description: form.description, isActive: form.isActive, status: form.isActive ? 'Active' : 'Inactive' };
    try {
      if (form.id) await apiClient.post(`/admin/categories/${form.id}/update`, body).catch(() => apiClient.post(`/categories/${form.id}/update`, body));
      else await apiClient.post('/admin/categories', body).catch(() => apiClient.post('/categories', body));
      setForm(null); load();
    } catch (e) { alert((e as Error).message); }
  }

  async function toggle(row: any) {
    const active = categoryStatus(row) !== 'Active';
    try {
      await apiClient.post(`/admin/categories/${row.id}/status`, { isActive: active, status: active ? 'Active' : 'Inactive' }).catch(() => apiClient.post(`/categories/${row.id}/status`, { isActive: active, status: active ? 'Active' : 'Inactive' }));
      load();
    } catch (e) { alert((e as Error).message); }
  }

  const columns: AdminColumn<any>[] = [
    { key: 'name', header: 'Category', render: (x) => <div className="message-listing"><strong>{x.name || '-'}</strong><small>{x.slug || x.description || ''}</small></div> },
    { key: 'description', header: 'Description', render: (x) => x.description || '-' },
    { key: 'status', header: 'Status', render: (x) => <StatusBadge value={categoryStatus(x)} /> },
    { key: 'actions', header: 'Actions', width: '120px', render: (x) => <AdminActionGroup><AdminIconButton icon="edit" label="Edit category" onClick={() => setForm(toForm(x))} /><AdminIconButton icon={categoryStatus(x) === 'Active' ? 'toggleOn' : 'toggleOff'} label={categoryStatus(x) === 'Active' ? 'Set inactive' : 'Set active'} className={categoryStatus(x) === 'Active' ? 'success-action' : 'danger-action'} onClick={() => toggle(x)} /></AdminActionGroup> },
  ];

  return <>
    <PageHero eyebrow="MARKETPLACE SETUP" title="Categories" description="Manage listing categories and subcategories." actions={<AdminButton variant="primary" onClick={() => setForm(emptyForm)}>+ Add Category</AdminButton>} />
    <AdminDataTable title="Categories Management" rows={rows} columns={columns} loading={loading} error={err} emptyText="No categories found." actions={<AdminToolbar><AdminSearchBox value={q} onChange={(e) => { setQ(e.target.value); setPage(1); }} placeholder="Search categories..." /><AdminIconButton icon="refresh" label="Refresh" onClick={load} /></AdminToolbar>} paging={{ page, pageSize, total, onPageChange: setPage, onPageSizeChange: (n) => { setPageSize(n); setPage(1); } }} />
    {form && <div className="admin-modal-backdrop"><section className="admin-modal-card"><h2>{form.id ? 'Edit Category' : 'Add Category'}</h2><div className="ad-form-grid"><AdminTextBox label="Name" value={form.name} onChange={(e) => setForm({ ...form, name: e.target.value })} /><AdminTextBox label="Slug" value={form.slug} onChange={(e) => setForm({ ...form, slug: e.target.value })} /><AdminTextBox label="Description" wrapperClassName="span2" value={form.description} onChange={(e) => setForm({ ...form, description: e.target.value })} /><AdminCheckbox label="Active" checked={form.isActive} onChange={(e) => setForm({ ...form, isActive: e.target.checked })} /></div><div className="modal-actions"><AdminButton onClick={() => setForm(null)}>Cancel</AdminButton><AdminButton variant="primary" onClick={save}>Save</AdminButton></div></section></div>}
  </>;
}
