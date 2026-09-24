import { useCallback, useEffect, useState } from 'react';
import { Button, Card } from 'react-bootstrap';
import { Link, useNavigate } from 'react-router-dom';
import { cvsApi } from '../api/endpoints';
import type { Cv } from '../api/types';
import { DataTable } from '../components/DataTable'; import type { Column } from '../components/DataTable';
export function CvsPage() {
  const navigate = useNavigate(); const [rows, setRows] = useState<Cv[]>([]); const [selected, setSelected] = useState<string[]>([]); const [error, setError] = useState('');
  const load = useCallback(() => cvsApi.mine().then(setRows).catch((e: Error) => setError(e.message)), []); useEffect(() => { void load(); }, [load]);
  const columns: Column<Cv>[] = [{ key:'position', header:'Position', render:(x)=><Link to={`/cvs/${x.id}`}>{x.positionTitle}</Link> }, {key:'candidate',header:'Candidate',render:(x)=>x.candidateName}, {key:'status',header:'Status',render:(x)=>x.status}, {key:'likes',header:'Likes',render:(x)=>x.likeCount}, {key:'updated',header:'Updated',render:(x)=>new Date(x.updatedAt).toLocaleDateString()}];
  return <Card><Card.Body><Card.Title>CVs</Card.Title>{error && <p className="text-danger">{error}</p>}<DataTable columns={columns} rows={rows} rowKey={(x)=>x.id} selectedIds={selected} onSelectionChange={setSelected} onRowClick={(x)=>navigate(`/cvs/${x.id}`)} emptyText="Nothing to display." toolbar={<div className="d-flex gap-2"><Button size="sm" variant="outline-success" disabled={!selected.length} onClick={async()=>{for(const id of selected) await cvsApi.publish(id); await load();}}>Publish</Button><Button size="sm" variant="outline-secondary" disabled={!selected.length} onClick={async()=>{for(const id of selected) await cvsApi.unpublish(id); await load();}}>Unpublish</Button><Button size="sm" variant="outline-danger" disabled={!selected.length} onClick={async()=>{await cvsApi.remove(selected);setSelected([]);await load();}}>Delete</Button></div>} /></Card.Body></Card>;
}
