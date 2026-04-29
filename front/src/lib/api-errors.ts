import { ApiError, AccountLockedError, isAccountLocked } from '@/lib/api';

type Translator = (key: string) => string;

/**
 * Maps an exception thrown by the api wrapper to a localized message,
 * preferring machine-readable signals (reason / typed errors) over the
 * English text that comes back from the backend.
 *
 * Order of precedence:
 *   1. AccountLockedError -> errors.account_locked (with {minutes} placeholder)
 *   2. ApiError with a known `reason` -> errors.<reason>
 *   3. ApiError with a known status code -> errors.<status>
 *   4. err.message if any
 *   5. common.error
 */
export function getErrorMessage(err: unknown, t: Translator): string {
  if (isAccountLocked(err)) {
    const minutes = Math.max(1, Math.ceil((err as AccountLockedError).retryAfterSeconds / 60));
    return formatTemplate(t('errors.account_locked'), { minutes });
  }

  if (err instanceof ApiError) {
    if (err.reason) {
      const key = `errors.${err.reason}`;
      const translated = t(key);
      // i18n.t() returns the key itself when the key isn't in the dict;
      // fall through to the next strategy in that case.
      if (translated !== key) return translated;
    }

    const statusKey = `errors.status_${err.statusCode}`;
    const statusTranslated = t(statusKey);
    if (statusTranslated !== statusKey) return statusTranslated;

    if (err.message) return err.message;
  }

  if (err instanceof Error && err.message) return err.message;

  return t('common.error');
}

/** Tiny `{var}` interpolator for translation strings with placeholders. */
function formatTemplate(template: string, vars: Record<string, string | number>): string {
  return Object.entries(vars).reduce(
    (s, [k, v]) => s.replaceAll(`{${k}}`, String(v)),
    template,
  );
}
