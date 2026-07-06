import { useMemo, useState } from 'react';
import { PageHero } from '../components/common/AdminCommon';
import { AdminActionGroup, AdminButton, AdminSearchBox, AdminSelect, AdminTextArea, AdminTextBox, AdminToolbar } from '../components/common/AdminControls';
import { useApi } from '../hooks/useApi';
import { apiClient } from '../lib/api/apiClient';

type BlockedWord = {
  id?: string;
  word: string;
  language: string;
  severity: string;
  matchType: string;
  category: string;
  isActive: boolean;
  notes?: string;
  createdAt?: string;
};

const emptyForm: BlockedWord = {
  word: '',
  language: 'Any',
  severity: 'Medium',
  matchType: 'Contains',
  category: 'General',
  isActive: true,
  notes: '',
};

export function BlockedWordsPage() {
  const [q, setQ] = useState('');
  const [status, setStatus] = useState('All');
  const [form, setForm] = useState<BlockedWord>(emptyForm);
  const [saving, setSaving] = useState(false);
  const path = useMemo(() => `/admin/blocked-words?q=${encodeURIComponent(q)}&status=${encodeURIComponent(status)}&page=1&pageSize=100`, [q, status]);
  const api = useApi<any>(path, { items: [] });
  const items: BlockedWord[] = api.data?.items ?? [];

  function setValue<K extends keyof BlockedWord>(key: K, value: BlockedWord[K]) {
    setForm((prev) => ({ ...prev, [key]: value }));
  }

  async function save() {
    if (!form.word.trim()) return alert('Word is required.');
    setSaving(true);
    try {
      await apiClient.post('/admin/blocked-words', form);
      setForm(emptyForm);
      api.load();
    } catch (e) {
      alert((e as Error).message);
    } finally {
      setSaving(false);
    }
  }

  async function toggle(row: BlockedWord) {
    if (!row.id) return;
    await apiClient.post(`/admin/blocked-words/${row.id}/toggle`, {});
    api.load();
  }

  async function remove(row: BlockedWord) {
    if (!row.id) return;
    if (!confirm(`Delete blocked word "${row.word}"?`)) return;
    await apiClient.delete(`/admin/blocked-words/${row.id}`);
    api.load();
  }

  return (
    <>
      <PageHero
        eyebrow="CONTENT MODERATION"
        title="Blocked Words"
        description="Manage local keyword rules for Vietnamese, English, spam, contact bypass and marketplace-specific prohibited content. OpenAI moderation still runs separately."
        actions={<AdminButton variant="primary" onClick={save} disabled={saving}>{saving ? 'Saving...' : form.id ? 'Update Word' : 'Add Word'}</AdminButton>}
      />

      <section className="admin-card">
        <div className="section-header-row">
          <div>
            <h2>{form.id ? 'Edit blocked word' : 'Add blocked word'}</h2>
            <p>Use High severity to reject automatically. Low/Medium will send the listing to review.</p>
          </div>
          {form.id && <AdminButton onClick={() => setForm(emptyForm)}>New Word</AdminButton>}
        </div>
        <div className="ad-form-grid site-settings-grid">
          <AdminTextBox label="Word / Phrase / Regex" value={form.word} onChange={(e) => setValue('word', e.target.value)} placeholder="example: phone number, banned item, spam phrase" />
          <AdminTextBox label="Category" value={form.category} onChange={(e) => setValue('category', e.target.value)} placeholder="Spam / Adult / Illegal / Contact" />
          <AdminSelect label="Language" value={form.language} onChange={(e) => setValue('language', e.target.value)} options={['Any', 'vi', 'en']} />
          <AdminSelect label="Severity" value={form.severity} onChange={(e) => setValue('severity', e.target.value)} options={['Low', 'Medium', 'High']} />
          <AdminSelect label="Match Type" value={form.matchType} onChange={(e) => setValue('matchType', e.target.value)} options={['Contains', 'Exact', 'Regex']} />
          <AdminSelect label="Status" value={form.isActive ? 'Active' : 'Inactive'} onChange={(e) => setValue('isActive', e.target.value === 'Active')} options={['Active', 'Inactive']} />
          <AdminTextArea wrapperClassName="span2" label="Notes" value={form.notes || ''} onChange={(e) => setValue('notes', e.target.value)} placeholder="Internal note for moderators" />
        </div>
      </section>

      <section className="admin-card">
        <div className="section-header-row">
          <div>
            <h2>Keyword rules</h2>
            <p>{api.loading ? 'Loading...' : `${items.length} rule(s) loaded`}</p>
          </div>
          <AdminToolbar>
            <AdminSearchBox placeholder="Search word or category..." value={q} onChange={(e) => setQ(e.target.value)} />
            <AdminSelect value={status} onChange={(e) => setStatus(e.target.value)} options={['All', 'Active', 'Inactive']} />
            <AdminButton onClick={api.load}>Refresh</AdminButton>
          </AdminToolbar>
        </div>
        <div className="admin-table-wrap">
          <table className="admin-table">
            <thead>
              <tr>
                <th>Word</th>
                <th>Category</th>
                <th>Language</th>
                <th>Severity</th>
                <th>Match</th>
                <th>Status</th>
                <th>Actions</th>
              </tr>
            </thead>
            <tbody>
              {items.map((row) => (
                <tr key={row.id || row.word}>
                  <td><strong>{row.word}</strong><br /><small>{row.notes}</small></td>
                  <td>{row.category}</td>
                  <td>{row.language}</td>
                  <td><span className={`status-badge ${row.severity?.toLowerCase?.()}`}>{row.severity}</span></td>
                  <td>{row.matchType}</td>
                  <td>{row.isActive ? 'Active' : 'Inactive'}</td>
                  <td>
                    <AdminActionGroup>
                      <AdminButton size="sm" onClick={() => setForm({ ...emptyForm, ...row })}>Edit</AdminButton>
                      <AdminButton size="sm" onClick={() => toggle(row)}>{row.isActive ? 'Disable' : 'Enable'}</AdminButton>
                      <AdminButton size="sm" variant="danger" onClick={() => remove(row)}>Delete</AdminButton>
                    </AdminActionGroup>
                  </td>
                </tr>
              ))}
              {!items.length && <tr><td colSpan={7}>No blocked words found.</td></tr>}
            </tbody>
          </table>
        </div>
      </section>
    </>
  );
}
