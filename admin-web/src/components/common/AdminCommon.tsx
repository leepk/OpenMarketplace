import type React from 'react';
import { Icon } from './Icon';
import { AdminIconButton, AdminSelect } from './AdminControls';
import { labelize } from '../../utils/format';

export type AdminColumn<T = any> = {
  key: string;
  header: string;
  width?: string;
  render?: (row: T) => React.ReactNode;
};

export type AdminPaging = {
  page: number;
  pageSize: number;
  total: number;
  onPageChange: (page: number) => void;
  onPageSizeChange: (pageSize: number) => void;
  pageSizeOptions?: number[];
};

export function PageHero({ eyebrow, title, description, actions }: { eyebrow?: string; title: string; description?: string; actions?: React.ReactNode }) {
  return (
    <section className="page-hero">
      <div>
        {eyebrow && <span className="page-eyebrow">{eyebrow}</span>}
        <h1>{title}</h1>
        {description && <p>{description}</p>}
      </div>
      {actions && <div className="page-hero-actions">{actions}</div>}
    </section>
  );
}

export function PanelHeader({ title, subtitle, action }: { title: string; subtitle?: string; action?: React.ReactNode }) {
  return (
    <div className="panel-head">
      <div>
        <h2>{title}</h2>
        {subtitle && <p>{subtitle}</p>}
      </div>
      {action && <div className="panel-actions">{action}</div>}
    </div>
  );
}

export function StatCard({ label, value, helper, tone }: { label: string; value: React.ReactNode; helper?: string; tone?: 'success' | 'warning' | 'danger' }) {
  return <div className={`metric ${tone ? `tone-${tone}` : ''}`}><span>{label}</span><strong>{value}</strong>{helper && <small>{helper}</small>}</div>;
}

export function StatusBadge({ value }: { value: unknown }) {
  const text = String(value || 'Unknown');
  const key = text.toLowerCase().replace(/[\s_-]+/g, '');
  const tone = key.includes('inactive') || key.includes('disabled') || key.includes('suspended') || key.includes('reject') || key.includes('ban') || key.includes('hidden') || key.includes('failed') || key.includes('expired')
    ? 'bad'
    : key.includes('pending') || key.includes('draft') || key.includes('review') || key.includes('flag') || key.includes('unread')
      ? 'warn'
      : key.includes('active') || key.includes('approve') || key.includes('published') || key.includes('paid') || key.includes('allow') || key.includes('read')
        ? 'good'
        : '';
  return <em className={`status-badge ${tone}`}>{text}</em>;
}

export function LoadingState() { return <div className="loading-state"><span /> Loading...</div>; }
export function ErrorState({ message }: { message: string }) { return <div className="error-box">{message}</div>; }
export function EmptyState({ text }: { text: string }) { return <div className="empty">{text}</div>; }

function getValue(row: any, key: string) { return key.split('.').reduce((acc, part) => acc?.[part], row); }

export function normalizePagedRows(payload: any) {
  const rows = Array.isArray(payload) ? payload : payload?.items ?? payload?.results ?? payload?.records ?? payload?.data ?? payload?.listings ?? [];
  const total = payload?.total ?? payload?.totalCount ?? payload?.count ?? payload?.pagination?.total ?? (Array.isArray(rows) ? rows.length : 0);
  const page = payload?.page ?? payload?.pageIndex ?? payload?.pagination?.page ?? 1;
  const pageSize = payload?.pageSize ?? payload?.take ?? payload?.pagination?.pageSize ?? 10;
  return { rows: Array.isArray(rows) ? rows : [], total: Number(total || 0), page: Number(page || 1), pageSize: Number(pageSize || 10) };
}

export function AdminDataTable<T extends { id?: string } = any>({
  title,
  subtitle,
  rows,
  columns,
  loading,
  error,
  emptyText = 'No data found',
  actions,
  paging,
  onRowClick,
}: {
  title: string;
  subtitle?: string;
  rows: T[];
  columns: AdminColumn<T>[];
  loading?: boolean;
  error?: string;
  emptyText?: string;
  actions?: React.ReactNode;
  paging?: AdminPaging;
  onRowClick?: (row: T) => void;
}) {
  const pageSizeOptions = paging?.pageSizeOptions ?? [10, 25, 50];
  const page = paging?.page ?? 1;
  const pageSize = paging?.pageSize ?? 10;
  const total = paging?.total ?? rows.length;
  const totalPages = Math.max(1, Math.ceil(total / pageSize));
  const first = total === 0 ? 0 : (page - 1) * pageSize + 1;
  const last = Math.min(page * pageSize, total);

  return (
    <section className="panel table-panel">
      <PanelHeader
        title={title}
        subtitle={subtitle}
        action={<div className="table-meta-actions">{actions}<label className="page-size-control"><span>Rows</span><AdminSelect value={pageSize} onChange={(e) => paging?.onPageSizeChange(Number(e.target.value))} options={pageSizeOptions.map((x) => ({ label: String(x), value: x }))} /></label><strong>{total} records</strong></div>}
      />
      {loading && <LoadingState />}
      {error && <ErrorState message={error} />}
      {!loading && !error && !rows.length && <EmptyState text={emptyText} />}
      {!!rows.length && (
        <>
          <div className="table-wrap common-table-wrap">
            <table className="admin-table">
              <thead><tr>{columns.map((c) => <th key={c.key} style={c.width ? { width: c.width } : undefined}>{c.header}</th>)}</tr></thead>
              <tbody>{rows.map((row, index) => <tr key={row.id ?? index} className={onRowClick ? 'clickable-row' : undefined} onClick={() => onRowClick?.(row)}>{columns.map((c) => <td key={c.key}>{c.render ? c.render(row) : String(getValue(row, c.key) ?? '-')}</td>)}</tr>)}</tbody>
            </table>
          </div>
          <div className="table-pagination">
            <span>Showing {first} to {last} of {total}</span>
            <div>
              <AdminIconButton icon="chevron-left" label="Previous page" disabled={page <= 1} onClick={() => paging?.onPageChange(Math.max(1, page - 1))} />
              <strong>{page}</strong>
              <AdminIconButton icon="chevron-right" label="Next page" disabled={page >= totalPages} onClick={() => paging?.onPageChange(Math.min(totalPages, page + 1))} />
            </div>
          </div>
        </>
      )}
    </section>
  );
}

export function AdminTable({ title, rows }: { title: string; rows: any[] }) {
  const keys = Array.from(new Set(rows.flatMap((r) => Object.keys(r)))).filter((k) => !['id'].includes(k)).slice(0, 6);
  const columns: AdminColumn[] = keys.map((k) => ({ key: k, header: labelize(k), render: (r) => (k.toLowerCase().includes('status') ? <StatusBadge value={r[k]} /> : String(r[k] ?? '').slice(0, 80)) }));
  columns.push({ key: 'actions', header: '', width: '56px', render: () => <AdminIconButton icon="dots" label="More actions" /> });
  return <AdminDataTable title={title} rows={rows} columns={columns} />;
}
