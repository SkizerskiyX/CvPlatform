type IconName = 'overview' | 'positions' | 'cv' | 'profile' | 'library' | 'search' | 'sun' | 'moon' | 'menu' | 'logout';
const paths: Record<IconName, string> = {
  overview: 'M3 3h7v7H3z M14 3h7v7h-7z M3 14h7v7H3z M14 14h7v7h-7z',
  positions: 'M8 7V4h8v3 M3 7h18v14H3z M3 12c5 4 13 4 18 0 M10 13h4',
  cv: 'M5 3h10l4 4v14H5z M14 3v5h5 M8 12h8 M8 16h6',
  profile: 'M16 7a4 4 0 1 1-8 0 4 4 0 0 1 8 0 M4 21v-2a8 8 0 0 1 16 0v2',
  library: 'M4 4h4v16H4z M10 4h4v16h-4z M16 5l3-1 4 15-3 1z',
  search: 'M16 10a6 6 0 1 1-12 0 6 6 0 0 1 12 0 M15 15l6 6',
  sun: 'M16 12a4 4 0 1 1-8 0 4 4 0 0 1 8 0 M12 2v2 M12 20v2 M2 12h2 M20 12h2 M5 5l1 1 M18 18l1 1 M5 19l1-1 M18 6l1-1',
  moon: 'M20 15A9 9 0 0 1 9 4a9 9 0 1 0 11 11',
  menu: 'M4 6h16 M4 12h16 M4 18h16',
  logout: 'M10 4H4v16h6 M10 12h11 M17 8l4 4-4 4',
};
export function Icon({ name }: { name: IconName }) {
  return <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.6" strokeLinecap="round" strokeLinejoin="round" aria-hidden="true"><path d={paths[name]} /></svg>;
}
