import type { StripeError } from '@stripe/stripe-js';

type TranslateFn = (key: string) => string;

/**
 * Maps a Stripe error to a user-facing localized message.
 *
 * Why this exists: Stripe Elements localizes `error.message` to the user's
 * language for FR, EN, ES, DE, IT, NL, JA, ZH (we set this via the Elements
 * `locale` prop). For MS and AR — which we ship — Stripe falls back to EN,
 * which is jarring. We keep our own dictionary keyed by `error.code` (or
 * `decline_code` when `code === 'card_declined'`) and fall back to Stripe's
 * own message when we don't have a translation.
 *
 * Lookup order:
 *   1. payment.stripe_error.<decline_code> — most specific (insufficient_funds, lost_card, …)
 *   2. payment.stripe_error.<code>         — general (expired_card, incorrect_cvc, …)
 *   3. error.message                        — Stripe's own localized text
 *   4. common.error                         — generic fallback
 */
export function getStripeErrorMessage(err: StripeError, t: TranslateFn): string {
  const tryKey = (k: string | undefined): string | null => {
    if (!k) return null;
    const key = `payment.stripe_error.${k}`;
    const val = t(key);
    // Our t() returns the key itself when the translation is missing,
    // so we use that as the "not found" signal.
    return val !== key ? val : null;
  };

  // `decline_code` is set only when `code === 'card_declined'` and gives
  // the bank's specific reason. Always preferred over the generic 'card_declined'.
  const declineMsg = err.code === 'card_declined' ? tryKey(err.decline_code) : null;
  if (declineMsg) return declineMsg;

  const codeMsg = tryKey(err.code);
  if (codeMsg) return codeMsg;

  if (err.message) return err.message;
  return t('common.error');
}
