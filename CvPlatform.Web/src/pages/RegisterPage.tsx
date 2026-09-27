import { useEffect, useRef, useState } from 'react';
import { Button, Card, Form } from 'react-bootstrap';
import { useNavigate } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { useAuth } from '../hooks/useAuth';
import { ExternalLoginButtons } from '../components/ExternalLoginButtons';
import { tokenStorageKey } from '../api/client';

export function RegisterPage() {
  const { t } = useTranslation();
  const { signUp } = useAuth();
  const navigate = useNavigate();
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [isRecruiter, setIsRecruiter] = useState(false);
  const [error, setError] = useState('');
  const [busy, setBusy] = useState(false);
  const passwordIsValid = /(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^A-Za-z0-9]).{8,}/.test(password);

  const submit = async (event: React.FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    if (busy) return;
    if (!passwordIsValid) {
      setError(t('auth.passwordRule'));
      return;
    }
    setBusy(true);
    try {
      await signUp(email, password, isRecruiter);
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
        <Card.Title>{t('auth.register')}</Card.Title>
        {error && <p className="text-danger" role="alert">{error}</p>}
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
          <div>
            <Form.Check type="switch" id="register-recruiter" checked={isRecruiter}
              disabled={busy} onChange={event => setIsRecruiter(event.target.checked)}
              aria-describedby="register-recruiter-hint"
              label={<span className="d-inline-flex align-items-center gap-2">
                <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.7" aria-hidden="true">
                  <rect x="3" y="7" width="18" height="14" rx="2" />
                  <path d="M8 7V5a2 2 0 0 1 2-2h4a2 2 0 0 1 2 2v2M3 12a20 20 0 0 0 18 0M12 11v4" />
                </svg>
                {t('auth.isRecruiter')}
              </span>} />
            <Form.Text id="register-recruiter-hint">{t('auth.recruiterHint')}</Form.Text>
          </div>
          <Button type="submit" disabled={busy || !email || !passwordIsValid}>{busy ? t('common.loading') : t('auth.register')}</Button>
        </Form>
        <ExternalLoginButtons />
      </Card.Body>
    </Card>
  );
}

export function OAuthCallbackPage() {
  const { t } = useTranslation();
  const { applyToken } = useAuth();
  const navigate = useNavigate();
  const [error, setError] = useState('');
  const started = useRef(false);

  useEffect(() => {
    if (started.current) return;
    started.current = true;
    const token = new URLSearchParams(window.location.hash.slice(1)).get('token');
    window.history.replaceState(null, '', window.location.pathname + window.location.search);
    if (!token) {
      setError(t('auth.externalErrors.external'));
      return;
    }

    applyToken(token)
      .then(() => navigate('/profile', { replace: true }))
      .catch(() => {
        localStorage.removeItem(tokenStorageKey);
        setError(t('auth.externalErrors.external'));
      });
  }, [applyToken, navigate, t]);

  return (
    <Card className="auth-card">
      <Card.Body>
        {error ? <><p className="text-danger" role="alert">{error}</p><Button onClick={() => navigate('/login', { replace: true })}>{t('auth.signIn')}</Button></> : <p className="text-body-secondary">{t('common.loading')}</p>}
      </Card.Body>
    </Card>
  );
}
