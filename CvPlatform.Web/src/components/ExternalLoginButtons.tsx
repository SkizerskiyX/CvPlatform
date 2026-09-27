import { useEffect, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { apiBaseUrl, apiClient } from '../api/client';

export function ExternalLoginButtons() {
  const { t } = useTranslation();
  const [providers, setProviders] = useState<string[]>([]);

  useEffect(() => {
    const controller = new AbortController();
    apiClient.get<string[]>('/api/account/external-providers', { signal: controller.signal })
      .then(({ data }) => setProviders(data.filter(x => x === 'Google' || x === 'Facebook')))
      .catch(() => undefined);
    return () => controller.abort();
  }, []);

  if (!providers.length) return null;
  return (
    <div className="d-flex flex-wrap gap-2 mt-3">
      {providers.map(provider => (
        <a key={provider} className="btn btn-outline-secondary btn-sm"
          href={`${apiBaseUrl}/api/account/external-login?provider=${encodeURIComponent(provider)}`}>
          {t('auth.emailProvider', { provider })}
        </a>
      ))}
    </div>
  );
}
