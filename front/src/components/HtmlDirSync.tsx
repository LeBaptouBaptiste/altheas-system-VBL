'use client';

import { useEffect } from 'react';
import { useI18n } from '@/context/i18n-context';

export function HtmlDirSync() {
  const { locale, dir } = useI18n();

  useEffect(() => {
    document.documentElement.setAttribute('dir', dir);
    document.documentElement.setAttribute('lang', locale);
  }, [locale, dir]);

  return null;
}
