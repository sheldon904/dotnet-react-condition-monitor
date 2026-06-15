import { useEffect, useRef, useState } from 'react';

// Poll an async fetcher on a fixed interval. Returns the latest data and any error. The
// interval is rebuilt when deps change (for example when the selected asset changes). This is
// how the dashboard stays live: we poll every couple of seconds rather than use SignalR.
export function usePolling<T>(
  fetcher: () => Promise<T>,
  intervalMs: number,
  deps: unknown[] = [],
): { data: T | null; error: string | null } {
  const [data, setData] = useState<T | null>(null);
  const [error, setError] = useState<string | null>(null);

  // Keep the latest fetcher in a ref so the effect does not need it in its dependency list.
  const fetcherRef = useRef(fetcher);
  fetcherRef.current = fetcher;

  useEffect(() => {
    let cancelled = false;

    const run = async () => {
      try {
        const result = await fetcherRef.current();
        if (!cancelled) {
          setData(result);
          setError(null);
        }
      } catch (e) {
        if (!cancelled) setError(e instanceof Error ? e.message : String(e));
      }
    };

    run(); // fetch immediately so the UI does not wait a full interval
    const handle = setInterval(run, intervalMs);
    return () => {
      cancelled = true;
      clearInterval(handle);
    };
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, deps);

  return { data, error };
}
