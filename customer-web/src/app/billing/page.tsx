'use client';

import { useEffect, useMemo, useState } from 'react';
import { AccountShell } from '@/components/account/AccountShell';
import { AuthEmpty, useSessionUser } from '@/components/account/RequireAuth';
import { apiClient, marketplaceApi } from '@/lib/api/apiClient';
import { appConfig } from '@/lib/config';
import { Icon } from '@/components/ui/Icon';
import { useI18n } from '@/lib/i18n/client';

type BillingData = { orders: any[]; payments: any[]; invoices: any[] };

export default function BillingPage() {
  const { t, packageLabel } = useI18n();
  const { user, ready } = useSessionUser();
  const [data, setData] = useState<BillingData>({ orders: [], payments: [], invoices: [] });
  const [packages, setPackages] = useState<any[]>([]);
  const [paymentSettings, setPaymentSettings] = useState<any>(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    if (!user?.id) { setLoading(false); return; }
    let off = false;
    Promise.all([
      marketplaceApi.billing(user.id),
      apiClient.get<any[]>('/packages').catch(() => []),
      apiClient.get<any>('/payment/settings').catch(() => null),
    ]).then(([billing, packs, pay]) => {
      if (!off) { setData(billing ?? { orders: [], payments: [], invoices: [] }); setPackages(packs ?? []); setPaymentSettings(pay); }
    }).catch(() => !off && setData({ orders: [], payments: [], invoices: [] }))
      .finally(() => !off && setLoading(false));
    return () => { off = true; };
  }, [user?.id]);

  const totals = useMemo(() => {
    const paid = (data.payments ?? []).filter((p:any) => String(p.status).toLowerCase() === 'paid').reduce((s:number,p:any)=>s+Number(p.amount ?? 0),0);
    const invoices = (data.invoices ?? []).reduce((s:number,i:any)=>s+Number(i.amount ?? i.total ?? 0),0);
    return { paid, invoices, orders: data.orders?.length ?? 0 };
  }, [data]);

  if (!ready) return null;
  if (!user) return <AuthEmpty title={t('paymentsInvoices')} text={t('loginToViewBilling')} />;

  const apiRoot = appConfig.apiBaseUrl.replace(/\/api\/v\d+\/?$/, '').replace(/\/$/, '');

  return <AccountShell user={user} title={t('paymentsInvoices')} subtitle={t('billingSubtitle')} action={<a className="primary-button" href="/post"><Icon name="plus" size={16}/> {t('upgradeListing')}</a>}>
    <div className="billing-dashboard-v4">
      <section className="billing-summary-v4">
        <div><Icon name="card" size={22}/><small>{t('totalPaid')}</small><b>${totals.paid.toLocaleString()}</b></div>
        <div><Icon name="card" size={22}/><small>{t('invoices')}</small><b>{data.invoices?.length ?? 0}</b></div>
        <div><Icon name="list" size={22}/><small>{t('orders')}</small><b>{totals.orders}</b></div>
        <div><Icon name="shield" size={22}/><small>{t('walletCredits')}</small><b>$0</b></div>
      </section>

      <section className="billing-packages-v4">
        <div className="billing-section-title-v4"><div><span>{t('packages')}</span><h2>{t('boostListings')}</h2></div><a href="/post">{t('postListing')}</a></div>
        <div className="billing-package-grid-v4">
          {packages.slice(0, 4).map((p:any) => <article key={p.id ?? p.name} className="billing-package-v4">
            <div><strong>{packageLabel(p.code ?? p.name)}</strong><small>{p.durationDays ? `${p.durationDays} days` : t('marketplacePackage')}</small></div>
            <b>${Number(p.price ?? 0).toLocaleString()}</b>
            <p>{p.description ?? t('promoteYourListing')}</p>
          </article>)}
          {!packages.length && !loading ? <div className="empty-state-v3"><Icon name="card" size={34}/><strong>{t('noPackagesYet')}</strong><p>{t('packagesSeedText')}</p></div> : null}
        </div>
      </section>

      {paymentSettings && <section className="billing-history-v4"><div className="billing-section-title-v4"><div><span>{t('checkout')}</span><h2>{t('availablePaymentMethods')}</h2></div></div><div className="billing-payment-methods-v4">{paymentSettings.stripe?.enabled && <span>Stripe</span>}{paymentSettings.paypal?.enabled && <span>PayPal ({paymentSettings.paypal.mode})</span>}{paymentSettings.manual?.enabled && <span>{t('manualPayment')}</span>}<small>{t('defaultLabel')}: {paymentSettings.defaultProvider} · {t('currency')}: {paymentSettings.currency}</small></div></section>}
      <section className="billing-history-v4">
        <div className="billing-section-title-v4"><div><span>{t('history')}</span><h2>{t('paymentActivity')}</h2></div></div>
        <div className="billing-table-v4">
          <div className="billing-table-head-v4"><span>{t('item')}</span><span>{t('type')}</span><span>{t('amount')}</span><span>{t('status')}</span><span>{t('action')}</span></div>
          {loading ? <div className="billing-empty-row-v4">{t('loadingBillingActivity')}</div> : null}
          {!loading && !(data.orders?.length || data.payments?.length || data.invoices?.length) ? <div className="billing-empty-row-v4">{t('noBillingActivity')}</div> : null}
          {(data.orders ?? []).map((o:any) => <div className="billing-row-v4" key={`order-${o.id}`}><span><b>{o.orderNumber ?? o.id}</b><small>{o.createdAt ? new Date(o.createdAt).toLocaleDateString() : t('order')}</small></span><span>{t('order')}</span><span>${Number(o.total ?? o.amount ?? 0).toLocaleString()}</span><span><em className="paid">{o.status ?? t('paid')}</em></span><span><a href="#">{t('details')}</a></span></div>)}
          {(data.payments ?? []).map((p:any) => <div className="billing-row-v4" key={`payment-${p.id}`}><span><b>{p.description ?? p.providerReference ?? t('payment')}</b><small>{p.createdAt ? new Date(p.createdAt).toLocaleDateString() : t('payment')}</small></span><span>{t('payment')}</span><span>${Number(p.amount ?? 0).toLocaleString()}</span><span><em className={String(p.status).toLowerCase()==='paid'?'paid':'pending'}>{p.status ?? t('pending')}</em></span><span><a href="#">{t('receipt')}</a></span></div>)}
          {(data.invoices ?? []).map((i:any) => <div className="billing-row-v4" key={`invoice-${i.id}`}><span><b>{i.invoiceNumber ?? t('invoice')}</b><small>{i.createdAt ? new Date(i.createdAt).toLocaleDateString() : t('invoice')}</small></span><span>{t('invoice')}</span><span>${Number(i.amount ?? i.total ?? 0).toLocaleString()}</span><span><em className={String(i.status).toLowerCase()==='paid'?'paid':'pending'}>{i.status ?? t('open')}</em></span><span>{i.pdfUrl ? <a href={`${apiRoot}${i.pdfUrl}`}>{t('viewPdf')}</a> : <a href="#">{t('view')}</a>}</span></div>)}
        </div>
      </section>
    </div>
  </AccountShell>;
}
