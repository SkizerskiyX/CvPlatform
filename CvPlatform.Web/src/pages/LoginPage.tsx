import { useState } from 'react';
import { Button, Card, Form } from 'react-bootstrap';
import { useNavigate, useSearchParams } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { ExternalLoginButtons } from '../components/ExternalLoginButtons';
import { useAuth } from '../hooks/useAuth';

export function LoginPage() {
  const { t } = useTranslation();
  const { signIn } = useAuth();
  const navigate = useNavigate();
  const [searchParams] = useSearchParams();
  const oauthError = searchParams.get('error');
  const oauthErrorKey = ['external', 'cancelled', 'unavailable', 'missing_email', 'account_exists', 'locked'].includes(oauthError ?? '')
    ? oauthError : 'external';
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [error, setError] = useState('');
  const [busy, setBusy] = useState(false);

  const submit = async (event: React.FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    if (busy) return;
    setBusy(true);
    try {
      await signIn(email, password);
      navigate('/profile');
    } catch (requestError) {
      setError((requestError as Error).message);
    } finally {
      setBusy(false);
    }
  };

  return (
    <Card className="auth-card">
      <Card.Body>
        <Card.Title>{t('auth.signIn')}</Card.Title>
        {error && <p className="text-danger" role="alert">{error}</p>}
        {!error && oauthError && <p className="text-danger" role="alert">{t(`auth.externalErrors.${oauthErrorKey}`)}</p>}
        <Form onSubmit={submit} className="d-grid gap-3">
          <Form.Control
            type="email"
            aria-label={t('auth.email')}
            autoComplete="email"
            required
            placeholder={t('auth.email')}
            value={email}
            onChange={(event) => setEmail(event.target.value)}
          />
          <Form.Control
            type="password"
            aria-label={t('auth.password')}
            autoComplete="current-password"
            required
            minLength={8}
            placeholder={t('auth.password')}
            value={password}
            onChange={(event) => setPassword(event.target.value)}
          />
          <Button type="submit" disabled={busy}>{busy ? t('common.loading') : t('auth.signIn')}</Button>
        </Form>
        <ExternalLoginButtons />
      </Card.Body>
    </Card>
  );
}
