import { useEffect, useState } from 'react';
import { Alert, Button, Form, Modal, Spinner } from 'react-bootstrap';
import { apiClient } from '../api/client';
import { useText } from '../hooks/useText';

type CrmProfile = { firstName: string; lastName: string; email: string; location: string | null; connected: boolean; salesforceConnectedAt: string | null };

export function SalesforceProfileAction({ profileId, disabled = false, onConnected }: { profileId: string; disabled?: boolean; onConnected?: () => Promise<void> }) {
  const text = useText();
  const [profile, setProfile] = useState<CrmProfile | null>(null);
  const [show, setShow] = useState(false);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState('');
  const [organization, setOrganization] = useState('');
  const [personal, setPersonal] = useState(false);
  const [phone, setPhone] = useState('');
  const [notes, setNotes] = useState('');
  const [consent, setConsent] = useState(false);
  const [sent, setSent] = useState(false);

  useEffect(() => {
    let active = true;
    setProfile(null);
    setError('');
    void apiClient.get<CrmProfile>('/api/Salesforce/profiles/' + profileId)
      .then(result => { if (active) setProfile(result.data); })
      .catch((e: Error) => { if (active) setError(e.message); });
    return () => { active = false; };
  }, [profileId, show]);

  const send = async (event: React.FormEvent) => {
    event.preventDefault();
    if (!profile || busy) return;
    setBusy(true);
    setError('');
    try {
      await apiClient.post('/api/Salesforce/profiles/' + profileId, {
        organization: personal ? `Personal — ${profile.firstName} ${profile.lastName}`.trim() : organization.trim(),
        phone: phone.trim() || null, notes: notes.trim() || null, newsletterConsent: consent,
      });
      setProfile({ ...profile, connected: true });
      setSent(true);
      if (onConnected) await onConnected();
    } catch (e) { setError((e as Error).message); }
    finally { setBusy(false); }
  };

  return <>
    <Button variant="outline-primary" disabled={disabled} onClick={() => { setSent(false); setShow(true); }}>
      {text('Connect to CRM', 'Подключить к CRM')}
    </Button>
    <Modal show={show} onHide={() => { if (!busy) setShow(false); }} centered backdrop={busy ? 'static' : true} keyboard={!busy}>
      <Modal.Header closeButton={!busy}><Modal.Title>{text('Connect to CRM', 'Подключить к CRM')}</Modal.Title></Modal.Header>
      <Form onSubmit={event => void send(event)}>
        <Modal.Body className="d-grid gap-3">
          {error && <Alert variant="danger" role="alert">{error}</Alert>}
          {!profile && !error && <Spinner animation="border" role="status"><span className="visually-hidden">{text('Loading', 'Загрузка')}</span></Spinner>}
          {sent && <Alert variant="success" role="status">{text('The account and contact have been saved in Salesforce.', 'Организация и контакт сохранены в Salesforce.')}</Alert>}
          {profile && <>
            {profile.connected && !sent && <Alert variant="info">{text('Submit again to update Salesforce or restore missing records.', 'Отправьте данные повторно, чтобы обновить Salesforce или восстановить отсутствующие записи.')}</Alert>}
            <p className="mb-0 text-body-secondary">{text('Send your saved profile and the details below to our CRM.', 'Передайте сохранённый профиль и сведения ниже в нашу CRM.')}</p>
            <div className="crm-profile-summary">
              <strong>{profile.firstName} {profile.lastName}</strong>
              <span>{profile.email}</span>
              {profile.location && <span>{profile.location}</span>}
            </div>
            {(!profile.lastName.trim() || profile.firstName.length > 40 || profile.lastName.length > 80) && <Alert variant="warning">{text('Save your last name in the profile first. First name must be at most 40 characters and last name at most 80.', 'Сначала сохраните фамилию в профиле. Имя — до 40 символов, фамилия — до 80.')}</Alert>}
            <Form.Check id={'crm-personal-' + profileId} label={text('I am registering as an individual', 'Я регистрируюсь как частное лицо')} checked={personal} disabled={busy} onChange={event => setPersonal(event.target.checked)} />
            {!personal && <Form.Group controlId={'crm-organization-' + profileId}><Form.Label>{text('Organization', 'Организация')}</Form.Label><Form.Control autoFocus required maxLength={255} value={organization} disabled={busy} onChange={event => setOrganization(event.target.value)} /></Form.Group>}
            <Form.Group controlId={'crm-phone-' + profileId}><Form.Label>{text('Phone (optional)', 'Телефон (необязательно)')}</Form.Label><Form.Control type="tel" maxLength={40} value={phone} disabled={busy} onChange={event => setPhone(event.target.value)} /></Form.Group>
            <Form.Group controlId={'crm-notes-' + profileId}><Form.Label>{text('Additional information (optional)', 'Дополнительные сведения (необязательно)')}</Form.Label><Form.Control as="textarea" rows={3} maxLength={2000} value={notes} disabled={busy} onChange={event => setNotes(event.target.value)} /></Form.Group>
            <Form.Check id={'crm-consent-' + profileId} label={text('I agree to receive newsletters', 'Я согласен получать рассылку')} checked={consent} disabled={busy} onChange={event => setConsent(event.target.checked)} />
          </>}
        </Modal.Body>
        <Modal.Footer><Button variant="secondary" disabled={busy} onClick={() => setShow(false)}>{text('Close', 'Закрыть')}</Button>
          {profile && <Button type="submit" disabled={busy || !profile.lastName.trim() || profile.firstName.length > 40 || profile.lastName.length > 80 || (!personal && !organization.trim())}>
            {busy ? text('Sending…', 'Отправка…') : text('Send to CRM', 'Отправить в CRM')}
          </Button>}
        </Modal.Footer>
      </Form>
    </Modal>
  </>;
}
