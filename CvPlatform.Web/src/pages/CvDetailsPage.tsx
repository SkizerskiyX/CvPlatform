import { useCallback, useEffect, useState } from 'react';
import { Badge, Button, Card } from 'react-bootstrap';
import Markdown from 'react-markdown';
import { Link, useParams } from 'react-router-dom';
import { cvsApi, likesApi } from '../api/endpoints';
import type { AttributeValueInput, CvDocument } from '../api/types';
import { AttributeValueEditor } from '../components/AttributeValueEditor';
export function CvDetailsPage() {
  const {id=''}=useParams(); const [cv,setCv]=useState<CvDocument|null>(null); const [error,setError]=useState('');
  const load=useCallback(()=>cvsApi.get(id).then(setCv).catch((e:Error)=>setError(e.message)),[id]); useEffect(()=>{void load();},[load]);
  const save=async(attributeDefinitionId:string,value:AttributeValueInput)=>{if(!cv)return;await cvsApi.setAttribute(id,cv.profileVersion,attributeDefinitionId,value);await load();};
  if(!cv)return <Card><Card.Body>{error||'Loading…'}</Card.Body></Card>;
  const fields=[...cv.header,...cv.sections.flatMap((x)=>x.fields)];
  return <div className="d-grid gap-3"><Card><Card.Body><div className="d-flex justify-content-between"><div><Card.Title>{cv.positionTitle}</Card.Title><p>{cv.candidateName}</p></div><Badge bg={cv.status==='Published'?'success':'secondary'}>{cv.status}</Badge></div>{error&&<p className="text-danger">{error}</p>}<div className="d-flex gap-2"><Link className="btn btn-outline-secondary btn-sm" to="/cvs">CVs</Link>{cv.canEdit&&<Button size="sm" disabled={cv.missingCount>0} onClick={async()=>{await cvsApi.publish(id);await load();}}>Publish</Button>}{cv.canEdit&&<Button size="sm" variant="outline-secondary" onClick={async()=>{await cvsApi.unpublish(id);await load();}}>Unpublish</Button>}{cv.canLike&&<Button size="sm" variant="outline-primary" onClick={async()=>{await likesApi.toggle(id);await load();}}>{cv.likedByMe?'Unlike':'Like'} · {cv.likeCount}</Button>}</div>{cv.missingCount>0&&<p className="text-danger mt-2">Missing required values: {cv.missingCount}</p>}</Card.Body></Card><Card><Card.Body><Card.Title>Professional profile</Card.Title><table className="table table-sm align-middle"><tbody>{fields.map((field)=><tr key={field.definition.id} className={!field.value.display?'table-danger':''}><th>{field.definition.name}</th><td>{field.definition.dataType==='Text'?<Markdown>{field.value.display??''}</Markdown>:(field.value.display??'Missing')}</td><td>{cv.canEdit&&<AttributeValueEditor dataType={field.definition.dataType} options={field.definition.options} value={field.value.value} onChange={(v)=>void save(field.definition.id,v)} />}</td></tr>)}</tbody></table></Card.Body></Card><Card><Card.Body><Card.Title>Projects</Card.Title>{cv.projects.map((p)=><article key={p.id} className="mb-3"><h6>{p.name}</h6><Markdown>{p.description??''}</Markdown><small>{p.tags.join(', ')}</small></article>)}</Card.Body></Card></div>;
}
