import { useEffect, useState } from 'react';
import { Alert, Button, Card, Form, Tab, Tabs } from 'react-bootstrap';
import Markdown from 'react-markdown';
import { Link, useNavigate, useParams } from 'react-router-dom';
import { apiClient } from '../api/client';
import { cvsApi, discussionsApi, positionsApi } from '../api/endpoints';
import type { Cv, DiscussionPost, Position } from '../api/types';
import { useAuth } from '../hooks/useAuth';
import { useText } from '../hooks/useText';
import { CvTable } from '../components/CvTable';
export function PositionDetailsPage() {
  const { id = '' } = useParams(); const navigate = useNavigate(); const { isAuthenticated, isStaff } = useAuth(); const text = useText();
  const [position, setPosition] = useState<Position | null>(null); const [posts, setPosts] = useState<DiscussionPost[]>([]); const [cvs, setCvs] = useState<Cv[]>([]);
  const [comment, setComment] = useState(''); const [error, setError] = useState(''); const [busy, setBusy] = useState(false);
  useEffect(() => { setPosition(null); void positionsApi.get(id).then(setPosition).catch(e => setError(e.message)); if (isStaff) void apiClient.get<Cv[]>('/api/positions/' + id + '/cvs').then(x => setCvs(x.data)).catch(e => setError(e.message)); }, [id, isStaff]);
  useEffect(() => { if (!isAuthenticated) return; let active = true; let pending = false; const refresh = async () => { if (pending) return; pending = true; try { const data = await discussionsApi.byPosition(id); if (active) setPosts(data); } catch (e) { if (active) setError((e as Error).message); } finally { pending = false; } }; void refresh(); const timer = setInterval(() => void refresh(), 3000); return () => { active = false; clearInterval(timer); }; }, [id, isAuthenticated]);
  const action = async (fn: () => Promise<unknown>) => { setBusy(true); setError(''); try { await fn(); } catch (e) { setError((e as Error).message); } finally { setBusy(false); } };
  if (!position) return <Alert variant={error ? 'danger' : 'info'}>{error || text('Loading…', 'Загрузка…')}</Alert>;
  return <Card><Card.Body><Card.Title>{position.title}</Card.Title><p>{position.company} {position.level}</p>{error && <Alert variant="danger">{error}</Alert>}
    <div className="d-flex gap-2 flex-wrap mb-3">
      {isStaff && <Link className="btn btn-outline-primary" to={'/positions/' + id + '/edit'}>{text('Edit template', 'Редактировать шаблон')}</Link>}
      {isAuthenticated && position.myCvId && <Link className="btn btn-primary" to={'/cvs/' + position.myCvId}>{text('Open my CV', 'Открыть моё резюме')}</Link>}
      {isAuthenticated && position.canApply && !position.myCvId && <Button disabled={busy} onClick={() => void action(async () => { const cv = await cvsApi.generate(id); navigate('/cvs/' + cv.id); })}>{text('Generate CV', 'Создать резюме')}</Button>}
    </div><Tabs defaultActiveKey="details" className="mb-3">
      <Tab eventKey="details" title={text('Position', 'Позиция')}><Markdown>{position.shortDescription ?? ''}</Markdown><h2 className="h5">{text('Template', 'Шаблон')}</h2><div className="table-responsive"><table className="table"><thead><tr><th>{text('Attribute', 'Атрибут')}</th><th>{text('Category', 'Категория')}</th></tr></thead><tbody>{position.attributes.map(x => <tr key={x.attributeDefinitionId}><td>{x.name}</td><td>{x.categoryName}</td></tr>)}</tbody></table></div><p>{text('Project limit', 'Лимит проектов')}: {position.maxProjects}; {position.projectTags.join(', ')}</p><h2 className="h5">{text('Access', 'Доступ')}</h2>{position.isPublic ? text('All signed-in users', 'Все авторизованные пользователи') : <table className="table"><tbody>{position.accessRules.map(x => <tr key={x.id}><td>{x.attributeName}</td><td>{{ EqualTo: '=', NotEquals: '≠', GreaterThan: '>', GreaterOrEqual: '≥', LessThan: '<', LessOrEqual: '≤', In: text('One of', 'Один из') }[x.operator]}</td><td>{x.displayValue}</td></tr>)}</tbody></table>}</Tab>
      {isStaff && <Tab eventKey="cvs" title={text('Submitted CVs', 'Резюме кандидатов')}><CvTable rows={cvs} /></Tab>}
      {isAuthenticated && <Tab eventKey="discussion" title={text('Discussion', 'Обсуждение')}><Form className="d-grid gap-2 mb-3" onSubmit={e => { e.preventDefault(); void action(async () => { await discussionsApi.add(id, comment); setComment(''); setPosts(await discussionsApi.byPosition(id)); }); }}><Form.Control as="textarea" rows={3} maxLength={4000} aria-label={text('Comment (Markdown)', 'Комментарий (Markdown)')} value={comment} onChange={e => setComment(e.target.value)} /><Button type="submit" disabled={busy || !comment.trim()}>{text('Post comment', 'Отправить комментарий')}</Button></Form>{posts.map(x => <article key={x.id} className="border-bottom py-3"><strong>{isStaff ? <Link to={'/profiles/' + x.authorProfileId}>{x.authorName}</Link> : x.authorName}</strong><time className="ms-2">{new Date(x.createdAt).toLocaleString()}</time><Markdown>{x.content}</Markdown></article>)}</Tab>}
    </Tabs></Card.Body></Card>;
}
