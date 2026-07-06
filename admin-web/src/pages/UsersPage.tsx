import { useState } from 'react';
import { AdminDataTable, PageHero, StatusBadge, StatCard, type AdminColumn, normalizePagedRows } from '../components/common/AdminCommon';
import { AdminActionGroup, AdminButton, AdminIconButton, AdminSearchBox, AdminSelect, AdminTextBox, AdminToolbar } from '../components/common/AdminControls';
import { useApi } from '../hooks/useApi';
import { apiClient } from '../lib/api/apiClient';
import { getInitials } from '../utils/format';

type UserForm = { id?: string; name: string; email: string; role: string; source: string; status: string; password?: string };
const emptyUserForm: UserForm = { name: '', email: '', role: 'Customer', source: 'AdminCreated', status: 'Active', password: '' };

function isActiveUser(u: any) { return String(u.status || 'Active').toLowerCase() === 'active'; }

export function UsersPage() {
  const [role, setRole] = useState('All');
  const [source, setSource] = useState('All');
  const [status, setStatus] = useState('All');
  const [q, setQ] = useState('');
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(10);
  const [form, setForm] = useState<UserForm | null>(null);
  const path = `/admin/users?role=${encodeURIComponent(role)}&source=${encodeURIComponent(source)}&status=${encodeURIComponent(status)}&q=${encodeURIComponent(q)}&page=${page}&pageSize=${pageSize}`;
  const { data, loading, err, load } = useApi<any>(path, { items: [], stats: {} });
  const { rows, total } = normalizePagedRows(data);
  const stats = data.stats ?? {};

  async function updateUser(u: any, nextRole: string, nextSource: string, nextStatus: string) {
    try {
      await apiClient.post(`/admin/users/${u.id}/role-source`, { role: nextRole, source: nextSource, status: nextStatus });
      load();
    } catch (e) {
      alert((e as Error).message);
    }
  }

  async function saveUser() {
    if (!form?.email.trim()) return alert('Email is required.');
    try {
      if (form.id) await apiClient.post(`/admin/users/${form.id}/update`, form).catch(() => apiClient.post(`/admin/users/${form.id}/role-source`, form));
      else await apiClient.post('/admin/users', form).catch(() => apiClient.post('/admin/users/create', form));
      setForm(null); load();
    } catch (e) { alert((e as Error).message); }
  }

  function editUser(u: any) {
    setForm({ id: u.id, name: u.name || '', email: u.email || '', role: u.role || 'Customer', source: u.source || 'WebCustomer', status: u.status || 'Active', password: '' });
  }

  async function toggleUser(u: any) {
    await updateUser(u, u.role || 'Customer', u.source || 'WebCustomer', isActiveUser(u) ? 'Inactive' : 'Active');
  }

  const columns: AdminColumn<any>[] = [
    {
      key: 'user',
      header: 'User',
      render: (u) => (
        <div className="user-cell">
          <div className="avatar">{getInitials(u.name || u.email || 'U')}</div>
          <div>
            <strong>{u.name || 'Unnamed user'}</strong>
            <small>{u.email}</small>
          </div>
        </div>
      ),
    },
    { key: 'role', header: 'Role', render: (u) => <StatusBadge value={u.role || 'Customer'} /> },
    { key: 'source', header: 'Source', render: (u) => u.source || 'WebCustomer' },
    { key: 'status', header: 'Status', render: (u) => <StatusBadge value={u.status || 'Active'} /> },
    { key: 'createdAt', header: 'Joined', render: (u) => u.createdAt ? new Date(u.createdAt).toLocaleDateString() : '-' },
    {
      key: 'controls',
      header: 'Manage',
      width: '220px',
      render: (u) => (
        <AdminActionGroup className="inline-controls">
          <AdminIconButton icon="edit" label="Edit user" onClick={() => editUser(u)} />
          <AdminIconButton icon={isActiveUser(u) ? 'toggleOn' : 'toggleOff'} label={isActiveUser(u) ? 'Set inactive' : 'Set active'} className={isActiveUser(u) ? 'success-action' : 'danger-action'} onClick={() => toggleUser(u)} />
        </AdminActionGroup>
      ),
    },
  ];

  return (
    <>
      <PageHero eyebrow="USER MANAGEMENT" title="Users" description="Manage customer, admin-created and system-managed accounts." actions={<AdminButton variant="primary" onClick={() => setForm(emptyUserForm)}>+ Add User</AdminButton>} />
      <div className="metric-grid four"><StatCard label="Total Users" value={stats.total ?? rows.length} /><StatCard label="Web Customer" value={stats.webCustomer ?? 0} /><StatCard label="Admin Created" value={stats.adminCreated ?? 0} /><StatCard label="System Managed" value={stats.systemManaged ?? 0} /></div>
      <AdminDataTable
        title="Users Management"
        rows={rows}
        columns={columns}
        loading={loading}
        error={err}
        emptyText="No users found."
        actions={
          <AdminToolbar>
            <AdminSearchBox value={q} onChange={(e) => { setQ(e.target.value); setPage(1); }} placeholder="Search name or email..." />
            <AdminSelect value={role} onChange={(e) => { setRole(e.target.value); setPage(1); }} options={['All', 'Customer', 'Seller', 'Admin', 'SuperAdmin', 'System']} />
            <AdminSelect value={source} onChange={(e) => { setSource(e.target.value); setPage(1); }} options={['All', 'WebCustomer', 'AdminCreated', 'SystemManaged', 'Imported']} />
            <AdminSelect value={status} onChange={(e) => { setStatus(e.target.value); setPage(1); }} options={['All', 'Active', 'Pending', 'Banned', 'Suspended']} />
            <AdminIconButton icon="refresh" label="Refresh" onClick={load} />
          </AdminToolbar>
        }
        paging={{ page, pageSize, total, onPageChange: setPage, onPageSizeChange: (n) => { setPageSize(n); setPage(1); } }}
      />
      {form && <div className="admin-modal-backdrop"><section className="admin-modal-card"><h2>{form.id ? 'Edit User' : 'Add User'}</h2><div className="ad-form-grid"><AdminTextBox label="Name" value={form.name} onChange={(e) => setForm({ ...form, name: e.target.value })} /><AdminTextBox label="Email" value={form.email} onChange={(e) => setForm({ ...form, email: e.target.value })} /><AdminSelect label="Role" value={form.role} onChange={(e) => setForm({ ...form, role: e.target.value })} options={['Customer', 'Seller', 'Admin', 'SuperAdmin', 'System']} /><AdminSelect label="Source" value={form.source} onChange={(e) => setForm({ ...form, source: e.target.value })} options={['WebCustomer', 'AdminCreated', 'SystemManaged', 'Imported']} /><AdminSelect label="Status" value={form.status} onChange={(e) => setForm({ ...form, status: e.target.value })} options={['Active', 'Inactive', 'Pending', 'Banned', 'Suspended']} /><AdminTextBox label="Password" type="password" value={form.password || ''} onChange={(e) => setForm({ ...form, password: e.target.value })} placeholder={form.id ? 'Leave blank to keep current' : 'Temporary password'} /></div><div className="modal-actions"><AdminButton onClick={() => setForm(null)}>Cancel</AdminButton><AdminButton variant="primary" onClick={saveUser}>Save</AdminButton></div></section></div>}
    </>
  );
}
