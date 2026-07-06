import { ErrorState, LoadingState, PageHero, PanelHeader } from '../components/common/AdminCommon';
import { useApi } from '../hooks/useApi';

export function HealthPage() {
  const { data, err, loading } = useApi<any>('/admin/health', {});
  return <><PageHero eyebrow="SYSTEM" title="System Health" description="API health and diagnostics." /><section className="panel"><PanelHeader title="System Health" />{loading && <LoadingState />}<pre>{JSON.stringify(data, null, 2)}</pre>{err && <ErrorState message={err} />}</section></>;
}
