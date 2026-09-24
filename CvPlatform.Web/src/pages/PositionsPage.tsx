import { useCallback, useEffect, useMemo, useState } from 'react';
import { Button, Card, Form, Modal } from 'react-bootstrap';
import { Link, useNavigate } from 'react-router-dom';
import { positionsApi } from '../api/endpoints';
import type { PositionListItem } from '../api/types';
import { DataTable } from '../components/DataTable';
import type { Column } from '../components/DataTable';
import { useAuth } from '../hooks/useAuth';

export function PositionsPage() {
  const navigate = useNavigate(); const { roles } = useAuth(); const staff = roles.some((x) => x === 'Recruiter' || x === 'Admin');
  const [rows, setRows] = useState<PositionListItem[]>([]); const [selected, setSelected] = useState<string[]>([]); const [search, setSearch] = useState(''); const [error, setError] = useState(''); const [show, setShow] = useState(false); const [title, setTitle] = useState('');
  const load = useCallback(() => positionsApi.list().then(setRows).catch((e: Error) => setError(e.message)), []);
  useEffect(() => { void load(); }, [load]);
  const visible = useMemo(() => rows.filter((x) => `${x.title} ${x.company ?? ''} ${x.projectTags.join(' ')}`.toLowerCase().includes(search.toLowerCase())), [rows, search]);
  const columns: Column<PositionListItem>[] = [
    { key: 'title', header: 'Position', render: (x) => <Link to={`/positions/${x.id}`}>{x.title}</Link> },
    { key: 'company', header: 'Company', render: (x) => x.company ?? '—' },
    { key: 'level', header: 'Level', render: (x) => x.level ?? '—' },
    { key: 'attributes', header: 'Attributes', render: (x) => x.attributeCount },
    { key: 'cvs', header: 'Published CVs', render: (x) => x.publishedCvCount },
  ];
  const create = async () => { const result = await positionsApi.create({ title, shortDescription: null, company: null, level: null, isPublic: true, maxProjects: 5, projectTags: [], attributeIds: [], accessRules: [] }); navigate(`/positions/${result.id}`); };
  const remove = async () => { await positionsApi.remove(selected); setSelected([]); await load(); };
  return <Card><Card.Body><div className="page-heading"><div><p className="eyebrow">Opportunity library</p><Card.Title>Positions</Card.Title></div>{staff && <Button onClick={() => setShow(true)}>Create</Button>}</div>{error && <p className="text-danger">{error}</p>}<DataTable columns={columns} rows={visible} rowKey={(x) => x.id} selectedIds={selected} onSelectionChange={setSelected} onRowClick={(x) => navigate(`/positions/${x.id}`)} emptyText="Nothing to display." toolbar={<div className="d-flex gap-2 flex-wrap"><Form.Control size="sm" style={{maxWidth: 280}} placeholder="Search positions" value={search} onChange={(e) => setSearch(e.target.value)} />{staff && <Button size="sm" variant="outline-secondary" disabled={selected.length !== 1} onClick={async () => { const x = await positionsApi.duplicate(selected[0]); navigate(`/positions/${x.id}`); }}>Duplicate</Button>}{staff && <Button size="sm" variant="outline-danger" disabled={!selected.length} onClick={() => void remove()}>Delete</Button>}</div>} /></Card.Body><Modal show={show} onHide={() => setShow(false)} centered><Modal.Header closeButton><Modal.Title>Create position</Modal.Title></Modal.Header><Modal.Body><Form.Control value={title} onChange={(e) => setTitle(e.target.value)} placeholder="Title" /></Modal.Body><Modal.Footer><Button variant="secondary" onClick={() => setShow(false)}>Cancel</Button><Button disabled={!title.trim()} onClick={() => void create()}>Create</Button></Modal.Footer></Modal></Card>;
}
