import { useEffect, useId, useState } from 'react';
import { Form } from 'react-bootstrap';
import { apiClient } from '../api/client';
import { attributesApi } from '../api/endpoints';
import type { AttributeCategory, AttributeDefinition } from '../api/types';
import { useText } from '../hooks/useText';
const key = 'cvplatform.recentAttributes';
function recentIds(): string[] {
  try { const ids: unknown = JSON.parse(localStorage.getItem(key) ?? '[]'); return Array.isArray(ids) ? ids.filter(x => typeof x === 'string').slice(0, 20) : []; } catch { return []; }
}
export function AttributePicker({ onSelect }: { onSelect: (item: AttributeDefinition) => void }) {
  const text = useText();
  const recentId = useId();
  const [prefix, setPrefix] = useState(''); const [category, setCategory] = useState(''); const [recent, setRecent] = useState(false);
  const [rows, setRows] = useState<AttributeDefinition[]>([]); const [categories, setCategories] = useState<AttributeCategory[]>([]); const [error, setError] = useState('');
  useEffect(() => { void attributesApi.categories().then(setCategories).catch(e => setError(e.message)); }, []);
  useEffect(() => {
    let active = true;
    const timer = setTimeout(() => {
      const request = recent ? apiClient.post<AttributeDefinition[]>('/api/attributes/lookup', { ids: recentIds() }).then(x => x.data) : attributesApi.search(prefix, category);
      void request.then(data => { if (active) { setRows(data); setError(''); } }).catch(e => { if (active) setError(e.message); });
    }, 250);
    return () => { active = false; clearTimeout(timer); };
  }, [prefix, category, recent]);
  return <div className="d-grid gap-2">
    <Form.Control aria-label={text('Name prefix', 'Начало названия')} placeholder={text('Name prefix', 'Начало названия')} value={prefix} onChange={e => setPrefix(e.target.value)} />
    <Form.Select aria-label={text('Category', 'Категория')} value={category} onChange={e => setCategory(e.target.value)}><option value="">{text('All categories', 'Все категории')}</option>{categories.map(x => <option key={x.id} value={x.id}>{x.name}</option>)}</Form.Select>
    <Form.Check id={recentId} label={text('Recently used', 'Недавно выбранные')} checked={recent} onChange={e => setRecent(e.target.checked)} />
    <Form.Select aria-label={text('Select attribute', 'Выбрать атрибут')} value="" onChange={e => { const item = rows.find(x => x.id === e.target.value); if (item) { localStorage.setItem(key, JSON.stringify([item.id, ...recentIds().filter(x => x !== item.id)].slice(0, 20))); onSelect(item); } }}>
      <option value="">{text('Select attribute', 'Выбрать атрибут')}</option>{rows.filter(x => (!category || x.categoryId === category) && x.name.toLowerCase().startsWith(prefix.toLowerCase())).map(x => <option key={x.id} value={x.id}>{x.name}</option>)}
    </Form.Select>{error && <p role="alert" className="text-danger">{error}</p>}
  </div>;
}
