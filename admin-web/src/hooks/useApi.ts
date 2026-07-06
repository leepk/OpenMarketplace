import { useEffect, useState } from 'react';
import { apiClient } from '../lib/api/apiClient';

export function useApi<T>(path: string | string[], fallback: T) {
  const [data, setData] = useState<T>(fallback);
  const [err, setErr] = useState('');
  const [loading, setLoading] = useState(false);
  const paths = Array.isArray(path) ? path : [path];
  const pathKey = paths.join('|');

  const load = async () => {
    setLoading(true);
    setErr('');
    const errors: string[] = [];
    for (const endpoint of paths) {
      try {
        const result = await apiClient.get<T>(endpoint);
        setData(result);
        setLoading(false);
        return result;
      } catch (e) {
        errors.push(`${endpoint}: ${e instanceof Error ? e.message : 'Unable to load data'}`);
      }
    }
    setErr(errors[errors.length - 1] || 'Unable to load data');
    setLoading(false);
    return fallback;
  };

  useEffect(() => {
    load();
  }, [pathKey]);

  return { data, err, loading, load, setData };
}
