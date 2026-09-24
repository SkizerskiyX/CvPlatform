import { useCallback, useEffect, useRef, useState } from 'react';
import { Alert, Button, Card, Form, Modal, Tab, Tabs } from 'react-bootstrap';
import { Link, useParams } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import CreatableSelect from 'react-select/creatable';
import Markdown from 'react-markdown';
import { attributesApi, profilesApi } from '../api/endpoints';
import { ApiError } from '../api/client';
import { emptyAttributeValue } from '../api/types';
import type { AttributeCategory, AttributeDefinition, AttributeValueInput, ProfileEditor, Project } from '../api/types';
import { AttributeValueEditor } from '../components/AttributeValueEditor';
import { DataTable } from '../components/DataTable';
import type { Column } from '../components/DataTable';

type Draft = { values: Record<string, AttributeValueInput>; removed: string[] };
const emptyProject = { name: '', periodStart: '', periodEnd: '', description: '', tags: [] as string[] };

export function ProfilePage() {
  const { id } = useParams();
  const { t } = useTranslation();
  const [profile, setProfile] = useState<ProfileEditor | null>(null);
  const profileRef = useRef<ProfileEditor | null>(null);
  const [draft, setDraft] = useState<Draft>({ values: {}, removed: [] });
  const draftRef = useRef(draft);
  const busy = useRef(false);
  const blocked = useRef(false);
  const [status, setStatus] = useState('idle');
  const [error, setError] = useState('');
  const [selected, setSelected] = useState<string[]>([]);
  const [selectedInfo, setSelectedInfo] = useState<string[]>([]);
  const [library, setLibrary] = useState<AttributeDefinition[]>([]);
  const [categories, setCategories] = useState<AttributeCategory[]>([]);
  const [prefix, setPrefix] = useState('');
  const [category, setCategory] = useState('');
  const [attributeId, setAttributeId] = useState('');
  const [projectForm, setProjectForm] = useState(emptyProject);
  const [projectId, setProjectId] = useState<string | null>(null);
  const [showProject, setShowProject] = useState(false);
  const [tagOptions, setTagOptions] = useState<string[]>([]);

  const changeDraft = (next: Draft) => {
    draftRef.current = next;
    setDraft(next);
    if (!blocked.current) setStatus('pending');
  };
  const load = useCallback(async () => {
    const result = id ? await profilesApi.editor(id) : await profilesApi.me();
    profileRef.current = result;
    setProfile(result);
    const clean = { values: {}, removed: [] };
    draftRef.current = clean;
    setDraft(clean);
    blocked.current = false;
    setStatus('idle');
    setError('');
  }, [id]);

  useEffect(() => { void load().catch((e: Error) => setError(e.message)); }, [load]);
  useEffect(() => {
    let active = true;
    const timer = window.setTimeout(() => {
      void attributesApi.search(prefix, category).then(result => { if (active) setLibrary(result); }).catch((e: Error) => { if (active) setError(e.message); });
    }, 250);
    return () => { active = false; window.clearTimeout(timer); };
  }, [prefix, category]);
  useEffect(() => {
    void attributesApi.categories().then(setCategories).catch((e: Error) => setError(e.message));
    void profilesApi.tagSuggestions('').then(setTagOptions).catch(() => undefined);
  }, []);

  // Keep the interval independent of keystrokes. Only acknowledge the submitted snapshot.
  useEffect(() => {
    const timer = window.setInterval(() => {
      const current = profileRef.current;
      const snapshot = draftRef.current;
      if (!current || busy.current || blocked.current || (!Object.keys(snapshot.values).length && !snapshot.removed.length)) return;
      busy.current = true;
      void profilesApi.autosave(current.id, current.version,
        Object.entries(snapshot.values).map(([attributeDefinitionId, value]) => ({ attributeDefinitionId, value })), snapshot.removed)
        .then(result => {
          const updated = { ...(profileRef.current ?? current), version: result.version,
            values: [...current.values.filter(v => !(v.attributeDefinitionId in snapshot.values) && !snapshot.removed.includes(v.attributeDefinitionId)),
              ...Object.entries(snapshot.values).map(([attributeDefinitionId, value]) => ({ attributeDefinitionId, value, display: null }))],
            infoDefinitions: current.infoDefinitions.filter(d => !snapshot.removed.includes(d.id)) };
          // Preserve definitions added while the request was in flight.
          updated.infoDefinitions = (profileRef.current?.infoDefinitions ?? updated.infoDefinitions).filter(d => !snapshot.removed.includes(d.id) || d.id in draftRef.current.values);
          profileRef.current = updated;
          setProfile(updated);
          const pending = draftRef.current;
          const values = Object.fromEntries(Object.entries(pending.values).filter(([key, value]) =>
            JSON.stringify(value) !== JSON.stringify(snapshot.values[key])));
          const next = { values, removed: pending.removed.filter(key => !snapshot.removed.includes(key)) };
          draftRef.current = next;
          setDraft(next);
          setStatus(Object.keys(values).length || next.removed.length ? 'pending' : 'saved');
        })
        .catch((e: Error) => {
          blocked.current = e instanceof ApiError && e.status === 409;
          setStatus(blocked.current ? 'conflict' : 'error');
          setError(e.message);
        })
        .finally(() => { busy.current = false; });
    }, 7000);
    return () => window.clearInterval(timer);
  }, []);

  useEffect(() => {
    const warn = (event: BeforeUnloadEvent) => {
      if (Object.keys(draftRef.current.values).length || draftRef.current.removed.length) { event.preventDefault(); event.returnValue = ''; }
    };
    window.addEventListener('beforeunload', warn);
    return () => window.removeEventListener('beforeunload', warn);
  }, []);

  const setValue = (attributeDefinitionId: string, value: AttributeValueInput) =>
    changeDraft({ values: { ...draftRef.current.values, [attributeDefinitionId]: value }, removed: draftRef.current.removed.filter(x => x !== attributeDefinitionId) });
  const addAttribute = () => {
    const definition = library.find(x => x.id === attributeId);
    if (!definition || !profileRef.current) return;
    const next = { ...profileRef.current, infoDefinitions: [...profileRef.current.infoDefinitions.filter(x => x.id !== definition.id), definition] };
    profileRef.current = next; setProfile(next);
    setValue(definition.id, { ...emptyAttributeValue });
    setAttributeId('');
  };
  const removeAttributes = () => {
    const values = { ...draftRef.current.values };
    selectedInfo.forEach(key => { delete values[key]; });
    changeDraft({ values, removed: [...new Set([...draftRef.current.removed, ...selectedInfo])] });
    setSelectedInfo([]);
  };
  const runProjectAction = async (action: () => Promise<unknown>) => {
    try {
      await action();
      const fresh = id ? await profilesApi.editor(id) : await profilesApi.me();
      if (profileRef.current) {
        const next = { ...profileRef.current, projects: fresh.projects };
        profileRef.current = next; setProfile(next);
      }
      setShowProject(false); setSelected([]); setError('');
    } catch (e) { setError((e as Error).message); }
  };
  const openProject = (project?: Project) => {
    setProjectId(project?.id ?? null);
    setProjectForm(project ? { name: project.name, periodStart: project.periodStart.slice(0, 10), periodEnd: project.periodEnd?.slice(0, 10) ?? '', description: project.description ?? '', tags: project.tags } : emptyProject);
    setShowProject(true);
  };
  if (!profile) return <Alert variant={error ? 'danger' : 'info'}>{error || t('common.loading')}</Alert>;
  const currentValue = (key: string) => draft.values[key] ?? profile.values.find(x => x.attributeDefinitionId === key)?.value ?? emptyAttributeValue;
  const renderEditor = (definition: AttributeDefinition) => <AttributeValueEditor dataType={definition.dataType} options={definition.options} value={currentValue(definition.id)} onChange={value => setValue(definition.id, value)} />;
  const projectColumns: Column<Project>[] = [
    { key: 'name', header: t('common.name'), render: x => x.name },
    { key: 'period', header: t('profile.periodStart'), render: x => x.periodStart.slice(0, 10) + ' — ' + (x.periodEnd?.slice(0, 10) ?? '') },
    { key: 'description', header: t('common.description'), render: x => <Markdown>{x.description ?? ''}</Markdown> },
    { key: 'tags', header: t('profile.tags'), render: x => x.tags.join(', ') },
  ];
  return <Card><Card.Body>
    <Card.Title>{t('profile.title')}</Card.Title>
    <p role="status">{t('profile.autosave.' + status)}</p>
    {error && <Alert variant="danger">{error}</Alert>}
    {status === 'conflict' && <Alert variant="warning">{t('profile.conflictNotice')} <Button disabled={busy.current} onClick={() => { if (window.confirm(t('profile.discardNotice'))) void load().catch((e: Error) => setError(e.message)); }}>{t('common.reload')}</Button></Alert>}
    <Tabs defaultActiveKey="me" className="mb-3">
      <Tab eventKey="me" title={t('profile.me')}><div className="d-grid gap-3">{profile.builtInDefinitions.map(d => <div key={d.id}><Form.Label>{d.name}</Form.Label>{renderEditor(d)}</div>)}</div></Tab>
      <Tab eventKey="info" title={t('profile.info')}>
        <div className="d-flex flex-wrap gap-2 mb-3">
          <Form.Control style={{ maxWidth: 240 }} aria-label={t('attributes.prefix')} placeholder={t('attributes.prefix')} value={prefix} onChange={e => setPrefix(e.target.value)} />
          <Form.Select style={{ maxWidth: 240 }} aria-label={t('attributes.category')} value={category} onChange={e => setCategory(e.target.value)}><option value="">{t('attributes.category')}</option>{categories.map(x => <option key={x.id} value={x.id}>{x.name}</option>)}</Form.Select>
          <Form.Select style={{ maxWidth: 300 }} aria-label={t('attributes.title')} value={attributeId} onChange={e => setAttributeId(e.target.value)}><option value="">{t('attributes.title')}</option>{library.filter(x => !x.isBuiltIn && (!profile.infoDefinitions.some(d => d.id === x.id) || draft.removed.includes(x.id))).map(x => <option key={x.id} value={x.id}>{x.name}</option>)}</Form.Select>
          <Button disabled={!attributeId} onClick={addAttribute}>{t('positions.addAttribute')}</Button>
        </div>
        <DataTable columns={[{ key: 'name', header: t('common.name'), render: d => d.name }, { key: 'value', header: t('common.value'), render: renderEditor }]} rows={profile.infoDefinitions.filter(d => !draft.removed.includes(d.id))} rowKey={d => d.id} selectedIds={selectedInfo} onSelectionChange={setSelectedInfo} emptyText={t('common.empty')} toolbar={<Button variant="outline-danger" disabled={!selectedInfo.length} onClick={removeAttributes}>{t('common.remove')}</Button>} />
      </Tab>
      <Tab eventKey="projects" title={t('profile.projects')}><DataTable columns={projectColumns} rows={profile.projects} rowKey={x => x.id} selectedIds={selected} onSelectionChange={setSelected} emptyText={t('common.empty')} toolbar={<div className="d-flex gap-2">
        <Button onClick={() => openProject()}>{t('profile.addProject')}</Button>
        <Button disabled={selected.length !== 1} onClick={() => openProject(profile.projects.find(x => x.id === selected[0]))}>{t('common.edit')}</Button>
        <Button variant="outline-danger" disabled={!selected.length} onClick={() => void runProjectAction(() => profilesApi.removeProjects(profile.id, selected))}>{t('common.remove')}</Button>
      </div>} /></Tab>
      <Tab eventKey="cvs" title={t('profile.cvs')}><table className="table"><thead><tr><th>{t('nav.positions')}</th><th>{t('cvs.status')}</th><th>{t('positions.likes')}</th></tr></thead><tbody>{profile.cvs.map(x => <tr key={x.id}><td><Link to={'/cvs/' + x.id}>{x.positionTitle}</Link></td><td>{t(x.status === 'Published' ? 'cvs.published' : 'cvs.draft')}</td><td>{x.likeCount}</td></tr>)}</tbody></table></Tab>
    </Tabs>
  </Card.Body>
    <Modal show={showProject} onHide={() => setShowProject(false)} centered><Modal.Header closeButton><Modal.Title>{t(projectId ? 'common.edit' : 'profile.addProject')}</Modal.Title></Modal.Header><Modal.Body className="d-grid gap-2">
      <Form.Label>{t('common.name')}<Form.Control value={projectForm.name} onChange={e => setProjectForm({ ...projectForm, name: e.target.value })} /></Form.Label>
      <Form.Label>{t('profile.periodStart')}<Form.Control type="date" value={projectForm.periodStart} onChange={e => setProjectForm({ ...projectForm, periodStart: e.target.value })} /></Form.Label>
      <Form.Label>{t('profile.periodEnd')}<Form.Control type="date" min={projectForm.periodStart} value={projectForm.periodEnd} onChange={e => setProjectForm({ ...projectForm, periodEnd: e.target.value })} /></Form.Label>
      <Form.Label>{t('common.description')}<Form.Control as="textarea" rows={4} value={projectForm.description} onChange={e => setProjectForm({ ...projectForm, description: e.target.value })} /></Form.Label>
      <Markdown>{projectForm.description}</Markdown>
      <CreatableSelect isMulti aria-label={t('profile.tags')} placeholder={t('profile.tags')} options={tagOptions.map(value => ({ value, label: value }))} value={projectForm.tags.map(value => ({ value, label: value }))} onChange={items => setProjectForm({ ...projectForm, tags: items.map(x => x.value) })} />
    </Modal.Body><Modal.Footer><Button variant="secondary" onClick={() => setShowProject(false)}>{t('common.cancel')}</Button><Button disabled={!projectForm.name.trim() || !projectForm.periodStart || (!!projectForm.periodEnd && projectForm.periodEnd < projectForm.periodStart)} onClick={() => void runProjectAction(() => {
      const body = { ...projectForm, periodStart: new Date(projectForm.periodStart).toISOString(), periodEnd: projectForm.periodEnd ? new Date(projectForm.periodEnd).toISOString() : null };
      return projectId ? profilesApi.updateProject(profile.id, projectId, body) : profilesApi.addProject(profile.id, body);
    })}>{t('common.save')}</Button></Modal.Footer></Modal>
  </Card>;
}
