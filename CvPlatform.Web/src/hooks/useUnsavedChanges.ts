import { useEffect } from 'react';
import { useBlocker } from 'react-router-dom';
import { useText } from './useText';
export function useUnsavedChanges(dirty: boolean) {
  const text = useText();
  const blocker = useBlocker(dirty);
  useEffect(() => {
    if (blocker.state === 'blocked') {
      if (confirm(text('Leave and discard unsaved changes?', 'Покинуть страницу и потерять изменения?'))) blocker.proceed();
      else blocker.reset();
    }
  }, [blocker, text]);
  useEffect(() => {
    const warn = (event: BeforeUnloadEvent) => { if (dirty) { event.preventDefault(); event.returnValue = ''; } };
    window.addEventListener('beforeunload', warn);
    return () => window.removeEventListener('beforeunload', warn);
  }, [dirty]);
}
