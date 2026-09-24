import { useState } from 'react';
import { Button, Card, Form } from 'react-bootstrap';
import { useNavigate } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { apiBaseUrl } from '../api/client';
import { useAuth } from '../hooks/useAuth';

export function LoginPage() {
  const { t } = useTranslation();
  const { signIn } = useAuth();
  const navigate = useNavigate();
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [error, setError] = useState('');

  const submit = async (event: React.FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    try {
      await signIn(email, password);
      navigate('/profile');
    } catch (requestError) {
      setError((requestError as Error).message);
    }
  };

  return (
    <Card className="auth-card">
      <Card.Body>
        <Card.Title>{t('auth.signIn')}</Card.Title>
        {error && <p className="text-danger">{error}</p>}
        <Form onSubmit={submit} className="d-grid gap-3">
          <Form.Control
            type="email"
            required
            placeholder={t('auth.email')}
            value={email}
            onChange={(event) => setEmail(event.target.value)}
          />
          <Form.Control
            type="password"
            required
            minLength={8}
            placeholder={t('auth.password')}
            value={password}
            onChange={(event) => setPassword(event.target.value)}
          />
          <Button type="submit">{t('auth.signIn')}</Button>
        </Form>
        <div className="d-flex gap-2 mt-3">
          <a className="btn btn-outline-secondary btn-sm" href={`${apiBaseUrl}/api/account/external-login?provider=Google&returnUrl=/`}>
            {t('auth.emailProvider', { provider: 'Google' })}
          </a>
          <a className="btn btn-outline-secondary btn-sm" href={`${apiBaseUrl}/api/account/external-login?provider=Facebook&returnUrl=/`}>
            {t('auth.emailProvider', { provider: 'Facebook' })}
          </a>
        </div>
      </Card.Body>
    </Card>
  );
}