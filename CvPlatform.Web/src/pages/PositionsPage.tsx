import { useEffect, useState } from 'react';
import { Alert, Button, Card, Form } from 'react-bootstrap';
import { Link, useNavigate } from 'react-router-dom';
import { positionsApi } from '../api/endpoints';
import type { PositionListItem } from '../api/types';
import { DataTable } from '../components/DataTable';
import { useAuth } from '../hooks/useAuth';
import { useText } from '../hooks/useText';
export function PositionsPage() {
  const text = useText(); const navigate = useNavigate(); const { isStaff } = useAuth();
  const [rows, setRows] = useState<PositionListItem[]>([]); const [selected, setSelected] = useState<string[]>([]);
  const [search, setSearch] = useState(''); const [level, setLevel] = useState(''); const [error, setError] = useState(''); const [busy, setBusy] = useState(false); const [sort, setSort] = useState('title');
  const load = () => positionsApi.list().then(setRows);
  useEffect(() => { void load().catch(e => setError(e.message)); }, []);
  const run = async (action: () => Promise<unknown>) => { setBusy(true); try { await action(); } catch (e) { setError((e as Error).message); } finally { setBusy(false); } };
  const visible = rows.filter(x => (!level || x.level === level) && (x.title + ' ' + x.company).toLowerCase().includes(search.toLowerCase())).sort((a, b) => sort === 'updated' ? b.updatedAt.localeCompare(a.updatedAt) : sort === 'cvs' ? b.publishedCvCount - a.publishedCvCount : a.title.localeCompare(b.title));
  return <Card><Card.Body><Card.Title>{text('Positions', 'Позиции')}</Card.Title>{error && <Alert variant="danger">{error}</Alert>}<DataTable rows={visible} rowKey={x => x.id} selectedIds={selected} onSelectionChange={setSelected} emptyText={text('Nothing to display', 'Нет данных')} columns={[
    { key: 'title', header: text('Position', 'Позиция'), render: x => <Link to={'/positions/' + x.id}>{x.title}</Link> }, { key: 'company', header: text('Company', 'Компания'), render: x => x.company }, { key: 'level', header: text('Level', 'Уровень'), render: x => x.level }, { key: 'attributes', header: text('Attributes', 'Атрибуты'), render: x => x.attributeCount }, { key: 'cvs', header: text('Published CVs', 'Опубликованные CV'), render: x => x.publishedCvCount }
  ]} toolbar={<div className="d-flex flex-wrap gap-2"><Form.Control placeholder={text('Search positions', 'Поиск позиций')} aria-label={text('Search positions', 'Поиск позиций')} value={search} onChange={e => setSearch(e.target.value)} /><Form.Select aria-label={text('Level', 'Уровень')} value={level} onChange={e => setLevel(e.target.value)}><option value="">{text('All levels', 'Все уровни')}</option>{['Junior', 'Middle', 'Senior', 'CLevel'].map(x => <option key={x}>{x}</option>)}</Form.Select><Form.Select aria-label={text('Sort', 'Сортировка')} value={sort} onChange={e => setSort(e.target.value)}><option value="title">{text('Title', 'Название')}</option><option value="updated">{text('Recently updated', 'Недавно обновлённые')}</option><option value="cvs">{text('CV count', 'Число резюме')}</option></Form.Select>
    {isStaff && <><Link className="btn btn-primary" to="/positions/new">{text('Create', 'Создать')}</Link><Button disabled={selected.length !== 1 || busy} onClick={() => navigate('/positions/' + selected[0] + '/edit')}>{text('Edit', 'Изменить')}</Button><Button disabled={selected.length !== 1 || busy} onClick={() => void run(async () => { const x = await positionsApi.duplicate(selected[0]); navigate('/positions/' + x.id + '/edit'); })}>{text('Duplicate', 'Дублировать')}</Button><Button variant="outline-danger" disabled={!selected.length || busy} onClick={() => { if (confirm(text('Delete selected positions?', 'Удалить выбранные позиции?'))) void run(async () => { await positionsApi.remove(selected); setSelected([]); await load(); }); }}>{text('Delete', 'Удалить')}</Button></>}
  </div>} /></Card.Body></Card>;
}
