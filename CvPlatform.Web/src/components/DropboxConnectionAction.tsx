import { useEffect, useState } from 'react';
import { Alert, Button } from 'react-bootstrap';
import { useSearchParams } from 'react-router-dom';
import { apiClient } from '../api/client';
import { useText } from '../hooks/useText';

export function DropboxConnectionAction() {
  const text = useText();
  const [query] = useSearchParams();
  const [status, setStatus] = useState<{ ready: boolean; connected: boolean; message?: string } | null>(null);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState('');
  useEffect(() => { void apiClient.get('/api/support/dropbox/status').then(x => setStatus(x.data)).catch(e => setError(e.message)); }, []);
  const connect = async () => {
    setBusy(true); setError('');
    try { const result = await apiClient.post<{ url: string }>('/api/support/dropbox/connect'); window.location.assign(result.data.url); }
    catch (e) { setError((e as Error).message); setBusy(false); }
  };
  const result = query.get('dropbox');
  return <section className="support-context mb-4" aria-label={text('Support storage', 'Хранилище обращений')}>
    <strong>{text('Support tickets in Dropbox', 'Обращения в Dropbox')}</strong>
    <p className="mb-1">{status?.connected ? text('Dropbox connected. Tickets will be uploaded to the app folder.', 'Dropbox подключён. Обращения будут загружаться в папку приложения.') : text('Connect the Dropbox account used by your Power Automate flow. The support folder is created automatically.', 'Подключите Dropbox, который используется в Power Automate. Папка для обращений создастся автоматически.')}</p>
    {error && <Alert variant="danger">{error}</Alert>}
    {status?.message && <Alert variant="warning">{status.message}</Alert>}
    {result && result !== 'connected' && <Alert variant="danger">{text('Dropbox was not connected.', 'Dropbox не подключён.')} {result === 'folder-failed' ? text('Check files.content.write permission and the support folder.', 'Проверьте разрешение files.content.write и папку поддержки.') : text('Check the Render keys and Dropbox Redirect URI, then try again.', 'Проверьте ключи Render и Redirect URI в Dropbox, затем повторите подключение.')}</Alert>}
    {result === 'connected' && status?.connected && <Alert variant="success">{text('Connection saved. No manual token exchange is needed.', 'Подключение сохранено. Обменивать код вручную не нужно.')}</Alert>}
    {status && !status.ready && <Alert variant="info">{text('Configure Dropbox App key, App secret, Public origin and Encryption key in Render first.', 'Сначала добавьте App key, App secret, Public origin и Encryption key в Render.')}</Alert>}
    <div><Button disabled={!status?.ready || busy} onClick={() => void connect()}>{busy ? text('Opening Dropbox…', 'Открываем Dropbox…') : status?.connected ? text('Reconnect Dropbox', 'Переподключить Dropbox') : text('Connect Dropbox', 'Подключить Dropbox')}</Button></div>
  </section>;
}
