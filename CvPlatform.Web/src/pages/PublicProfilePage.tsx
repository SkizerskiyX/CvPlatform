import { useEffect, useState } from 'react';
import { Alert, Card } from 'react-bootstrap';
import { Link, useParams } from 'react-router-dom';
import { apiClient } from '../api/client';
import type { Cv } from '../api/types';
import { CvTable } from '../components/CvTable';
import { useAuth } from '../hooks/useAuth';
import { useText } from '../hooks/useText';
import { SalesforceProfileAction } from '../components/SalesforceProfileAction';
type PublicProfile = { id: string; displayName: string; location: string | null; photoUrl: string | null; publishedCvs: Cv[] };
export function PublicProfilePage() {
  const { id } = useParams(); const { isAdmin, profile: me } = useAuth(); const text = useText();
  const [profile, setProfile] = useState<PublicProfile | null>(null); const [error, setError] = useState('');
  useEffect(() => { setProfile(null); void apiClient.get<PublicProfile>('/api/profiles/' + id).then(x => setProfile(x.data)).catch(e => setError(e.message)); }, [id]);
  return <Card><Card.Body>{error && <Alert variant="danger">{error}</Alert>}{profile ? <><Card.Title>{profile.displayName}</Card.Title>{profile.photoUrl && <img className="photo-preview" src={profile.photoUrl} alt={profile.displayName} />}<p>{profile.location}</p>{(isAdmin || me?.profileId === id) && <div className="d-flex flex-wrap gap-3 align-items-center"><Link to={'/profiles/' + id + '/edit'}>{text('Edit profile', 'Изменить профиль')}</Link><SalesforceProfileAction profileId={profile.id} /></div>}<h2 className="h5 mt-3">{text('Published CVs', 'Опубликованные резюме')}</h2><CvTable rows={profile.publishedCvs} /></> : !error && text('Loading…', 'Загрузка…')}</Card.Body></Card>;
}
