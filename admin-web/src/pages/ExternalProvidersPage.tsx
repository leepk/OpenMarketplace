import { useMemo, useState } from 'react';
import { PageHero } from '../components/common/AdminCommon';
import { AdminButton, AdminCheckbox, AdminSelect, AdminTextBox } from '../components/common/AdminControls';
import { Icon } from '../components/common/Icon';
import { useApi } from '../hooks/useApi';
import { apiClient } from '../lib/api/apiClient';

type SettingsMap = Record<string, string>;
type ProviderCode = 'ebay' | 'amazon' | 'walmart' | 'aliexpress';

type ProviderDefinition = {
  code: ProviderCode;
  name: string;
  shortName: string;
  description: string;
  defaultPriority: number;
  live: boolean;
  accentClass: string;
};

const providers: ProviderDefinition[] = [
  { code: 'ebay', name: 'eBay', shortName: 'e', description: 'Browse API with eBay Partner Network affiliate tracking.', defaultPriority: 1, live: true, accentClass: 'ebay-logo' },
  { code: 'amazon', name: 'Amazon', shortName: 'a', description: 'Product Advertising API with Amazon Associates tracking.', defaultPriority: 2, live: false, accentClass: 'amazon-logo' },
  { code: 'walmart', name: 'Walmart', shortName: 'w', description: 'Product catalog and affiliate tracking for Walmart.', defaultPriority: 3, live: false, accentClass: 'walmart-logo' },
  { code: 'aliexpress', name: 'AliExpress', shortName: 'a', description: 'Affiliate product feed and tracked destination links.', defaultPriority: 4, live: false, accentClass: 'aliexpress-logo' },
];

const key = (provider: ProviderCode, name: string) => `external.${provider}.${name}`;
const globalKeys = {
  minimumLocalResults: 'external.minimum_local_results',
  maximumResults: 'external.maximum_results',
};

function pickSettings(data: any): SettingsMap {
  const raw = data?.settings ?? data?.data?.settings ?? data ?? {};
  if (Array.isArray(raw)) {
    return raw.reduce<SettingsMap>((acc, item) => {
      const itemKey = String(item?.key ?? item?.name ?? '');
      if (itemKey) acc[itemKey] = String(item?.value ?? '');
      return acc;
    }, {});
  }
  return { ...raw };
}

function truthy(value: string | undefined) {
  return String(value ?? '').toLowerCase() === 'true';
}

function normalizedInt(value: string | undefined, fallback: number, min: number, max: number) {
  const parsed = Number.parseInt(value ?? '', 10);
  return String(Number.isFinite(parsed) ? Math.min(max, Math.max(min, parsed)) : fallback);
}

export function ExternalProvidersPage() {
  const settingsApi = useApi<any>(['/admin/site-settings', '/site-settings'], { settings: {} });
  const initial = useMemo(() => pickSettings(settingsApi.data), [settingsApi.data]);
  const [form, setForm] = useState<SettingsMap>({});
  const [saving, setSaving] = useState(false);
  const [testing, setTesting] = useState(false);
  const [message, setMessage] = useState('');
  const [globalExpanded, setGlobalExpanded] = useState(true);
  const [expanded, setExpanded] = useState<Record<ProviderCode, boolean>>({
    ebay: true,
    amazon: false,
    walmart: false,
    aliexpress: false,
  });
  const values = { ...initial, ...form };
  const enabledCount = providers.filter((provider) => truthy(values[key(provider.code, 'enabled')])).length;

  function setValue(settingKey: string, value: string) {
    setForm((prev) => ({ ...prev, [settingKey]: value }));
  }

  function toggleProvider(provider: ProviderCode) {
    setExpanded((prev) => ({ ...prev, [provider]: !prev[provider] }));
  }

  async function save() {
    setSaving(true);
    setMessage('');
    try {
      const next: SettingsMap = { ...values };
      next[globalKeys.minimumLocalResults] = normalizedInt(values[globalKeys.minimumLocalResults], 10, 0, 1000);
      next[globalKeys.maximumResults] = normalizedInt(values[globalKeys.maximumResults], 100, 1, 100);
      for (const provider of providers) {
        next[key(provider.code, 'priority')] = normalizedInt(values[key(provider.code, 'priority')], provider.defaultPriority, 1, 999);
        next[key(provider.code, 'maximum_results')] = normalizedInt(values[key(provider.code, 'maximum_results')], 100, 1, 100);
        next[key(provider.code, 'cache_minutes')] = normalizedInt(values[key(provider.code, 'cache_minutes')], 30, 1, 1440);
      }
      await apiClient.post('/admin/site-settings', { settings: next });
      setForm({});
      await settingsApi.load();
      setMessage('External provider settings saved successfully. Blank secret fields kept their existing values.');
    } catch (error) {
      setMessage((error as Error).message);
    } finally {
      setSaving(false);
    }
  }

  async function testEbay() {
    setTesting(true);
    setMessage('');
    try {
      const result = await apiClient.get<any>('/external-listings/ebay/search?q=iphone&limit=1&force=true');
      const count = Array.isArray(result?.items) ? result.items.length : 0;
      setMessage(`eBay connection successful. Returned ${count} sample listing${count === 1 ? '' : 's'}.`);
    } catch (error) {
      setMessage((error as Error).message);
    } finally {
      setTesting(false);
    }
  }

  function renderProviderSettings(provider: ProviderDefinition) {
    const commonFields = (
      <div className="external-provider-grid external-provider-common-grid">
        <AdminTextBox label="Provider Maximum Results" type="number" min={1} max={100} value={values[key(provider.code, 'maximum_results')] || '100'} onChange={(e) => setValue(key(provider.code, 'maximum_results'), e.target.value)} />
        <AdminTextBox label="Cache Duration (minutes)" type="number" min={1} max={1440} value={values[key(provider.code, 'cache_minutes')] || '30'} onChange={(e) => setValue(key(provider.code, 'cache_minutes'), e.target.value)} />
      </div>
    );

    if (provider.code === 'ebay') {
      return (
        <>
          <div className="external-provider-grid">
            <AdminTextBox label="Production Client ID / App ID" value={values[key('ebay', 'client_id')] || ''} placeholder="Enter eBay production App ID" onChange={(e) => setValue(key('ebay', 'client_id'), e.target.value)} />
            <AdminTextBox label="Production Client Secret / Cert ID" type="password" value={form[key('ebay', 'client_secret')] ?? ''} placeholder="Leave blank to keep existing secret" autoComplete="new-password" onChange={(e) => setValue(key('ebay', 'client_secret'), e.target.value)} />
            <AdminTextBox label="EPN Campaign ID" value={values[key('ebay', 'campaign_id')] || ''} placeholder="Affiliate campaign ID" onChange={(e) => setValue(key('ebay', 'campaign_id'), e.target.value)} />
            <AdminSelect label="Marketplace" value={values[key('ebay', 'marketplace_id')] || 'EBAY_US'} options={[{ value: 'EBAY_US', label: 'United States (EBAY_US)' }, { value: 'EBAY_CA', label: 'Canada (EBAY_CA)' }, { value: 'EBAY_GB', label: 'United Kingdom (EBAY_GB)' }, { value: 'EBAY_AU', label: 'Australia (EBAY_AU)' }]} onChange={(e) => setValue(key('ebay', 'marketplace_id'), e.target.value)} />
          </div>
          {commonFields}
        </>
      );
    }

    if (provider.code === 'amazon') {
      return (
        <>
          <div className="external-provider-grid">
            <AdminTextBox label="Access Key" value={values[key('amazon', 'access_key')] || ''} placeholder="Amazon Product Advertising API access key" onChange={(e) => setValue(key('amazon', 'access_key'), e.target.value)} />
            <AdminTextBox label="Secret Key" type="password" value={form[key('amazon', 'secret_key')] ?? ''} placeholder="Leave blank to keep existing secret" autoComplete="new-password" onChange={(e) => setValue(key('amazon', 'secret_key'), e.target.value)} />
            <AdminTextBox label="Associate Tag" value={values[key('amazon', 'associate_tag')] || ''} placeholder="Example: vunoca-20" onChange={(e) => setValue(key('amazon', 'associate_tag'), e.target.value)} />
            <AdminSelect label="Marketplace" value={values[key('amazon', 'marketplace')] || 'www.amazon.com'} options={[{ value: 'www.amazon.com', label: 'United States (amazon.com)' }, { value: 'www.amazon.ca', label: 'Canada (amazon.ca)' }, { value: 'www.amazon.co.uk', label: 'United Kingdom (amazon.co.uk)' }]} onChange={(e) => setValue(key('amazon', 'marketplace'), e.target.value)} />
          </div>
          {commonFields}
        </>
      );
    }

    if (provider.code === 'walmart') {
      return (
        <>
          <div className="external-provider-grid">
            <AdminTextBox label="API Key / Client ID" value={values[key('walmart', 'api_key')] || ''} placeholder="Walmart API credential" onChange={(e) => setValue(key('walmart', 'api_key'), e.target.value)} />
            <AdminTextBox label="Client Secret" type="password" value={form[key('walmart', 'client_secret')] ?? ''} placeholder="Leave blank to keep existing secret" autoComplete="new-password" onChange={(e) => setValue(key('walmart', 'client_secret'), e.target.value)} />
            <AdminTextBox label="Publisher ID" value={values[key('walmart', 'publisher_id')] || ''} placeholder="Affiliate publisher or partner ID" onChange={(e) => setValue(key('walmart', 'publisher_id'), e.target.value)} />
            <AdminTextBox label="Tracking / Sub ID" value={values[key('walmart', 'tracking_id')] || ''} placeholder="Optional campaign tracking ID" onChange={(e) => setValue(key('walmart', 'tracking_id'), e.target.value)} />
          </div>
          {commonFields}
        </>
      );
    }

    return (
      <>
        <div className="external-provider-grid">
          <AdminTextBox label="App Key" value={values[key('aliexpress', 'app_key')] || ''} placeholder="AliExpress affiliate App Key" onChange={(e) => setValue(key('aliexpress', 'app_key'), e.target.value)} />
          <AdminTextBox label="App Secret" type="password" value={form[key('aliexpress', 'app_secret')] ?? ''} placeholder="Leave blank to keep existing secret" autoComplete="new-password" onChange={(e) => setValue(key('aliexpress', 'app_secret'), e.target.value)} />
          <AdminTextBox label="Tracking ID" value={values[key('aliexpress', 'tracking_id')] || ''} placeholder="Affiliate tracking ID" onChange={(e) => setValue(key('aliexpress', 'tracking_id'), e.target.value)} />
          <AdminTextBox label="Affiliate / Publisher ID" value={values[key('aliexpress', 'publisher_id')] || ''} placeholder="Optional publisher identifier" onChange={(e) => setValue(key('aliexpress', 'publisher_id'), e.target.value)} />
        </div>
        {commonFields}
      </>
    );
  }

  return (
    <>
      <PageHero eyebrow="EXTERNAL LISTINGS" title="External Providers" description="Configure affiliate marketplaces now and enable each connector only when its credentials and backend adapter are ready." actions={<div className="external-provider-actions"><AdminButton onClick={settingsApi.load} disabled={settingsApi.loading}>Refresh</AdminButton><AdminButton variant="primary" onClick={save} disabled={saving}>{saving ? 'Saving...' : 'Save Settings'}</AdminButton></div>} />

      <div className="external-provider-overview">
        <div><span>Providers</span><strong>{providers.length}</strong></div>
        <div><span>Enabled</span><strong>{enabledCount}</strong></div>
        <div><span>Live adapters</span><strong>{providers.filter((provider) => provider.live).length}</strong></div>
        <div><span>External limit</span><strong>{values[globalKeys.maximumResults] || '100'}</strong></div>
      </div>

      <section className={`admin-card external-provider-collapsible ${globalExpanded ? 'is-expanded' : ''}`}>
        <button type="button" className="external-provider-collapse-header" onClick={() => setGlobalExpanded((value) => !value)} aria-expanded={globalExpanded}>
          <div className="external-provider-header-icon"><Icon name="settings" size={20} /></div>
          <div className="external-provider-collapse-copy"><strong>Global Result Rules</strong><span>Local listings are always returned first. Enabled providers run by ascending priority.</span></div>
          <Icon name="chevron-right" size={20} className="external-provider-chevron" />
        </button>
        {globalExpanded && <div className="external-provider-collapse-body"><div className="external-provider-grid external-provider-rule-grid"><AdminTextBox label="Minimum Local Results" type="number" min={0} max={1000} value={values[globalKeys.minimumLocalResults] || '10'} onChange={(event) => setValue(globalKeys.minimumLocalResults, event.target.value)} /><AdminTextBox label="Maximum External Results" type="number" min={1} max={100} value={values[globalKeys.maximumResults] || '100'} onChange={(event) => setValue(globalKeys.maximumResults, event.target.value)} /></div><div className="external-provider-flow"><b>Local listings</b><Icon name="chevron-right" size={15} /><span>Minimum threshold check</span><Icon name="chevron-right" size={15} /><span>Enabled providers by priority</span><Icon name="chevron-right" size={15} /><b>Unified response</b></div></div>}
      </section>

      <section className="external-provider-list">
        {providers.map((provider) => {
          const enabledKey = key(provider.code, 'enabled');
          const priorityKey = key(provider.code, 'priority');
          const enabled = truthy(values[enabledKey]);
          const isExpanded = expanded[provider.code];
          return (
            <article className={`admin-card external-provider-collapsible external-provider-card-${provider.code} ${isExpanded ? 'is-expanded' : ''}`} key={provider.code}>
              <div className="external-provider-provider-row">
                <button type="button" className="external-provider-collapse-header external-provider-provider-header" onClick={() => toggleProvider(provider.code)} aria-expanded={isExpanded}>
                  <div className={`external-provider-logo ${provider.accentClass}`}>{provider.shortName}</div>
                  <div className="external-provider-collapse-copy"><strong>{provider.name}</strong><span>{provider.description}</span><small>{provider.live ? 'Live backend adapter available.' : 'Credentials can be saved now; live backend adapter will be connected later.'}</small></div>
                  <Icon name="chevron-right" size={20} className="external-provider-chevron" />
                </button>
                <div className="external-provider-quick-controls">
                  <AdminTextBox label="Priority" type="number" min={1} max={999} value={values[priorityKey] || String(provider.defaultPriority)} onChange={(event) => setValue(priorityKey, event.target.value)} />
                  <AdminCheckbox label="Enabled" checked={enabled} onChange={(event) => setValue(enabledKey, String(event.target.checked))} />
                  <span className={`status-badge ${enabled ? 'good' : 'warn'}`}>{enabled ? 'Enabled' : 'Disabled'}</span>
                </div>
              </div>
              {isExpanded && <div className="external-provider-collapse-body"><div className="external-provider-body-heading"><div><h3>API & Affiliate Settings</h3><p>Save credentials now or leave them blank until the provider account is approved. Secret fields are never displayed again.</p></div>{provider.live ? <AdminButton onClick={testEbay} disabled={testing || !enabled}><Icon name="refresh" size={16} /> {testing ? 'Testing...' : 'Test Connection'}</AdminButton> : <span className="external-provider-adapter-badge"><Icon name="settings" size={15} /> Adapter pending</span>}</div>{renderProviderSettings(provider)}<div className="external-provider-secret-note"><Icon name="lock" size={15} /><span>Leaving a secret field blank keeps the value already stored on the backend.</span></div></div>}
            </article>
          );
        })}
      </section>

      {message && <div className="external-provider-message" role="status">{message}</div>}
    </>
  );
}
