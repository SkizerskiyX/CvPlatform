import { useEffect, useState } from 'react';
import { Alert, Button, Card } from 'react-bootstrap';
import { Link } from 'react-router-dom';
import { cvsApi } from '../api/endpoints';
import type { Cv } from '../api/types';
import { DataTable } from '../components/DataTable';
import { useText } from '../hooks/useText';
export function CvsPage() {
  const text = useText(); const [rows, setRows] = useState<Cv[]>([]); const [selected, setSelected] = useState<string[]>([]); const [error, setError] = useState(''); const [busy, setBusy] = useState(false);
  const load = () => cvsApi.mine().then(setRows);
  useEffect(() => { void load().catch(e => setError(e.message)); }, []);
  const run = async (action: 'publish' | 'unpublish' | 'remove') => {
    if (action === 'remove' && !confirm(text('Delete selected CVs?', 'Удалить выбранные резюме?'))) return;
    setBusy(true); setError('');
    try { if (action === 'remove') await cvsApi.remove(selected); else await Promise.all(selected.map(id => cvsApi[action](id))); setSelected([]); await load(); }
    catch (e) { setError((e as Error).message); await load().catch(() => undefined); }
    finally { setBusy(false); }
  };
  return <Card><Card.Body><Card.Title>{text('My CVs', 'Мои резюме')}</Card.Title>{error && <Alert variant="danger">{error}</Alert>}<DataTable rows={rows} rowKey={x => x.id} selectedIds={selected} onSelectionChange={setSelected} emptyText={text('No CVs yet. Choose a position to generate one.', 'Пока нет резюме. Выберите позицию для создания.')} columns={[
    { key: 'position', header: text('Position', 'Позиция'), render: x => <Link to={'/cvs/' + x.id}>{x.positionTitle}</Link> }, { key: 'candidate', header: text('Candidate', 'Кандидат'), render: x => x.candidateName }, { key: 'status', header: text('Status', 'Статус'), render: x => x.status === 'Published' ? text('Published', 'Опубликовано') : text('Draft', 'Черновик') }, { key: 'likes', header: text('Likes', 'Лайки'), render: x => x.likeCount }, { key: 'updated', header: text('Updated', 'Обновлено'), render: x => new Date(x.updatedAt).toLocaleDateString() }
  ]} toolbar={<div className="d-flex flex-wrap gap-2"><Link className="btn btn-outline-primary" to="/positions">{text('Find a position', 'Выбрать позицию')}</Link>{(['publish', 'unpublish', 'remove'] as const).map(action => <Button key={action} disabled={busy || !selected.length} variant={action === 'remove' ? 'outline-danger' : 'outline-secondary'} onClick={() => void run(action)}>{action === 'publish' ? text('Publish', 'Опубликовать') : action === 'unpublish' ? text('Unpublish', 'Снять с публикации') : text('Delete', 'Удалить')}</Button>)}</div>} /></Card.Body></Card>;
}
