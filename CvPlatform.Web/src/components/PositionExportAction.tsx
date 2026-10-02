import { useEffect, useState } from 'react';
import { Alert, Button, Form } from 'react-bootstrap';
import { apiBaseUrl, apiClient } from '../api/client';
import { useText } from '../hooks/useText';

type TokenStatus = { active: boolean; generatedAt: string | null };

export function PositionExportAction({ positionId, disabled }: { positionId: string; disabled: boolean }) {
  const text = useText();
  const [status, setStatus] = useState<TokenStatus | null>(null);
  const [token, setToken] = useState('');
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState('');
  const [copied, setCopied] = useState(false);
  const endpoint = new URL('/api/integrations/odoo/position', apiBaseUrl || window.location.origin).href;
  const path = '/api/integrations/odoo/positions/' + positionId + '/token';

  useEffect(() => {
    let active = true;
    setStatus(null); setToken(''); setError(''); setCopied(false);
    void apiClient.get<TokenStatus>(path).then(result => { if (active) setStatus(result.data); })
      .catch((e: Error) => { if (active) setError(e.message); });
    return () => { active = false; };
  }, [path]);

  const generate = async () => {
    if (status?.active && !window.confirm(text('Replace the current token? The previous token will stop working in Odoo.', 'Заменить текущий токен? Старый токен перестанет работать в Odoo.'))) return;
    setBusy(true); setError(''); setCopied(false);
    try {
      const result = await apiClient.post<{ token: string }>(path);
      setToken(result.data.token); setStatus({ active: true, generatedAt: new Date().toISOString() });
    } catch (e) { setError((e as Error).message); }
    finally { setBusy(false); }
  };
  const revoke = async () => {
    if (!window.confirm(text('Revoke access to this position? New imports will require a new token.', 'Отозвать доступ к этой позиции? Для новых импортов потребуется новый токен.'))) return;
    setBusy(true); setError('');
    try { await apiClient.delete(path); setToken(''); setStatus({ active: false, generatedAt: null }); }
    catch (e) { setError((e as Error).message); }
    finally { setBusy(false); }
  };

  return <section className="position-export mt-4" aria-labelledby="position-export-title">
    <h2 id="position-export-title" className="h5">{text('Export to Odoo', 'Экспорт в Odoo')}</h2>
    <p className="text-body-secondary">{text('Import this position and aggregated results from its published CVs into Odoo. The token gives access to this position only.', 'Импортируйте позицию и сводные результаты опубликованных резюме в Odoo. Токен открывает доступ только к этой позиции.')}</p>
    {error && <Alert variant="danger" role="alert">{error}</Alert>}
    {disabled && <p role="status">{text('Save the position before managing its token.', 'Сохраните позицию перед управлением токеном.')}</p>}
    {status && <p role="status">{status.active ? text('An export token is active.', 'Токен экспорта активен.') : text('Export access has not been enabled.', 'Доступ к экспорту не включён.')}</p>}
    <div className="d-flex flex-wrap gap-3 align-items-center">
      <Button variant="link" className="p-0" disabled={disabled || busy || !status} onClick={() => void generate()}>{busy ? text('Please wait…', 'Подождите…') : status?.active ? text('Generate a new API token', 'Создать новый API-токен') : text('Generate API token', 'Сгенерировать API-токен')}</Button>
      {status?.active && <Button variant="outline-danger" size="sm" disabled={disabled || busy} onClick={() => void revoke()}>{text('Revoke token', 'Отозвать токен')}</Button>}
    </div>
    {token && <div className="d-grid gap-3 mt-3">
      <Form.Group controlId="odoo-export-token"><Form.Label>{text('API token', 'API-токен')}</Form.Label><Form.Control readOnly value={token} onFocus={event => event.currentTarget.select()} autoComplete="off" spellCheck={false} /></Form.Group>
      <div><Button variant="outline-primary" size="sm" onClick={() => {
        void navigator.clipboard.writeText(token).then(() => setCopied(true)).catch(() => setError(text('Select the token above and copy it manually.', 'Выделите токен выше и скопируйте его вручную.')));
      }}>{copied ? text('Copied', 'Скопировано') : text('Copy token', 'Скопировать токен')}</Button></div>
      <p className="mb-0">{text('Save the token now. It is shown only after generation. In Odoo, open CV Platform → Import position and paste it there.', 'Сохраните токен сейчас: он отображается только после генерации. В Odoo откройте CV Platform → Import position и вставьте его.')}</p>
      <Form.Group controlId="odoo-export-endpoint"><Form.Label>{text('Export endpoint', 'Адрес экспорта')}</Form.Label><Form.Control readOnly value={endpoint} onFocus={event => event.currentTarget.select()} /></Form.Group>
    </div>}
  </section>;
}
