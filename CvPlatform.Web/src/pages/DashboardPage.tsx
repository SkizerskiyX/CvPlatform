import { useEffect, useState } from 'react';
import { Alert } from 'react-bootstrap';
import { Link } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { discoveryApi, positionsApi } from '../api/endpoints';
import type { PositionListItem, Stats } from '../api/types';
import { useAuth } from '../hooks/useAuth';

function PositionTable({ rows, popular = false }: { rows: PositionListItem[]; popular?: boolean }) {
  const { i18n } = useTranslation(); const ru = i18n.language.startsWith('ru');
  return <div className="table-responsive"><table className="table overview-table"><thead><tr><th>{ru ? 'Позиция' : 'Position'}</th><th>{popular ? (ru ? 'Резюме' : 'CVs') : (ru ? 'Уровень' : 'Level')}</th><th>{ru ? 'Доступ' : 'Access'}</th></tr></thead><tbody>{rows.map(x => <tr key={x.id}><td><Link to={'/positions/' + x.id}>{x.title}</Link><small>{x.company || (ru ? 'Открытая позиция' : 'Open position')}</small></td><td>{popular ? x.publishedCvCount : x.level ?? '—'}</td><td><span className={'status-pill ' + (x.isPublic ? 'status-open' : '')}>{x.isPublic ? (ru ? 'Открыт' : 'Open') : (ru ? 'По условиям' : 'Restricted')}</span></td></tr>)}{!rows.length && <tr><td colSpan={3} className="empty-state">{ru ? 'Здесь появятся доступные позиции.' : 'Available positions will appear here.'}</td></tr>}</tbody></table></div>;
}

export function DashboardPage() {
  const { i18n } = useTranslation(); const ru = i18n.language.startsWith('ru');
  const { isAuthenticated } = useAuth();
  const [stats, setStats] = useState<Stats | null>(null);
  const [latest, setLatest] = useState<PositionListItem[]>([]);
  const [popular, setPopular] = useState<PositionListItem[]>([]);
  const [tags, setTags] = useState<{ tag: string; count: number }[]>([]);
  const [error, setError] = useState('');
  useEffect(() => { let active = true; void Promise.all([discoveryApi.stats(), positionsApi.latest(), positionsApi.popular(), discoveryApi.tags()]).then(([s, l, p, tagsResult]) => { if (active) { setStats(s); setLatest(l); setPopular(p); setTags(tagsResult); } }).catch((e: Error) => { if (active) setError(e.message); }); return () => { active = false; }; }, []);
  const metrics = [
    [ru ? 'Открытые позиции' : 'Available positions', stats?.totalPositions],
    [ru ? 'Кандидаты' : 'Candidates', stats?.totalCandidates],
    [ru ? 'Рекрутеры' : 'Recruiters', stats?.totalRecruiters],
    [ru ? 'Опубликованные CV' : 'Published CVs', stats?.totalPublishedCvs],
  ];
  return <div className="overview">
    <section className="overview-intro"><div><h1>{ru ? 'Место для следующего шага.' : 'Room for your next chapter.'}</h1><p>{ru ? 'Изучайте позиции. Собирайте свой опыт. Находите подходящие возможности.' : 'Explore positions, shape your profile, and find where your experience belongs.'}</p></div><Link className="btn btn-primary" to={isAuthenticated ? '/profile' : '/register'}>{ru ? 'Собрать профиль' : 'Build your profile'}</Link></section>
    {error && <Alert variant="danger">{error}</Alert>}
    <div className="metrics-strip">{metrics.map(([label, value]) => <div className="metric" key={label}><span>{label}</span><strong>{value ?? '—'}</strong></div>)}</div>
    <div className="overview-grid">
      <section className="workspace-panel latest-panel"><div className="panel-heading"><div><h2>{ru ? 'Новые позиции' : 'Latest positions'}</h2><p>{ru ? 'Недавно добавленные и обновлённые.' : 'Recently added and updated opportunities.'}</p></div><Link to="/positions">{ru ? 'Все позиции' : 'View all positions'}</Link></div><PositionTable rows={latest} /></section>
      <aside className="profile-callout"><div className="paper-motif" aria-hidden="true"><span /><span /><span /><span /></div><h2>{ru ? 'Один профиль. Много возможностей.' : 'One profile. More possibilities.'}</h2><p>{ru ? 'Ваши навыки и проекты станут основой резюме для каждой позиции.' : 'Your skills and projects become the foundation of a CV tailored to each position.'}</p><Link to={isAuthenticated ? '/profile' : '/register'}>{ru ? 'Перейти к профилю' : 'Go to your profile'}</Link></aside>
      <section className="workspace-panel popular-panel"><div className="panel-heading"><div><h2>{ru ? 'Популярные позиции' : 'Most popular positions'}</h2><p>{ru ? 'Пять позиций с наибольшим числом резюме.' : 'The five positions with the most submitted CVs.'}</p></div></div><PositionTable rows={popular} popular /></section>
      <section className="workspace-panel tags-panel"><div className="panel-heading"><div><h2>{ru ? 'Навыки и технологии' : 'Skills & technologies'}</h2><p>{ru ? 'Исследуйте знакомые направления.' : 'Explore the areas you know best.'}</p></div></div><div className="tag-collection">{tags.map(x => <Link key={x.tag} to={'/search?q=' + encodeURIComponent(x.tag)}>{x.tag}<span>{x.count}</span></Link>)}{!tags.length && <p className="text-body-secondary">{ru ? 'Теги появятся вместе с проектами.' : 'Tags appear as projects are added.'}</p>}</div><p className="activity-note">{stats?.cvsLast24Hours ?? '—'} {ru ? 'новых резюме за 24 часа' : 'new CVs in the last 24 hours'}</p></section>
    </div>
  </div>;
}
