import { useMemo, useState } from 'react';
import { PageHero } from '../components/common/AdminCommon';
import { AdminButton, AdminCheckbox, AdminSelect, AdminTextArea, AdminTextBox, AdminToolbar } from '../components/common/AdminControls';
import { useApi } from '../hooks/useApi';
import { apiClient } from '../lib/api/apiClient';
import { appConfig } from '../lib/config';

type SiteSettingForm = Record<string, string>;
type SettingField = { key: string; label: string; type?: string; upload?: boolean; placeholder?: string; control?: 'checkbox' | 'textarea' | 'select'; options?: string[]; span2?: boolean };

const fieldGroups: Array<{ title: string; description: string; fields: SettingField[] }> = [
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
    title: 'Moderation',
    description: 'OpenAI moderation and review thresholds for listing text and images.',
    fields: [
      { key: 'moderation.ai_enabled', label: 'Enable AI Moderation', placeholder: 'true' },
      { key: 'moderation.auto_approve_safe', label: 'Auto Approve Safe Listings', placeholder: 'true' },
      { key: 'moderation.review_threshold', label: 'Review Threshold', placeholder: '0.45' },
      { key: 'moderation.reject_threshold', label: 'Reject Threshold', placeholder: '0.85' },
    ],
  },

  {
    title: 'Authentication Providers',
    description: 'Enable customer sign-in methods and configure OAuth applications.',
    fields: [
      { key: 'auth.email_enabled', label: 'Enable Email / Password', control: 'checkbox' },
      { key: 'auth.google_enabled', label: 'Enable Google Login', control: 'checkbox' },
      { key: 'auth.google_client_id', label: 'Google Client ID', placeholder: 'Google OAuth client ID' },
      { key: 'auth.google_client_secret', label: 'Google Client Secret', type: 'password', placeholder: 'Leave masked value unchanged' },
      { key: 'auth.facebook_enabled', label: 'Enable Facebook Login', control: 'checkbox' },
      { key: 'auth.facebook_app_id', label: 'Facebook App ID', placeholder: 'Facebook App ID' },
      { key: 'auth.facebook_app_secret', label: 'Facebook App Secret', type: 'password', placeholder: 'Leave masked value unchanged' },
      { key: 'auth.auto_create_user', label: 'Auto-create User on First Social Login', control: 'checkbox' },
    ],
  },
  {
    title: 'Payment Providers',
    description: 'Choose checkout methods, credentials, environment and default currency.',
    fields: [
      { key: 'payment.default_provider', label: 'Default Provider', control: 'select', options: ['STRIPE', 'PAYPAL', 'MANUAL'] },
      { key: 'payment.currency', label: 'Default Currency', placeholder: 'USD' },
      { key: 'payment.stripe_enabled', label: 'Enable Stripe', control: 'checkbox' },
      { key: 'payment.stripe_publishable_key', label: 'Stripe Publishable Key', placeholder: 'pk_...' },
      { key: 'payment.stripe_secret_key', label: 'Stripe Secret Key', type: 'password', placeholder: 'sk_...' },
      { key: 'payment.stripe_webhook_secret', label: 'Stripe Webhook Secret', type: 'password', placeholder: 'whsec_...' },
      { key: 'payment.paypal_enabled', label: 'Enable PayPal', control: 'checkbox' },
      { key: 'payment.paypal_client_id', label: 'PayPal Client ID', placeholder: 'PayPal client ID' },
      { key: 'payment.paypal_secret', label: 'PayPal Secret', type: 'password', placeholder: 'Leave masked value unchanged' },
      { key: 'payment.paypal_mode', label: 'PayPal Mode', control: 'select', options: ['Sandbox', 'Live'] },
      { key: 'payment.manual_enabled', label: 'Enable Manual Payment', control: 'checkbox' },
      { key: 'payment.manual_instructions', label: 'Manual Payment Instructions', control: 'textarea', span2: true, placeholder: 'Payment instructions shown to customers' },
    ],
  },
  {
    title: 'Email Settings',
    description: 'Configure SMTP delivery used for account, listing and payment notifications.',
    fields: [
      { key: 'email.enabled', label: 'Enable Email Delivery', control: 'checkbox' },
      { key: 'email.provider', label: 'Provider', control: 'select', options: ['SMTP', 'SendGrid', 'AmazonSES'] },
      { key: 'email.from_name', label: 'From Name', placeholder: 'Vunoca' },
      { key: 'email.from_address', label: 'From Email', placeholder: 'no-reply@vunoca.com' },
      { key: 'email.smtp_host', label: 'SMTP Host', placeholder: 'smtp.example.com' },
      { key: 'email.smtp_port', label: 'SMTP Port', placeholder: '587' },
      { key: 'email.smtp_username', label: 'SMTP Username', placeholder: 'SMTP username' },
      { key: 'email.smtp_password', label: 'SMTP Password / API Key', type: 'password', placeholder: 'Leave masked value unchanged' },
      { key: 'email.smtp_use_ssl', label: 'Use SSL / TLS', control: 'checkbox' },
    ],
  },
  {
    title: 'SMS Settings',
    description: 'Configure SMS delivery for verification codes and customer notifications.',
    fields: [
      { key: 'sms.enabled', label: 'Enable SMS Delivery', control: 'checkbox' },
      { key: 'sms.provider', label: 'Provider', control: 'select', options: ['Twilio', 'Vonage', 'Manual'] },
      { key: 'sms.account_sid', label: 'Account SID / API Key', placeholder: 'Provider account identifier' },
      { key: 'sms.auth_token', label: 'Auth Token / API Secret', type: 'password', placeholder: 'Leave masked value unchanged' },
      { key: 'sms.from_number', label: 'From Phone Number', placeholder: '+14085550100' },
      { key: 'otp.length', label: 'OTP Length', control: 'select', options: ['4', '5', '6', '7', '8'] },
      { key: 'otp.expires_minutes', label: 'OTP Expiration (Minutes)', placeholder: '5' },
      { key: 'otp.max_attempts', label: 'Maximum Verification Attempts', placeholder: '5' },
      { key: 'otp.resend_seconds', label: 'Resend Cooldown (Seconds)', placeholder: '60' },
    ],
  },
  {
    title: 'Email Templates',
    description: 'Reusable email subjects and bodies. Supported placeholders include {{siteName}}, {{userName}}, {{verificationUrl}}, {{listingTitle}}, {{orderNumber}} and {{amount}}.',
    fields: [
      { key: 'template.email_welcome_subject', label: 'Welcome Subject', placeholder: 'Welcome to {{siteName}}' },
      { key: 'template.email_welcome_body', label: 'Welcome Email Body', control: 'textarea', span2: true, placeholder: 'Hello {{userName}}, welcome to {{siteName}}.' },
      { key: 'template.email_verify_subject', label: 'Email Verification Subject', placeholder: 'Verify your {{siteName}} account' },
      { key: 'template.email_verify_body', label: 'Email Verification Body', control: 'textarea', span2: true, placeholder: 'Verify your email: {{verificationUrl}}' },
      { key: 'template.email_password_reset_subject', label: 'Password Reset Subject', placeholder: 'Reset your {{siteName}} password' },
      { key: 'template.email_password_reset_body', label: 'Password Reset Body', control: 'textarea', span2: true, placeholder: 'Reset your password: {{resetUrl}}' },
      { key: 'template.email_payment_subject', label: 'Payment Confirmation Subject', placeholder: 'Payment received - {{orderNumber}}' },
      { key: 'template.email_payment_body', label: 'Payment Confirmation Body', control: 'textarea', span2: true, placeholder: 'We received {{amount}} for order {{orderNumber}}.' },
    ],
  },
  {
    title: 'SMS Templates',
    description: 'Reusable short-message templates for verification and payment notifications.',
    fields: [
      { key: 'template.sms_verification', label: 'Verification SMS', control: 'textarea', span2: true, placeholder: '{{siteName}} verification code: {{code}}. Expires in {{expiresMinutes}} minutes.' },
      { key: 'template.sms_payment', label: 'Payment Confirmation SMS', control: 'textarea', span2: true, placeholder: 'Payment {{amount}} received for {{orderNumber}}.' },
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

      {fieldGroups.map((group) => {
        const providerGroups = group.title === 'Authentication Providers'
          ? [
              { title: 'Email / Password', description: 'Built-in account login.', keys: ['auth.email_enabled'] },
              { title: 'Google', description: 'Google OAuth application credentials.', keys: ['auth.google_enabled', 'auth.google_client_id', 'auth.google_client_secret'] },
              { title: 'Facebook', description: 'Facebook Login application credentials.', keys: ['auth.facebook_enabled', 'auth.facebook_app_id', 'auth.facebook_app_secret'] },
              { title: 'Account Creation', description: 'Shared behavior for social sign-in providers.', keys: ['auth.auto_create_user'] },
            ]
          : group.title === 'Payment Providers'
            ? [
                { title: 'General Payment Settings', description: 'Default checkout provider and currency.', keys: ['payment.default_provider', 'payment.currency'] },
                { title: 'Stripe', description: 'Stripe checkout and webhook credentials.', keys: ['payment.stripe_enabled', 'payment.stripe_publishable_key', 'payment.stripe_secret_key', 'payment.stripe_webhook_secret'] },
                { title: 'PayPal', description: 'PayPal checkout credentials and environment.', keys: ['payment.paypal_enabled', 'payment.paypal_client_id', 'payment.paypal_secret', 'payment.paypal_mode'] },
                { title: 'Manual Payment', description: 'Offline payment instructions shown to customers.', keys: ['payment.manual_enabled', 'payment.manual_instructions'] },
              ]
            : null;

        const renderField = (field: SettingField) => (
          <div className={`${field.upload ? 'site-setting-upload ' : ''}${field.span2 ? 'span2' : ''}`.trim()} key={field.key}>
            {field.control === 'checkbox' ? (
              <AdminCheckbox
                className="site-setting-checkbox"
                label={field.label}
                checked={(values[field.key] || '').toLowerCase() === 'true'}
                onChange={(e) => setValue(field.key, String(e.target.checked))}
              />
            ) : field.control === 'textarea' ? (
              <AdminTextArea
                label={field.label}
                value={values[field.key] || ''}
                placeholder={field.placeholder}
                rows={5}
                onChange={(e) => setValue(field.key, e.target.value)}
              />
            ) : field.control === 'select' ? (
              <AdminSelect
                label={field.label}
                value={values[field.key] || field.options?.[0] || ''}
                options={field.options}
                onChange={(e) => setValue(field.key, e.target.value)}
              />
            ) : (
              <AdminTextBox
                label={field.label}
                type={field.type || 'text'}
                value={values[field.key] || ''}
                placeholder={field.placeholder}
                onChange={(e) => setValue(field.key, e.target.value)}
              />
            )}
            {field.upload && (
              <div className="upload-line">
                <input type="file" accept="image/*" onChange={(e) => upload(field.key, e.target.files?.[0])} />
                {values[field.key] && <img src={resolveUrl(values[field.key])} alt={`${field.label} preview`} />}
              </div>
            )}
          </div>
        );

        return (
          <section className="admin-card site-settings-card" key={group.title}>
            <div className="section-header-row site-settings-section-header">
              <div>
                <h2>{group.title}</h2>
                <p>{group.description}</p>
              </div>
              <AdminToolbar><AdminButton onClick={settingsApi.load}>Refresh</AdminButton></AdminToolbar>
            </div>

            {providerGroups ? (
              <div className="settings-provider-grid">
                {providerGroups.map((provider) => (
                  <article className="settings-provider-card" key={provider.title}>
                    <div className="settings-provider-heading">
                      <div>
                        <h3>{provider.title}</h3>
                        <p>{provider.description}</p>
                        {provider.title === 'Google' && (
                          <p className="oauth-callback-hint"><strong>Authorized redirect URI:</strong><br/><code>{`${appConfig.apiBaseUrl.replace(/\/api\/v1\/?$/i, '')}/api/v1/auth/external/google/callback`}</code></p>
                        )}
                        {provider.title === 'Facebook' && (
                          <p className="oauth-callback-hint"><strong>Valid OAuth Redirect URI:</strong><br/><code>{`${appConfig.apiBaseUrl.replace(/\/api\/v1\/?$/i, '')}/api/v1/auth/external/facebook/callback`}</code></p>
                        )}
                      </div>
                    </div>
                    <div className="ad-form-grid site-settings-grid settings-provider-fields">
                      {group.fields.filter((field) => provider.keys.includes(field.key)).map(renderField)}
                    </div>
                  </article>
                ))}
              </div>
            ) : (
              <div className="settings-group-box">
                <div className="ad-form-grid site-settings-grid">
                  {group.fields.map(renderField)}
                </div>
              </div>
            )}
          </section>
        );
      })}
    </>
  );
}
