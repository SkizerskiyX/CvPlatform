import { useEffect, useState } from 'react';
import { Alert, Button, Card, Form, Modal } from 'react-bootstrap';
import { attributesApi } from '../api/endpoints';
import type { AttributeCategory, AttributeDataType, AttributeDefinition, SaveAttributeDefinition } from '../api/types';
import { DataTable } from '../components/DataTable';
import { useAuth } from '../hooks/useAuth';
import { useText } from '../hooks/useText';
import { ApiError } from '../api/client';
import { useUnsavedChanges } from '../hooks/useUnsavedChanges';

const empty: SaveAttributeDefinition = { name: '', description: '', dataType: 'String', categoryId: '', dropdownOptions: [] };
const types = { String: 'Строка', Text: 'Текст', Image: 'Изображение', Numeric: 'Число', Date: 'Дата', Period: 'Период', Boolean: 'Да/нет', Dropdown: 'Список' };
export function AttributesPage() {
  const text = useText(); const { isStaff } = useAuth();
  const [rows, setRows] = useState<AttributeDefinition[]>([]);
  const [categories, setCategories] = useState<AttributeCategory[]>([]);
  const [selected, setSelected] = useState<string[]>([]);
  const [prefix, setPrefix] = useState(''); const [category, setCategory] = useState('');
  const [form, setForm] = useState<SaveAttributeDefinition | null>(null);
  const [editing, setEditing] = useState<AttributeDefinition | null>(null);
  const [options, setOptions] = useState(''); const [error, setError] = useState(''); const [conflict, setConflict] = useState(false); const [busy, setBusy] = useState(false);
  useUnsavedChanges(form !== null);
  const load = () => attributesApi.search(prefix, category).then(setRows);
  useEffect(() => { let active = true; const timer = setTimeout(() => { void attributesApi.search(prefix, category).then(x => { if (active) setRows(x); }).catch(e => setError(e.message)); }, 250); return () => { active = false; clearTimeout(timer); }; }, [prefix, category]);
  useEffect(() => { void attributesApi.categories().then(setCategories).catch(e => setError(e.message)); }, []);
  const open = (row?: AttributeDefinition) => { setEditing(row ?? null); setForm(row ? { ...row, dropdownOptions: row.options.map(x => x.value) } : { ...empty }); setOptions(row?.options.map(x => x.value).join('\n') ?? ''); setConflict(false); setError(''); };
  const save = async () => {
    if (!form || busy || conflict) return;
    setBusy(true);
    try { const body = { ...form, dropdownOptions: form.dataType === 'Dropdown' ? options.split('\n').map(x => x.trim()).filter(Boolean) : [] }; if (editing) await attributesApi.update(editing.id, body); else await attributesApi.create(body); setForm(null); await load(); }
    catch (e) { setConflict(e instanceof ApiError && e.status === 409); setError((e as Error).message); }
    finally { setBusy(false); }
  };
  return <Card><Card.Body><Card.Title>{text('Attribute library', 'Библиотека атрибутов')}</Card.Title>
    {error && !form && <Alert variant="danger">{error}</Alert>}
    <DataTable rows={rows} rowKey={x => x.id} selectedIds={selected} onSelectionChange={setSelected} emptyText={text('Nothing to display', 'Нет данных')}
      columns={[{ key: 'name', header: text('Name', 'Название'), render: x => x.name }, { key: 'description', header: text('Description', 'Описание'), render: x => x.description }, { key: 'category', header: text('Category', 'Категория'), render: x => x.categoryName }, { key: 'type', header: text('Type', 'Тип'), render: x => text(x.dataType, types[x.dataType]) }]}
      toolbar={<div className="d-flex flex-wrap gap-2"><Form.Control aria-label={text('Name prefix', 'Начало названия')} placeholder={text('Name prefix', 'Начало названия')} value={prefix} onChange={e => setPrefix(e.target.value)} /><Form.Select value={category} onChange={e => setCategory(e.target.value)} aria-label={text('Category', 'Категория')}><option value="">{text('All categories', 'Все категории')}</option>{categories.map(x => <option key={x.id} value={x.id}>{x.name}</option>)}</Form.Select>{isStaff && <><Button onClick={() => open()}>{text('Create', 'Создать')}</Button><Button disabled={selected.length !== 1} onClick={() => open(rows.find(x => x.id === selected[0]))}>{text('Edit', 'Изменить')}</Button><Button variant="outline-danger" disabled={!selected.length || rows.some(x => selected.includes(x.id) && x.isBuiltIn)} onClick={() => { if (confirm(text('Delete selected attributes?', 'Удалить выбранные атрибуты?'))) void attributesApi.remove(selected).then(() => { setSelected([]); return load(); }).catch(e => setError(e.message)); }}>{text('Delete', 'Удалить')}</Button></>}</div>} />
    <Modal show={form !== null} onHide={() => { if (!busy && confirm(text('Discard changes?', 'Отменить изменения?'))) setForm(null); }} centered><Modal.Header closeButton><Modal.Title>{text('Attribute', 'Атрибут')}</Modal.Title></Modal.Header>{form && <Form onSubmit={e => { e.preventDefault(); void save(); }}><fieldset disabled={busy}><Modal.Body className="d-grid gap-3">
      {error && <Alert variant="danger">{error}</Alert>}
      {conflict && <Alert variant="warning">{text('The attribute changed. Copy your changes and reload.', 'Атрибут изменён. Скопируйте свой ввод и загрузите новую версию.')}<Button onClick={() => { if (editing) void attributesApi.search(editing.name).then(rows => { const fresh = rows.find(x => x.id === editing.id); if (fresh) open(fresh); }).catch(e => setError(e.message)); }}>{text('Reload', 'Обновить')}</Button></Alert>}
      <Form.Label>{text('Name', 'Название')}<Form.Control required maxLength={200} value={form.name} onChange={e => setForm({ ...form, name: e.target.value })} /></Form.Label>
      <Form.Label>{text('Description', 'Описание')}<Form.Control as="textarea" maxLength={2000} value={form.description ?? ''} onChange={e => setForm({ ...form, description: e.target.value })} /></Form.Label>
      <Form.Label>{text('Category', 'Категория')}<Form.Select required value={form.categoryId} onChange={e => setForm({ ...form, categoryId: e.target.value })}><option value="">—</option>{categories.map(x => <option key={x.id} value={x.id}>{x.name}</option>)}</Form.Select></Form.Label>
      <Form.Label>{text('Type', 'Тип')}<Form.Select disabled={editing?.isBuiltIn} value={form.dataType} onChange={e => setForm({ ...form, dataType: e.target.value as AttributeDataType })}>{Object.entries(types).map(([en, ru]) => <option key={en} value={en}>{text(en, ru)}</option>)}</Form.Select></Form.Label>
      {form.dataType === 'Dropdown' && <Form.Label>{text('Options, one per line', 'Варианты, по одному в строке')}<Form.Control required as="textarea" rows={5} value={options} onChange={e => setOptions(e.target.value)} /></Form.Label>}
    </Modal.Body><Modal.Footer><Button type="submit" disabled={busy || conflict}>{text('Save', 'Сохранить')}</Button></Modal.Footer></fieldset></Form>}</Modal>
  </Card.Body></Card>;
}
