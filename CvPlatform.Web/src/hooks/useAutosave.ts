import { useCallback, useEffect, useRef, useState } from 'react';
import { ApiError } from '../api/client';

export type AutosaveStatus = 'idle' | 'pending' | 'saved' | 'conflict' | 'error';

export function useAutosave<T>(value: T, save: (value: T) => Promise<void>, intervalMs = 7000) {
  const [status, setStatus] = useState<AutosaveStatus>('idle');
  const latest = useRef(value);
  const persisted = useRef(value);
  const isDirty = useRef(false);

  useEffect(() => {
    latest.current = value;
    if (JSON.stringify(value) !== JSON.stringify(persisted.current)) {
      isDirty.current = true;
      setStatus('pending');
    }
  }, [value]);

  const flush = useCallback(async () => {
    if (!isDirty.current) {
      return;
    }

    try {
      await save(latest.current);
      persisted.current = latest.current;
      isDirty.current = false;
      setStatus('saved');
    } catch (error) {
      if (error instanceof ApiError && error.status === 409) {
        setStatus('conflict');
        return;
      }
      setStatus('error');
    }
  }, [save]);

  useEffect(() => {
    const handle = window.setInterval(() => {
      void flush();
    }, intervalMs);
    return () => window.clearInterval(handle);
  }, [flush, intervalMs]);

  const markPersisted = useCallback((next: T) => {
    persisted.current = next;
    latest.current = next;
    isDirty.current = false;
    setStatus('idle');
  }, []);

  return { status, flush, markPersisted };
}