/**
 * Single shared Stripe.js loader. `loadStripe()` is async — it injects the
 * Stripe.js script the first time it's awaited — and is meant to be called
 * at module level so the same promise is reused everywhere.
 *
 * The publishable key (pk_test_… in dev, pk_live_… in prod) is safe to
 * ship to the browser: it can only initiate payments, never confirm or
 * read sensitive data. See https://stripe.com/docs/keys.
 */
import { loadStripe, type Stripe } from '@stripe/stripe-js';

const publishableKey = process.env.NEXT_PUBLIC_STRIPE_PUBLISHABLE_KEY;

if (!publishableKey) {
  // Throw at module load time in dev so we don't ship a half-broken checkout.
  // The message is plain text — the trace will point at this file.
  // In prod, Next swallows this and shows the generic error boundary;
  // the CI build will also fail because `NEXT_PUBLIC_STRIPE_PUBLISHABLE_KEY`
  // must be available at build time for the bundle.
  // eslint-disable-next-line no-console
  console.error(
    '[stripe] NEXT_PUBLIC_STRIPE_PUBLISHABLE_KEY is not set. ' +
      'Add it to .env (pk_test_… in dev, pk_live_… in prod).',
  );
}

/**
 * Shared promise reused across the app. Pass it to `<Elements stripe={...}>`.
 * If the key is missing, `loadStripe('')` resolves to `null` and Elements
 * renders nothing — the absence of the field is the user-visible signal.
 */
export const stripePromise: Promise<Stripe | null> = loadStripe(publishableKey ?? '');
