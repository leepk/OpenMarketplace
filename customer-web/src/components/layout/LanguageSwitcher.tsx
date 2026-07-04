'use client';

import { useI18n, type Lang } from '@/lib/i18n/client';

const languageOptions: Array<{ value: Lang; label: string; short: string }> = [
  { value: 'en', label: 'English', short: 'EN' },
  { value: 'vi', label: 'Tiếng Việt', short: 'VI' },
];

export function LanguageSwitcher() {
  const { lang, setLang, t } = useI18n();

  return (
    <label className="language-select-wrap" title={t('langLabel')} aria-label={t('langLabel')}>
      <span className="language-select-icon">🌐</span>
      <select
        className="language-select"
        value={lang}
        onChange={(event) => setLang(event.target.value as Lang)}
      >
        {languageOptions.map((option) => (
          <option key={option.value} value={option.value}>
            {option.short} · {option.label}
          </option>
        ))}
      </select>
    </label>
  );
}
