import { useEffect, useRef, useState } from 'react';
import { Alert, Button, Card } from 'react-bootstrap';
import Markdown from 'react-markdown';
import { Link, useParams } from 'react-router-dom';
import { apiClient } from '../api/client';
import { cvsApi, likesApi } from '../api/endpoints';
import type { AttributeValueInput, CvDocument, CvField } from '../api/types';
import { AttributeValueEditor } from '../components/AttributeValueEditor';
import { useAutosave } from '../hooks/useAutosave';
import { useUnsavedChanges } from '../hooks/useUnsavedChanges';
import { useText } from '../hooks/useText';
import { useTranslation } from 'react-i18next';
export function CvDetailsPage() {
  const { id = '' } = useParams(); const text = useText(); const { t } = useTranslation();
  const [cv, setCv] = useState<CvDocument | null>(null); const current = useRef<CvDocument | null>(null);
  const [draft, setDraft] = useState<Record<string, AttributeValueInput>>({});
  const [error, setError] = useState(''); const [acting, setActing] = useState(false);
  const autosave = useAutosave(draft, async values => {
    const document = current.current; if (!document) return;
    try {
      const result = await apiClient.put<{ version: number }>('/api/cvs/' + id + '/attributes', { version: document.profileVersion, changes: Object.entries(values).map(([attributeDefinitionId, value]) => ({ attributeDefinitionId, value })), removedAttributeIds: [] });
      current.current = { ...document, profileVersion: result.data.version };
      const fresh = await cvsApi.get(id); current.current = fresh; setCv(fresh); setError('');
    } catch (e) { setError((e as Error).message); throw e; }
  });
  useUnsavedChanges(autosave.dirty);
  const load = async () => {
    const result = await cvsApi.get(id); current.current = result; setCv(result);
    setDraft({}); autosave.markPersisted({}); setError('');
  };
  useEffect(() => { void load().catch(e => setError(e.message)); }, [id]);
  const run = async (fn: () => Promise<unknown>) => { setActing(true); try { await fn(); await load(); } catch (e) { setError((e as Error).message); } finally { setActing(false); } };
  if (!cv) return <Alert variant={error ? 'danger' : 'info'}>{error || text('Loading…', 'Загрузка…')}</Alert>;
  const renderFields = (fields: CvField[]) => <div className="table-responsive"><table className="table align-middle"><tbody>{fields.map(field => <tr key={field.definition.id} className={!field.value.display ? 'table-danger' : ''}><th style={{ minWidth: 130 }}>{field.definition.name}</th><td>{cv.canEdit ? <AttributeValueEditor dataType={field.definition.dataType} options={field.definition.options} value={draft[field.definition.id] ?? field.value.value} onChange={value => setDraft(current => ({ ...current, [field.definition.id]: value }))} /> : field.definition.dataType === 'Text' ? <Markdown>{field.value.display ?? ''}</Markdown> : field.definition.dataType === 'Image' && field.value.value.imageUrl ? <img className="photo-preview" src={field.value.value.imageUrl} alt={field.definition.name} /> : field.value.display || text('Missing', 'Не заполнено')}</td></tr>)}</tbody></table></div>;
  return <div className="d-grid gap-3"><Card><Card.Body><Card.Title>{cv.positionTitle}</Card.Title><p>{cv.candidateName} · {cv.positionCompany}</p><p>{cv.status === 'Published' ? text('Published', 'Опубликовано') : text('Draft', 'Черновик')}</p>{error && <Alert variant="danger">{error}</Alert>}
    {cv.canEdit && <p role="status">{t('profile.autosave.' + autosave.status)}</p>}
    {autosave.status === 'conflict' && <Alert variant="warning">{t('profile.conflictNotice')}<Button onClick={() => { if (confirm(t('profile.discardNotice'))) void load().catch(e => setError(e.message)); }}>{t('common.reload')}</Button></Alert>}
    <div className="d-flex gap-2 flex-wrap"><Link className="btn btn-outline-secondary" to={'/positions/' + cv.positionId}>{text('Position', 'Позиция')}</Link>{cv.canEdit && <><Button disabled={acting || autosave.dirty || autosave.busy.current || cv.missingCount > 0 || cv.status === 'Published'} onClick={() => void run(() => cvsApi.publish(id))}>{text('Publish', 'Опубликовать')}</Button><Button disabled={acting || autosave.dirty || autosave.busy.current || cv.status !== 'Published'} onClick={() => void run(() => cvsApi.unpublish(id))}>{text('Unpublish', 'Снять с публикации')}</Button></>}{cv.canLike && <Button disabled={acting || autosave.dirty} onClick={() => void run(() => likesApi.toggle(id))}>{cv.likedByMe ? text('Unlike', 'Убрать лайк') : text('Like', 'Нравится')} · {cv.likeCount}</Button>}</div>{cv.missingCount > 0 && <p className="text-danger mt-2">{text('Missing values', 'Незаполненные поля')}: {cv.missingCount}</p>}
  </Card.Body></Card><Card><Card.Body><h2 className="h5">{text('Personal details', 'Личные данные')}</h2>{renderFields(cv.header)}{cv.sections.map(section => <section key={section.title}><h2 className="h5">{section.title}</h2>{renderFields(section.fields)}</section>)}</Card.Body></Card><Card><Card.Body><h2 className="h5">{text('Projects', 'Проекты')}</h2>{cv.projects.map(p => <article key={p.id} className="mb-4"><h3 className="h6">{p.name}</h3><p>{p.periodStart.slice(0, 10)} — {p.periodEnd?.slice(0, 10) ?? text('Present', 'Настоящее время')}</p><Markdown>{p.description ?? ''}</Markdown><small>{p.tags.join(', ')}</small></article>)}</Card.Body></Card></div>;
}
