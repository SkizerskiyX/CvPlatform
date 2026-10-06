import { useEffect, useState } from 'react';
import { Alert, Button, Form, Modal } from 'react-bootstrap';
import { useTranslation } from 'react-i18next';
import { useLocation } from 'react-router-dom';
import { apiClient } from '../api/client';
import { useAuth } from '../hooks/useAuth';

export function SupportTicketDialog({ show, onHide }: { show: boolean; onHide: () => void }) {
  const { i18n } = useTranslation();
  const { isAuthenticated } = useAuth();
  const { pathname } = useLocation();
  const ru = i18n.language.startsWith('ru');
  const text = (en: string, russian: string) => ru ? russian : en;
  const [page, setPage] = useState('');
  const [id, setId] = useState('');
  const [summary, setSummary] = useState('');
  const [priority, setPriority] = useState('Average');
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState('');
  const [sent, setSent] = useState(false);
  useEffect(() => {
    if (show) {
      setPage(pathname); setId(crypto.randomUUID()); setSummary('');
      setPriority('Average'); setError(''); setSent(false);
    }
  }, [show]);
  const send = async (event: React.FormEvent) => {
    event.preventDefault();
    if (busy || !summary.trim() || !isAuthenticated) return;
    setBusy(true); setError('');
    try {
      await apiClient.post('/api/support/tickets', { ticketId: id, summary: summary.trim(), priority, pagePath: page });
      setSent(true);
    } catch (e) {
      const message = (e as Error).message;
      setError(message.includes('429') ? text('Wait a minute before sending another ticket.', 'Подождите минуту перед следующей отправкой.') : message);
    } finally { setBusy(false); }
  };
  return <Modal show={show} onHide={() => { if (!busy) onHide(); }} centered backdrop={busy ? 'static' : true} keyboard={!busy}>
    <Modal.Header closeButton={!busy}><Modal.Title>{text('Contact support', 'Обратиться в поддержку')}</Modal.Title></Modal.Header>
    <Form onSubmit={event => void send(event)}>
      <Modal.Body className="d-grid gap-3">
        {!isAuthenticated ? <Alert variant="info">{text('Sign in to create a support ticket.', 'Войдите в аккаунт, чтобы создать обращение.')}</Alert> : sent ? <Alert variant="success" role="status">
          {text('Your ticket was uploaded. Notifications are handled by the support workflow.', 'Обращение загружено. Уведомления отправляет настроенный процесс поддержки.')}
          <small className="d-block mt-2">{id}</small>
        </Alert> : <>
          <p className="mb-0 text-body-secondary">{text('Tell us what happened. We include your account, role and this page so the team can investigate.', 'Расскажите, что произошло. К обращению добавим ваш аккаунт, роль и страницу, чтобы команда разобралась.')}</p>
          <div className="support-context"><span>{text('Page', 'Страница')}</span><strong>{page}</strong></div>
          {error && <Alert variant="danger" role="alert">{error}</Alert>}
          <Form.Group controlId="support-summary"><Form.Label>{text('What needs attention?', 'Что нужно исправить?')}</Form.Label>
            <Form.Control as="textarea" rows={4} autoFocus required maxLength={2000} value={summary} disabled={busy} onChange={e => setSummary(e.target.value)} />
          </Form.Group>
          <Form.Group controlId="support-priority"><Form.Label>{text('Priority', 'Приоритет')}</Form.Label>
            <Form.Select value={priority} disabled={busy} onChange={e => setPriority(e.target.value)}>
              <option value="Low">{text('Low', 'Низкий')}</option><option value="Average">{text('Average', 'Средний')}</option><option value="High">{text('High', 'Высокий')}</option>
            </Form.Select>
          </Form.Group>
        </>}
      </Modal.Body>
      <Modal.Footer><Button variant="outline-secondary" disabled={busy} onClick={onHide}>{text('Close', 'Закрыть')}</Button>
        {isAuthenticated && !sent && <Button type="submit" disabled={busy || !summary.trim()}>{busy ? text('Uploading…', 'Отправляем…') : text('Send ticket', 'Отправить обращение')}</Button>}
      </Modal.Footer>
    </Form>
  </Modal>;
}
