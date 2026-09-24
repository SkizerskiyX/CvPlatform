import { useEffect, useState } from 'react';
import { Link, NavLink, useLocation, useNavigate } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { useAuth } from '../hooks/useAuth';
import { useTheme } from '../hooks/useTheme';
import { languageStorageKey } from '../i18n';
import { Icon } from './Icon';

export function AppLayout({ children }: { children: React.ReactNode }) {
  const { t, i18n } = useTranslation();
  const { isAuthenticated, isStaff, profile, signOut } = useAuth();
  const { theme, toggle } = useTheme();
  const { pathname } = useLocation();
  const navigate = useNavigate();
  const [search, setSearch] = useState('');
  const [menuOpen, setMenuOpen] = useState(false);
  const ru = i18n.language.startsWith('ru');
  const authScreen = ['/login', '/register', '/oauth-callback'].includes(pathname);
  useEffect(() => { setMenuOpen(false); }, [pathname]);
  const items = [
    { to: '/', label: ru ? 'Обзор' : 'Overview', icon: 'overview' as const, visible: true },
    { to: '/positions', label: t('nav.positions'), icon: 'positions' as const, visible: true },
    { to: '/cvs', label: t('nav.cvs'), icon: 'cv' as const, visible: isAuthenticated },
    { to: '/profile', label: t('nav.profile'), icon: 'profile' as const, visible: isAuthenticated },
    { to: '/attributes', label: t('nav.attributes'), icon: 'library' as const, visible: isStaff },
  ];
  const title = items.find(x => x.to === '/' ? pathname === '/' : pathname.startsWith(x.to))?.label ?? (pathname === '/search' ? t('common.search') : t('app.title'));
  const subtitles: Record<string, string> = {
    '/positions': ru ? 'Найдите позицию, которая соответствует вашему опыту.' : 'Find a position that fits your experience.',
    '/cvs': ru ? 'Ваш опыт, представленный для каждой позиции.' : 'Your experience, tailored to each opportunity.',
    '/profile': ru ? 'Один профиль. Основа для каждого вашего резюме.' : 'One professional profile. The foundation of every CV.',
    '/attributes': ru ? 'Общие поля для профилей и шаблонов резюме.' : 'Shared fields for professional profiles and CV templates.',
  };
  return <div className={'app-shell' + (menuOpen ? ' menu-open' : '')}>
    <a className="skip-link" href="#main-content">{ru ? 'К содержимому' : 'Skip to content'}</a>
    {menuOpen && <button className="nav-scrim" aria-label={ru ? 'Закрыть меню' : 'Close menu'} onClick={() => setMenuOpen(false)} />}
    <aside className="app-sidebar">
      <Link to="/" className="sidebar-brand"><span className="brand-mark">cv</span><span>CV Platform<small>{ru ? 'Пространство карьеры' : 'Your career workspace'}</small></span></Link>
      <nav className="sidebar-nav" aria-label={ru ? 'Основная навигация' : 'Main navigation'}>
        {items.filter(x => x.visible).map(item => <NavLink key={item.to} to={item.to} end={item.to === '/'} className={({ isActive }) => 'sidebar-link' + (isActive ? ' active' : '')}><Icon name={item.icon} /><span>{item.label}</span></NavLink>)}
      </nav>
      <div className="sidebar-note"><Icon name="cv" /><p>{ru ? 'Ваш следующий шаг начинается с вашего опыта.' : 'Your next chapter starts with your experience.'}</p><Link to={isAuthenticated ? '/profile' : '/register'}>{ru ? 'Собрать профиль' : 'Build your profile'}</Link></div>
      <div className="sidebar-footer"><span className="avatar">{profile?.displayName?.[0]?.toUpperCase() ?? 'G'}</span><div><strong>{profile?.displayName ?? (ru ? 'Гостевой доступ' : 'Guest workspace')}</strong><small>{isStaff ? (ru ? 'Подбор кандидатов' : 'Recruitment workspace') : (ru ? 'Профессиональный профиль' : 'Professional profile')}</small></div></div>
    </aside>
    <div className="app-stage">
      <header className="context-bar">
        <button className="icon-button mobile-menu" aria-label={ru ? 'Открыть меню' : 'Open menu'} aria-expanded={menuOpen} onClick={() => setMenuOpen(!menuOpen)}><Icon name="menu" /></button>
        <form className="global-search" role="search" onSubmit={event => { event.preventDefault(); if (search.trim()) navigate('/search?q=' + encodeURIComponent(search.trim())); }}>
          <Icon name="search" /><input aria-label={t('common.search')} placeholder={ru ? 'Поиск позиций и резюме' : 'Search positions and CVs'} value={search} onChange={event => setSearch(event.target.value)} /><button type="submit">{t('common.search')}</button>
        </form>
        <div className="context-actions">
          <select className="language-select" value={ru ? 'ru' : 'en'} aria-label={ru ? 'Язык' : 'Language'} onChange={event => { localStorage.setItem(languageStorageKey, event.target.value); void i18n.changeLanguage(event.target.value); }}><option value="en">EN</option><option value="ru">RU</option></select>
          <button className="icon-button" onClick={toggle} aria-label={ru ? 'Переключить тему' : 'Switch theme'}><Icon name={theme === 'dark' ? 'sun' : 'moon'} /></button>
          {isAuthenticated ? <button className="icon-button" title={t('nav.logout')} aria-label={t('nav.logout')} onClick={() => void signOut()}><Icon name="logout" /></button> : <Link className="header-signin" to="/login">{t('nav.login')}</Link>}
        </div>
      </header>
      <main id="main-content" className={'app-content' + (authScreen ? ' auth-content' : '')}>
        {!authScreen && pathname !== '/' && <div className="workspace-heading"><h1>{title}</h1>{subtitles[pathname] && <p>{subtitles[pathname]}</p>}</div>}
        {authScreen ? <div className="auth-composition"><section className="auth-introduction"><span className="document-emblem"><Icon name="cv" /></span><h1>{ru ? 'Весь ваш опыт. В правильном свете.' : 'Your experience. In the right light.'}</h1><p>{ru ? 'Соберите профиль один раз. Создавайте резюме под подходящие позиции.' : 'Build your profile once. Create a CV for the positions that fit you.'}</p><Link to="/positions">{ru ? 'Посмотреть позиции' : 'Explore open positions'}</Link></section><div>{children}<p className="auth-switch">{pathname === '/register' ? <Link to="/login">{ru ? 'Уже есть аккаунт? Войти' : 'Already have an account? Sign in'}</Link> : <Link to="/register">{ru ? 'Нет аккаунта? Зарегистрироваться' : 'New here? Create an account'}</Link>}</p></div></div> : children}
      </main>
      <footer className="workspace-footer"><span>CV Platform</span><span>{ru ? 'Ваш опыт имеет значение.' : 'Make your experience count.'}</span></footer>
    </div>
  </div>;
}
