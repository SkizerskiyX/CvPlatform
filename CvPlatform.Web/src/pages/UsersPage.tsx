import { useEffect, useState } from 'react';
import { Alert, Button, Card, Form } from 'react-bootstrap';
import { Link } from 'react-router-dom';
import { apiClient } from '../api/client';
import { DataTable } from '../components/DataTable';
import { useAuth } from '../hooks/useAuth';
import { useText } from '../hooks/useText';
type User = { id: string; profileId: string | null; email: string; displayName: string; roles: string[]; isBlocked: boolean; registeredAt: string };
export function UsersPage() {
  const text = useText(); const { reloadProfile } = useAuth();
  const [rows, setRows] = useState<User[]>([]); const [selected, setSelected] = useState<string[]>([]); const [search, setSearch] = useState(''); const [role, setRole] = useState('Recruiter'); const [error, setError] = useState(''); const [busy, setBusy] = useState(false);
  const load = () => apiClient.get<User[]>('/api/users', { params: { search } }).then(x => setRows(x.data));
  useEffect(() => { const timer = setTimeout(() => { void load().catch(e => setError(e.message)); }, 250); return () => clearTimeout(timer); }, [search]);
  const run = async (action: string) => {
    if (busy || !selected.length) return;
    if ((action === 'delete' || action === 'removeRole') && !confirm(text('Apply to selected users?', 'Применить к выбранным пользователям?'))) return;
    setBusy(true); setError('');
    try {
      if (action === 'delete') await apiClient.delete('/api/users', { data: selected });
      else if (action === 'removeRole') await apiClient.delete('/api/users/roles', { data: { ids: selected, role } });
      else await apiClient.post('/api/users/' + (action === 'addRole' ? 'roles' : action), { ids: selected, role });
      setSelected([]); await reloadProfile(); await load();
    } catch (e) { setError((e as Error).message); } finally { setBusy(false); }
  };
  return <Card><Card.Body><Card.Title>{text('Users', 'Пользователи')}</Card.Title>{error && <Alert variant="danger">{error}</Alert>}<DataTable rows={rows} rowKey={x => x.id} selectedIds={selected} onSelectionChange={setSelected} emptyText={text('No users', 'Нет пользователей')} columns={[
    { key: 'email', header: 'Email', render: x => x.email }, { key: 'name', header: text('Name', 'Имя'), render: x => x.profileId ? <Link to={'/profiles/' + x.profileId + '/edit'}>{x.displayName || x.email}</Link> : x.displayName }, { key: 'roles', header: text('Roles', 'Роли'), render: x => x.roles.map(r => text(r, ({ Admin: 'Администратор', Recruiter: 'Рекрутер', Candidate: 'Кандидат' } as Record<string, string>)[r] ?? r)).join(', ') }, { key: 'status', header: text('Status', 'Статус'), render: x => x.isBlocked ? text('Blocked', 'Заблокирован') : text('Active', 'Активен') }, { key: 'registered', header: text('Registered', 'Регистрация'), render: x => new Date(x.registeredAt).toLocaleDateString() }
  ]} toolbar={<div className="d-flex flex-wrap gap-2"><Form.Control aria-label={text('Search users', 'Поиск пользователей')} placeholder={text('Search users', 'Поиск пользователей')} value={search} onChange={e => setSearch(e.target.value)} /><Form.Select aria-label={text('Role', 'Роль')} value={role} onChange={e => setRole(e.target.value)}>{['Candidate', 'Recruiter', 'Admin'].map(r => <option key={r} value={r}>{text(r, ({ Admin: 'Администратор', Recruiter: 'Рекрутер', Candidate: 'Кандидат' } as Record<string, string>)[r])}</option>)}</Form.Select>{[['block', 'Block', 'Блокировать'], ['unblock', 'Unblock', 'Разблокировать'], ['addRole', 'Add role', 'Добавить роль'], ['removeRole', 'Remove role', 'Убрать роль'], ['delete', 'Delete', 'Удалить']].map(([action, en, ru]) => <Button key={action} disabled={busy || !selected.length} variant={action === 'delete' ? 'outline-danger' : 'outline-secondary'} onClick={() => void run(action)}>{text(en, ru)}</Button>)}</div>} /></Card.Body></Card>;
}
