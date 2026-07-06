import { useMemo, useState } from 'react';
import { PageHero } from '../components/common/AdminCommon';
import { AdminButton, AdminTextBox, AdminToolbar } from '../components/common/AdminControls';
import { useApi } from '../hooks/useApi';
import { apiClient } from '../lib/api/apiClient';
import { appConfig } from '../lib/config';

type SiteSettingForm = Record<string, string>;

const fieldGroups: Array<{ title: string; description: string; fields: Array<{ key: string; label: string; type?: string; upload?: boolean; placeholder?: string }> }> = [
  {
    title: 'Branding',
    description: 'Logo, browser icon and customer website colors.',
    fields: [
      { key: 'site.name', label: 'Website Name', placeholder: 'OpenMarketplace' },
      { key: 'site.logo_url', label: 'Homepage Logo', upload: true, placeholder: '/site/logo-openmarketplace.svg' },
      { key: 'site.favicon_url', label: 'Browser / Taskbar Logo', upload: true, placeholder: '/site/favicon-openmarketplace.svg' },
      { key: 'site.primary_color', label: 'Primary Color', type: 'color' },
      { key: 'site.secondary_color', label: 'Secondary Color', type: 'color' },
    ],
  },
  {
    title: 'Social Links',
    description: 'Links used by customer footer/header social icons.',
    fields: [
      { key: 'social.facebook_url', label: 'Facebook Link', placeholder: 'https://facebook.com/your-page' },
      { key: 'social.youtube_url', label: 'YouTube Link', placeholder: 'https://youtube.com/@your-channel' },
      { key: 'social.instagram_url', label: 'Instagram Link', placeholder: 'https://instagram.com/your-page' },
    ],
  },
  {
    title: 'Contact & Footer',
    description: 'Public contact information shown to customer users.',
    fields: [
      { key: 'contact.email', label: 'Contact Email', placeholder: 'support@example.com' },
      { key: 'contact.phone', label: 'Contact Phone', placeholder: '(408) 555-0100' },
      { key: 'contact.address', label: 'Address', placeholder: 'Santa Clara, CA' },
      { key: 'footer.text', label: 'Footer Text', placeholder: '© OpenMarketplace. All rights reserved.' },
    ],
  },
  {
    title: 'SEO',
    description: 'Default title and description for customer pages.',
    fields: [
      { key: 'seo.title', label: 'SEO Title', placeholder: 'OpenMarketplace - Local Classifieds' },
      { key: 'seo.description', label: 'SEO Description', placeholder: 'Buy, sell and discover local listings near you.' },
    ],
  },
];

function pickSettings(data: any): SiteSettingForm {
  return { ...(data?.settings ?? data?.branding ?? {}) };
}

function resolveUrl(url: string) {
  if (!url) return '';
  if (/^https?:\/\//i.test(url) || url.startsWith('data:')) return url;
  const apiRoot = appConfig.apiBaseUrl.replace(/\/api\/v1\/?$/i, '');
  return `${apiRoot}${url.startsWith('/') ? url : `/${url}`}`;
}

export function SiteSettingsPage() {
  const settingsApi = useApi<any>(['/admin/site-settings', '/site-settings'], { settings: {} });
  const initial = useMemo(() => pickSettings(settingsApi.data), [settingsApi.data]);
  const [form, setForm] = useState<SiteSettingForm>({});
  const [saving, setSaving] = useState(false);
  const values = { ...initial, ...form };

  function setValue(key: string, value: string) {
    setForm((prev) => ({ ...prev, [key]: value }));
  }

  async function upload(key: string, file?: File | null) {
    if (!file) return;
    try {
      const result: any = await apiClient.uploadMedia(file);
      const url = result?.url || result?.data?.url || result?.asset?.url || '';
      if (!url) throw new Error('Upload did not return image URL.');
      setValue(key, url);
    } catch (e) {
      alert((e as Error).message);
    }
  }

  async function save() {
    setSaving(true);
    try {
      await apiClient.post('/admin/site-settings', { settings: values });
      setForm({});
      settingsApi.load();
    } catch (e) {
      alert((e as Error).message);
    } finally {
      setSaving(false);
    }
  }

  return (
    <>
      <PageHero
        eyebrow="SITE SETTINGS"
        title="Site Settings"
        description="Configure customer website name, logo, colors, social links, contact info and SEO from the database."
        actions={<AdminButton variant="primary" onClick={save} disabled={saving}>{saving ? 'Saving...' : 'Save Settings'}</AdminButton>}
      />

      <section className="site-settings-preview admin-card">
        <div className="site-preview-brand" style={{ ['--preview-primary' as any]: values['site.primary_color'] || '#2563eb' }}>
          {values['site.logo_url'] ? <img src={resolveUrl(values['site.logo_url'])} alt="Website logo preview" /> : <span className="site-preview-logo">OM</span>}
          <div>
            <strong>{values['site.name'] || 'OpenMarketplace'}</strong>
            <small>{values['seo.title'] || 'Customer website preview'}</small>
          </div>
        </div>
        <div className="site-preview-colors">
          <span style={{ background: values['site.primary_color'] || '#2563eb' }} />
          <span style={{ background: values['site.secondary_color'] || '#f59e0b' }} />
        </div>
      </section>

      {fieldGroups.map((group) => (
        <section className="admin-card site-settings-card" key={group.title}>
          <div className="section-header-row">
            <div>
              <h2>{group.title}</h2>
              <p>{group.description}</p>
            </div>
            <AdminToolbar><AdminButton onClick={settingsApi.load}>Refresh</AdminButton></AdminToolbar>
          </div>
          <div className="ad-form-grid site-settings-grid">
            {group.fields.map((field) => (
              <div className={field.upload ? 'site-setting-upload span2' : ''} key={field.key}>
                <AdminTextBox
                  label={field.label}
                  type={field.type || 'text'}
                  value={values[field.key] || ''}
                  placeholder={field.placeholder}
                  onChange={(e) => setValue(field.key, e.target.value)}
                />
                {field.upload && (
                  <div className="upload-line">
                    <input type="file" accept="image/*" onChange={(e) => upload(field.key, e.target.files?.[0])} />
                    {values[field.key] && <img src={resolveUrl(values[field.key])} alt={`${field.label} preview`} />}
                  </div>
                )}
              </div>
            ))}
          </div>
        </section>
      ))}
    </>
  );
}
