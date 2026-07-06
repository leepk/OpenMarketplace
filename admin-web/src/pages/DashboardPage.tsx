import { AdminTable, ErrorState, LoadingState, PageHero, PanelHeader, StatCard } from '../components/common/AdminCommon';
import { AdminSelect } from '../components/common/AdminControls';
import { useApi } from '../hooks/useApi';
import { normalizeListings } from '../utils/format';

function chartRows(data: any) {
  const rows = data.listingsOverview || data.listingOverview || data.dailyListings || data.trends || [];
  if (Array.isArray(rows) && rows.length) return rows.slice(-12);
  const s = data.stats ?? {};
  return [
    { label: 'Total', count: s.totalListings ?? 0 },
    { label: 'Pending', count: s.pendingListings ?? 0 },
    { label: 'Users', count: s.users ?? 0 },
    { label: 'Messages', count: s.messages ?? 0 },
  ];
}

export function DashboardPage() {
  const { data, loading, err } = useApi<any>('/admin/dashboard', { stats: {}, recentListings: [] });
  const s = data.stats ?? {};
  return (
    <>
      <PageHero eyebrow="ADMIN DASHBOARD" title="Dashboard" description="Welcome back. Here is what is happening across OpenMarketplace today." />
      <div className="metric-grid">
        <StatCard label="Total Listings" value={s.totalListings ?? 0} helper="all time" />
        <StatCard label="Pending Review" value={s.pendingListings ?? 0} tone="warning" helper="needs action" />
        <StatCard label="Users" value={s.users ?? 0} helper="registered" />
        <StatCard label="Messages" value={s.messages ?? 0} helper="conversations" />
        <StatCard label="Revenue" value={`$${s.revenue ?? 0}`} tone="success" helper="succeeded payments" />
      </div>
      {loading && <LoadingState />}
      {err && <ErrorState message={err} />}
      <div className="dashboard-grid">
        <section className="panel wide">
          <PanelHeader title="Listings Overview" action={<AdminSelect defaultValue="Daily" options={['Daily', 'Weekly']} />} />
          <div className="bar-chart dashboard-bars">{chartRows(data).map((x: any, i: number) => { const max = Math.max(1, ...chartRows(data).map((r: any) => Number(r.count ?? r.total ?? r.listings ?? 0))); const count = Number(x.count ?? x.total ?? x.listings ?? 0); return <div key={i} className="bar-item"><span>{x.label || x.date || x.name || `#${i + 1}`}</span><i style={{ height: `${Math.max(8, count / max * 100)}%` }} /><b>{count}</b></div>; })}</div>
        </section>
        <section className="panel">
          <PanelHeader title="Listings by Status" />
          <div className="donut"><span>{s.totalListings ?? 0}<small>Total</small></span></div>
          <div className="legend"><span><b className="blue" />Active</span><span><b className="orange" />Pending</span><span><b className="purple" />Draft</span><span><b className="red" />Rejected</span></div>
        </section>
        <AdminTable title="Recent Listings" rows={normalizeListings(data.recentListings ?? [])} />
        <section className="panel">
          <PanelHeader title="Top Categories" />
          {(data.topCategories ?? []).length ? (data.topCategories ?? []).map((x: any) => <div className="line" key={x.id ?? x.name}><span>{x.name}</span><b>{x.count ?? x.listings ?? 0}</b></div>) : <div className="empty">No category data</div>}
        </section>
      </div>
    </>
  );
}
