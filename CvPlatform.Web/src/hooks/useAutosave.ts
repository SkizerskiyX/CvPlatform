import { useCallback, useEffect, useRef, useState } from 'react';
import { ApiError } from '../api/client';
export type AutosaveStatus = 'idle' | 'pending' | 'saved' | 'conflict' | 'error';
export function useAutosave<T>(value: T, save: (value: T) => Promise<void>, intervalMs = 7000) {
  const [status, setStatus] = useState<AutosaveStatus>('idle');
  const [dirty, setDirty] = useState(false);
  const latest = useRef(value); const persisted = useRef(value); const busy = useRef(false); const blocked = useRef(false);
  const saveRef = useRef(save); saveRef.current = save; latest.current = value;
  useEffect(() => {
    const changed = JSON.stringify(value) !== JSON.stringify(persisted.current);
    setDirty(changed);
    if (changed && !blocked.current) setStatus('pending');
  }, [value]);
  const flush = useCallback(async () => {
    if (busy.current || blocked.current || JSON.stringify(latest.current) === JSON.stringify(persisted.current)) return;
    const snapshot = latest.current; busy.current = true;
    try {
      await saveRef.current(snapshot); persisted.current = snapshot;
      const pending = JSON.stringify(latest.current) !== JSON.stringify(snapshot);
      setDirty(pending); setStatus(pending ? 'pending' : 'saved');
    } catch (error) {
      blocked.current = error instanceof ApiError && error.status === 409;
      setStatus(blocked.current ? 'conflict' : 'error');
    } finally { busy.current = false; }
  }, []);
  useEffect(() => { const timer = setInterval(() => void flush(), intervalMs); return () => clearInterval(timer); }, [flush, intervalMs]);
  const markPersisted = useCallback((next: T) => {
    persisted.current = next; latest.current = next; blocked.current = false; setDirty(false); setStatus('idle');
  }, []);
  return { status, dirty, flush, markPersisted, busy };
}
