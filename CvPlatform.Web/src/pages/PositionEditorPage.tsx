import { useEffect, useState } from 'react';
import { Alert, Button, Card, Form } from 'react-bootstrap';
import { useNavigate, useParams } from 'react-router-dom';
import CreatableSelect from 'react-select/creatable';
import Markdown from 'react-markdown';
import { apiClient, ApiError } from '../api/client';
import { positionsApi, profilesApi } from '../api/endpoints';
import type { AttributeDefinition, ComparisonOperator, PositionLevel, SavePosition } from '../api/types';
import { AttributePicker } from '../components/AttributePicker';
import { DataTable } from '../components/DataTable';
import { useText } from '../hooks/useText';
import { useUnsavedChanges } from '../hooks/useUnsavedChanges';
import { PositionExportAction } from '../components/PositionExportAction';

const empty: SavePosition = { title: '', shortDescription: '', company: '', level: null, isPublic: true, maxProjects: 5, projectTags: [], attributeIds: [], accessRules: [] };
function operators(type: string): ComparisonOperator[] {
  if (type === 'Boolean') return ['EqualTo'];
  if (['Numeric', 'Date', 'Period'].includes(type)) return ['EqualTo', 'NotEquals', 'GreaterThan', 'GreaterOrEqual', 'LessThan', 'LessOrEqual'];
  return type === 'Image' ? [] : ['EqualTo', 'NotEquals', 'In'];
}
export function PositionEditorPage() {
  const { id } = useParams(); const navigate = useNavigate(); const text = useText();
  const [form, setForm] = useState<SavePosition>(empty); const [definitions, setDefinitions] = useState<AttributeDefinition[]>([]);
  const [selected, setSelected] = useState<string[]>([]); const [selectedRules, setSelectedRules] = useState<string[]>([]);
  const [ruleDefinition, setRuleDefinition] = useState<AttributeDefinition | null>(null);
  const [operator, setOperator] = useState<ComparisonOperator>('EqualTo'); const [value, setValue] = useState('');
  const [tags, setTags] = useState<string[]>([]); const [dirty, setDirty] = useState(false); const [ready, setReady] = useState(!id);
  const [error, setError] = useState(''); const [busy, setBusy] = useState(false); const [conflict, setConflict] = useState(false);
  const [createdId, setCreatedId] = useState<string | null>(null);
  useEffect(() => { if (createdId && !dirty) navigate('/positions/' + createdId + '/edit', { replace: true }); }, [createdId, dirty, navigate]);
  useUnsavedChanges(dirty);
  const change = (next: Partial<SavePosition>) => { setForm(current => ({ ...current, ...next })); setDirty(true); };
  const load = async () => {
    if (!id) return;
    const p = await positionsApi.get(id);
    const ids = [...new Set([...p.attributes.map(x => x.attributeDefinitionId), ...p.accessRules.map(x => x.attributeDefinitionId)])];
    const batches = await Promise.all(Array.from({ length: Math.ceil(ids.length / 50) }, (_, i) => apiClient.post<AttributeDefinition[]>('/api/attributes/lookup', { ids: ids.slice(i * 50, i * 50 + 50) }).then(x => x.data)));
    setDefinitions(batches.flat()); setForm({ ...p, attributeIds: p.attributes.map(x => x.attributeDefinitionId), accessRules: p.accessRules.map(x => ({ attributeDefinitionId: x.attributeDefinitionId, operator: x.operator, comparisonValue: x.comparisonValue })) }); setDirty(false); setConflict(false); setReady(true); setError('');
  };
  useEffect(() => { void load().catch(e => setError(e.message)); void profilesApi.tagSuggestions('').then(setTags).catch(() => undefined); }, [id]);
  const addDefinition = (d: AttributeDefinition) => setDefinitions(current => [...current.filter(x => x.id !== d.id), d]);
  const save = async () => {
    setBusy(true);
    try { const body = { ...form, accessRules: form.isPublic ? [] : form.accessRules }; if (id) { await positionsApi.update(id, body); await load(); } else { const result = await positionsApi.create(body); setDirty(false); setCreatedId(result.id); } }
    catch (e) { setError((e as Error).message); setConflict(e instanceof ApiError && e.status === 409); }
    finally { setBusy(false); }
  };
  const labels: Record<ComparisonOperator, string> = { EqualTo: '=', NotEquals: '≠', GreaterThan: '>', GreaterOrEqual: '≥', LessThan: '<', LessOrEqual: '≤', In: text('One of', 'Один из') };
  return <Card><Card.Body><Card.Title>{text('Position template', 'Шаблон позиции')}</Card.Title>
    {error && <Alert variant="danger">{error}</Alert>}{conflict && <Alert variant="warning">{text('Changed by another user. Copy your changes before reloading.', 'Изменено другим пользователем. Скопируйте ввод перед обновлением.')}<Button onClick={() => { if (confirm(text('Discard and reload?', 'Отменить изменения и обновить?'))) void load().catch(e => setError(e.message)); }}>{text('Reload', 'Обновить')}</Button></Alert>}
    {ready && <Form onSubmit={e => { e.preventDefault(); void save(); }}><fieldset disabled={busy} className="d-grid gap-4">
      <Form.Label>{text('Title', 'Название')}<Form.Control required maxLength={200} value={form.title} onChange={e => change({ title: e.target.value })} /></Form.Label>
      <Form.Label>{text('Description (Markdown)', 'Описание (Markdown)')}<Form.Control as="textarea" rows={4} maxLength={2000} value={form.shortDescription ?? ''} onChange={e => change({ shortDescription: e.target.value })} /></Form.Label><Markdown>{form.shortDescription ?? ''}</Markdown>
      <Form.Label>{text('Company', 'Компания')}<Form.Control maxLength={200} value={form.company ?? ''} onChange={e => change({ company: e.target.value })} /></Form.Label>
      <Form.Label>{text('Level', 'Уровень')}<Form.Select value={form.level ?? ''} onChange={e => change({ level: (e.target.value || null) as PositionLevel | null })}><option value="">—</option>{['Junior', 'Middle', 'Senior', 'CLevel'].map(x => <option key={x}>{x}</option>)}</Form.Select></Form.Label>
      <section><h2 className="h5">{text('Template attributes', 'Атрибуты шаблона')}</h2><AttributePicker onSelect={d => { addDefinition(d); if (!form.attributeIds.includes(d.id)) change({ attributeIds: [...form.attributeIds, d.id] }); }} />
        <DataTable rows={form.attributeIds.map(id => definitions.find(x => x.id === id)).filter((x): x is AttributeDefinition => !!x)} rowKey={x => x.id} selectedIds={selected} onSelectionChange={setSelected} emptyText={text('Select attributes above', 'Выберите атрибуты выше')} columns={[{ key: 'name', header: text('Name', 'Название'), render: x => x.name }]}
          toolbar={<Button variant="outline-danger" disabled={!selected.length} onClick={() => { change({ attributeIds: form.attributeIds.filter(x => !selected.includes(x)) }); setSelected([]); }}>{text('Remove from template', 'Убрать из шаблона')}</Button>} /></section>
      <section><h2 className="h5">{text('Access rules', 'Правила доступа')}</h2><Form.Check id="position-public" label={text('Available to all signed-in users', 'Доступна всем авторизованным пользователям')} checked={form.isPublic} onChange={e => change({ isPublic: e.target.checked })} />
        {!form.isPublic && <div className="d-grid gap-2"><p>{text('All rules must match. An empty restricted position is inaccessible to candidates.', 'Все условия должны выполняться. Без правил закрытая позиция недоступна кандидатам.')}</p><AttributePicker onSelect={d => { addDefinition(d); setRuleDefinition(d); setOperator('EqualTo'); setValue(d.dataType === 'Boolean' ? 'true' : ''); }} />
          {ruleDefinition && <><strong>{ruleDefinition.name}</strong><Form.Select aria-label={text('Operator', 'Оператор')} value={operator} onChange={e => { setOperator(e.target.value as ComparisonOperator); setValue(ruleDefinition.dataType === 'Boolean' ? 'true' : ''); }}>{operators(ruleDefinition.dataType).map(x => <option key={x} value={x}>{labels[x]}</option>)}</Form.Select>
            {ruleDefinition.dataType === 'Dropdown' ? <Form.Select multiple={operator === 'In'} aria-label={text('Value', 'Значение')} value={operator === 'In' ? value.split(',') : value} onChange={e => setValue(Array.from(e.target.selectedOptions).map(x => x.value).join(','))}><option value="">—</option>{ruleDefinition.options.map(x => <option key={x.id} value={x.id}>{x.value}</option>)}</Form.Select>
              : ruleDefinition.dataType === 'Boolean' ? <Form.Select aria-label={text('Value', 'Значение')} value={value} onChange={e => setValue(e.target.value)}><option value="true">{text('Yes', 'Да')}</option><option value="false">{text('No', 'Нет')}</option></Form.Select>
                : <Form.Control aria-label={text('Value', 'Значение')} type={ruleDefinition.dataType === 'Numeric' ? 'number' : ['Date', 'Period'].includes(ruleDefinition.dataType) ? 'date' : 'text'} step="any" value={value} onChange={e => setValue(e.target.value)} />}
            <Button disabled={!value || !operators(ruleDefinition.dataType).length || form.accessRules.length >= 30} onClick={() => { change({ accessRules: [...form.accessRules, { attributeDefinitionId: ruleDefinition.id, operator, comparisonValue: value }] }); setRuleDefinition(null); }}>{text('Add rule', 'Добавить правило')}</Button></>}
          <DataTable rows={form.accessRules.map((x, index) => ({ ...x, id: String(index) }))} rowKey={x => x.id} selectedIds={selectedRules} onSelectionChange={setSelectedRules} emptyText={text('No rules', 'Нет правил')} columns={[{ key: 'attribute', header: text('Attribute', 'Атрибут'), render: x => definitions.find(d => d.id === x.attributeDefinitionId)?.name }, { key: 'operator', header: text('Operator', 'Оператор'), render: x => labels[x.operator] }, { key: 'value', header: text('Value', 'Значение'), render: x => x.comparisonValue.split(',').map(v => definitions.find(d => d.id === x.attributeDefinitionId)?.options.find(o => o.id === v)?.value ?? v).join(', ') }]} toolbar={<Button variant="outline-danger" disabled={!selectedRules.length} onClick={() => { change({ accessRules: form.accessRules.filter((_, i) => !selectedRules.includes(String(i))) }); setSelectedRules([]); }}>{text('Delete rules', 'Удалить правила')}</Button>} />
        </div>}
      </section>
      <Form.Label>{text('Project tags', 'Теги проектов')}<CreatableSelect isMulti isDisabled={busy} placeholder={text('Select or create tags', 'Выберите или создайте теги')} noOptionsMessage={() => text('No options', 'Нет вариантов')} formatCreateLabel={value => text('Create: ', 'Создать: ') + value} options={tags.map(value => ({ value, label: value }))} value={form.projectTags.map(value => ({ value, label: value }))} onChange={items => change({ projectTags: items.map(x => x.value) })} /></Form.Label>
      <Form.Label>{text('Maximum projects', 'Максимум проектов')}<Form.Control type="number" min={0} max={20} required value={form.maxProjects} onChange={e => change({ maxProjects: Number(e.target.value) })} /></Form.Label>
      <Button type="submit" disabled={busy || conflict || !dirty}>{text('Save position', 'Сохранить позицию')}</Button>
    </fieldset></Form>}
    {id && <PositionExportAction positionId={id} disabled={!ready || dirty || busy} />}
  </Card.Body></Card>;
}
