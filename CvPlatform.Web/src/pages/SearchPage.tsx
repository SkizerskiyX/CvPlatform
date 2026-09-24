import { useEffect, useState } from 'react';
import { Alert } from 'react-bootstrap';
import { Link, useSearchParams } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { discoveryApi } from '../api/endpoints';
import type { SearchResult } from '../api/types';
export function SearchPage() {
  const [params] = useSearchParams(); const q = params.get('q') ?? '';
  const { i18n } = useTranslation(); const ru = i18n.language.startsWith('ru');
  const [result, setResult] = useState<SearchResult | null>(null);
  const [error, setError] = useState('');
  useEffect(() => { let active = true; setResult(null); setError(''); void discoveryApi.search(q).then(value => { if (active) setResult(value); }).catch((e: Error) => { if (active) setError(e.message); }); return () => { active = false; }; }, [q]);
  return <div className="d-grid gap-4">
    <p className="search-summary">{ru ? 'Результаты для' : 'Results for'} <strong>“{q}”</strong>{result && ' · ' + (result.positions.length + result.cvs.length)}</p>
    {error && <Alert variant="danger">{error}</Alert>}
    {!result && !error && <p role="status">{ru ? 'Ищем подходящие результаты…' : 'Finding matching results…'}</p>}
    {result && !result.positions.length && !result.cvs.length && <div className="workspace-panel empty-state"><h2>{ru ? 'Совпадений пока нет' : 'No matches yet'}</h2><p className="mx-auto">{ru ? 'Попробуйте название позиции, навык или другое ключевое слово.' : 'Try a position title, a skill, or another keyword.'}</p><Link to="/positions">{ru ? 'Посмотреть все позиции' : 'Browse all positions'}</Link></div>}
    {!!result?.positions.length && <section className="workspace-panel"><div className="panel-heading"><h2>{ru ? 'Позиции' : 'Positions'}</h2><span>{result.positions.length}</span></div><div className="table-responsive"><table className="table overview-table"><thead><tr><th>{ru ? 'Название' : 'Title'}</th><th>{ru ? 'Компания' : 'Company'}</th><th>{ru ? 'Уровень' : 'Level'}</th></tr></thead><tbody>{result.positions.map(x => <tr key={x.id}><td><Link to={'/positions/' + x.id}>{x.title}</Link></td><td>{x.company ?? '—'}</td><td>{x.level ?? '—'}</td></tr>)}</tbody></table></div></section>}
    {!!result?.cvs.length && <section className="workspace-panel"><div className="panel-heading"><h2>{ru ? 'Резюме' : 'CVs'}</h2><span>{result.cvs.length}</span></div><div className="table-responsive"><table className="table overview-table"><thead><tr><th>{ru ? 'Кандидат' : 'Candidate'}</th><th>{ru ? 'Позиция' : 'Position'}</th><th>{ru ? 'Лайки' : 'Likes'}</th></tr></thead><tbody>{result.cvs.map(x => <tr key={x.id}><td><Link to={'/cvs/' + x.id}>{x.candidateName}</Link></td><td>{x.positionTitle}</td><td>{x.likeCount}</td></tr>)}</tbody></table></div></section>}
  </div>;
}
