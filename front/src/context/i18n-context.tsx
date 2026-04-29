'use client';

import { createContext, useContext, useState, useCallback, type ReactNode } from 'react';
import { type Locale, DEFAULT_LOCALE, LOCALES, isRTL, t as translate, localized as getLocalized } from '@/lib/i18n';

interface I18nContextType {
  locale: Locale;
  setLocale: (locale: Locale) => void;
  t: (key: string) => string;
  localized: (obj: Record<string, string> | undefined) => string;
  dir: 'ltr' | 'rtl';
}

const I18nContext = createContext<I18nContextType | null>(null);

export function I18nProvider({ children }: { children: ReactNode }) {
  const [locale, setLocaleState] = useState<Locale>(() => {
    if (typeof window !== 'undefined') {
      const saved = localStorage.getItem('althea-locale');
      if (saved && (LOCALES as string[]).includes(saved)) return saved as Locale;
    }
    return DEFAULT_LOCALE;
  });

  const setLocale = useCallback((newLocale: Locale) => {
    setLocaleState(newLocale);
    if (typeof window !== 'undefined') {
      localStorage.setItem('althea-locale', newLocale);
    }
  }, []);

  const t = useCallback((key: string) => translate(key, locale), [locale]);
  const localized = useCallback((obj: Record<string, string> | undefined) => getLocalized(obj, locale), [locale]);

  const dir: 'ltr' | 'rtl' = isRTL(locale) ? 'rtl' : 'ltr';

  return (
    <I18nContext.Provider value={{ locale, setLocale, t, localized, dir }}>
      {children}
    </I18nContext.Provider>
  );
}

export function useI18n() {
  const ctx = useContext(I18nContext);
  if (!ctx) throw new Error('useI18n must be used within I18nProvider');
  return ctx;
}
