import { useTranslation } from 'react-i18next';
export function useText() {
  const { i18n } = useTranslation();
  return (en: string, ru: string) => i18n.language.startsWith('ru') ? ru : en;
}
