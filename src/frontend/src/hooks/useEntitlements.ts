import { useCallback, useEffect, useRef, useState } from 'react';
import { normalizeError } from '../utils/api';

export interface EntitlementDto {
  planSlug: string;
  planName: string;
  captionGenerationsPerMonth: number;
  mediaAssetsLimit: number;
  seatsIncluded: number;
  schedulingEnabled: boolean;
  aiImproveEnabled: boolean;
  captionsUsedThisPeriod: number;
  mediaUsedThisPeriod: number;
  periodStartUtc: string;
  activeUntilUtc?: string | null;
}

interface State {
  data: EntitlementDto | null;
  loading: boolean;
  error: string;
}

export function useEntitlements(pollMs = 30000) {
  const [state, setState] = useState<State>({ data: null, loading: true, error: '' });
  const abortRef = useRef<AbortController | null>(null);

  const load = useCallback(async () => {
    abortRef.current?.abort();
    const controller = new AbortController();
    abortRef.current = controller;
    setState(prev => ({ ...prev, loading: true, error: '' }));
    try {
      const res = await fetch('/api/entitlements', { credentials: 'include', signal: controller.signal });
      if (!res.ok) throw new Error(await res.text());
      const data: EntitlementDto = await res.json();
      setState({ data, loading: false, error: '' });
    } catch (err) {
      if (controller.signal.aborted) return;
      setState(prev => ({ ...prev, loading: false, error: normalizeError(err, 'Unable to load plan') }));
    }
  }, []);

  useEffect(() => {
    load();
    if (!pollMs || pollMs < 5000) return;
    const id = globalThis.setInterval(load, pollMs);
    return () => {
      globalThis.clearInterval(id);
      abortRef.current?.abort();
    };
  }, [load, pollMs]);

  return { ...state, refresh: load };
}
