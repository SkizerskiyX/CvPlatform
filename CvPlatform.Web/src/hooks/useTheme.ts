import { useCallback, useEffect, useState } from 'react';

const themeStorageKey = 'cvplatform.theme';
export type ThemeName = 'light' | 'dark';

const initialTheme = (): ThemeName => {
  const saved = localStorage.getItem(themeStorageKey);
  if (saved === 'light' || saved === 'dark') return saved;
  return window.matchMedia('(prefers-color-scheme: dark)').matches ? 'dark' : 'light';
};

export function useTheme() {
  const [theme, setTheme] = useState<ThemeName>(initialTheme);
  useEffect(() => { document.documentElement.setAttribute('data-bs-theme', theme); localStorage.setItem(themeStorageKey, theme); }, [theme]);
  const toggle = useCallback(() => setTheme((current) => current === 'dark' ? 'light' : 'dark'), []);
  return { theme, toggle };
}
