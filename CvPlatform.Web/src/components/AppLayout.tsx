import { useState } from 'react';
import { Button } from 'react-bootstrap';
import { Link, NavLink, useLocation, useNavigate } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { useAuth } from '../hooks/useAuth';
import { useTheme } from '../hooks/useTheme';
import { languageStorageKey } from '../i18n';

type NavItem = { to: string; label: string; mark: string; auth?: boolean; admin?: boolean };

export function AppLayout({ children }: { children: React.ReactNode }) {
  const { t, i18n } = useTranslation();
  const { isAuthenticated, isStaff, roles, signOut } = useAuth();
  const { theme, toggle } = useTheme();
  const [language, setLanguage] = useState(i18n.language);
  const [collapsed, setCollapsed] = useState(false);
  const { pathname } = useLocation();
  const navigate = useNavigate();
  const [search, setSearch] = useState('');
  const isAuthScreen = ['/login', '/register', '/oauth-callback'].includes(pathname);
  const items: NavItem[] = [
    { to: '/positions', label: t('nav.positions'), mark: 'V' },
    { to: '/cvs', label: t('nav.cvs'), mark: 'C', auth: true },
    { to: '/profile', label: t('nav.profile'), mark: 'P', auth: true },
    { to: '/attributes', label: t('nav.attributes'), mark: 'A', admin: true },
  ];
  const changeLanguage = (next: string) => { setLanguage(next); localStorage.setItem(languageStorageKey, next); void i18n.changeLanguage(next); };

  if (isAuthScreen) return <main className="auth-shell">{children}</main>;

  return <div className={`app-shell ${collapsed ? 'sidebar-collapsed' : ''}`}>
    <aside className="app-sidebar">
      <div className="sidebar-brand-row">
        <Link className="sidebar-brand" to="/positions" aria-label={t('app.title')}><span className="brand-mark">CV</span><span className="brand-name">{t('app.title')}</span></Link>
        <button className="sidebar-toggle" type="button" onClick={() => setCollapsed((value) => !value)} aria-label="Toggle navigation"><span /></button>
      </div>
      <nav className="sidebar-nav" aria-label="Main navigation">
        <p className="nav-section-label">Workspace</p>
        {items.filter((item) => (!item.auth || isAuthenticated) && (!item.admin || isStaff)).map((item) => <NavLink key={item.to} to={item.to} className={({ isActive }) => `sidebar-link ${isActive ? 'active' : ''}`}><span className="nav-mark">{item.mark}</span><span className="nav-label">{item.label}</span></NavLink>)}
      </nav>
      <div className="sidebar-footer"><div className="role-chip"><span className="role-dot" /> <span>{roles[0] ?? (isAuthenticated ? 'User' : 'Guest workspace')}</span></div><div className="sidebar-meta">© 2026 CV Platform</div></div>
    </aside>
    <div className="app-stage">
      <header className="context-bar"><div className="context-crumb"><span>Workspace</span><b>/</b><strong>{items.find((item) => pathname.startsWith(item.to))?.label ?? t('app.title')}</strong></div><form className="d-none d-md-flex flex-grow-1 mx-3" onSubmit={(event) => { event.preventDefault(); if (search.trim()) navigate(`/search?q=${encodeURIComponent(search.trim())}`); }}><input className="form-control form-control-sm" aria-label="Full-text search" placeholder={t('common.search')} value={search} onChange={(event) => setSearch(event.target.value)} /></form><div className="context-actions">
        <select className="language-select" value={language} onChange={(event) => changeLanguage(event.target.value)} aria-label="Language"><option value="en">EN</option><option value="ru">RU</option></select>
        <Button className="theme-button" variant="link" onClick={toggle} aria-label="Toggle color theme">{theme === 'dark' ? 'Light' : 'Dark'}</Button>
        {isAuthenticated ? <button className="account-button" type="button" onClick={() => void signOut()}><span className="avatar">{roles[0]?.[0] ?? 'U'}</span><span className="account-label">{t('nav.logout')}</span></button> : <div className="auth-actions"><Link to="/login" className="quiet-link">{t('nav.login')}</Link><Link to="/register" className="btn btn-primary btn-sm">{t('nav.register')}</Link></div>}
      </div></header>
      <main className="app-content">{children}</main>
    </div>
  </div>;
}
