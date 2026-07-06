import { PageHero, PanelHeader, StatCard, normalizePagedRows } from '../components/common/AdminCommon';
import { useApi } from '../hooks/useApi';

function value(data: any, ...keys: string[]) { for (const k of keys) if (data?.[k] != null) return data[k]; return 0; }

export function ReportsPage() {
  const { data } = useApi<any>(['/admin/reports/overview', '/admin/reports', '/admin/dashboard'], { stats: {}, trends: [] });
  const stats = data.stats ?? data;
  const trends = normalizePagedRows(data.trends ?? data.listingsOverview ?? data.dailyListings ?? []).rows;
  const max = Math.max(1, ...trends.map((x: any) => Number(x.count ?? x.total ?? x.listings ?? 0)));
  const bars = trends.length ? trends.slice(-14) : [
    { label: 'Listings', count: value(stats, 'totalListings') },
    { label: 'Pending', count: value(stats, 'pendingListings') },
    { label: 'Users', count: value(stats, 'users', 'totalUsers') },
    { label: 'Payments', count: value(stats, 'payments', 'totalOrders') },
  ];
  return <><PageHero eyebrow="ANALYTICS" title="Reports & Analytics" description="Review marketplace revenue, growth, and moderation trends." /><div className="metric-grid four"><StatCard label="Total Revenue" value={`$${value(stats, 'revenue', 'totalRevenue')}`} tone="success" /><StatCard label="Total Orders" value={value(stats, 'orders', 'totalOrders', 'payments')} /><StatCard label="New Users" value={value(stats, 'newUsers', 'users', 'totalUsers')} /><StatCard label="Listings" value={value(stats, 'totalListings', 'listings')} /></div><section className="panel wide"><PanelHeader title="Reports & Analytics" /><div className="bar-chart">{bars.map((b: any, i: number) => { const count = Number(b.count ?? b.total ?? b.listings ?? 0); return <div key={i} className="bar-item"><span>{b.label || b.date || b.name || `#${i+1}`}</span><i style={{ height: `${Math.max(8, count / max * 100)}%` }} /><b>{count}</b></div>; })}</div></section></>;
}
