import { useEffect, useState } from 'react';
import { Button, Card, Form } from 'react-bootstrap';
import { useNavigate } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { useAuth } from '../hooks/useAuth';

export function RegisterPage() {
  const { t } = useTranslation();
  const { signUp } = useAuth();
  const navigate = useNavigate();
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [error, setError] = useState('');
  const passwordIsValid = /(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^A-Za-z0-9]).{8,}/.test(password);

  const submit = async (event: React.FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    if (!passwordIsValid) {
      setError(t('auth.passwordRule'));
      return;
    }
    try {
      await signUp(email, password);
      navigate('/profile');
    } catch (requestError) {
      setError((requestError as Error).message);
    }
  };

  return (
    <Card className="auth-card">
      <Card.Body>
        <Card.Title>{t('auth.register')}</Card.Title>
        {error && <p className="text-danger" role="alert">{error}</p>}
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
            autoComplete="new-password"
            isInvalid={password.length > 0 && !passwordIsValid}
            placeholder={t('auth.password')}
            value={password}
            onChange={(event) => setPassword(event.target.value)}
          />
          <Form.Text className={password.length > 0 && !passwordIsValid ? 'text-danger' : 'text-body-secondary'}>
            {t('auth.passwordRule')}
          </Form.Text>
          <Button type="submit" disabled={!email || !passwordIsValid}>{t('auth.register')}</Button>
        </Form>
      </Card.Body>
    </Card>
  );
}

export function OAuthCallbackPage() {
  const { applyToken } = useAuth();
  const navigate = useNavigate();
  const [error, setError] = useState('');

  useEffect(() => {
    const token = window.location.hash.replace('#token=', '');
    if (!token) {
      setError('Missing token');
      return;
    }

    applyToken(decodeURIComponent(token))
      .then(() => navigate('/profile'))
      .catch((requestError: Error) => setError(requestError.message));
  }, [applyToken, navigate]);

  return (
    <Card className="auth-card">
      <Card.Body>
        {error ? <p className="text-danger">{error}</p> : <p className="text-body-secondary">Loading...</p>}
      </Card.Body>
    </Card>
  );
}
