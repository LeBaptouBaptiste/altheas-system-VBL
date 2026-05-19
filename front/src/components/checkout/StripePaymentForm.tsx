'use client';

import { useState } from 'react';
import { Loader2 } from 'lucide-react';
import {
  Elements,
  PaymentElement,
  useElements,
  useStripe,
} from '@stripe/react-stripe-js';
import type { StripeElementsOptionsClientSecret } from '@stripe/stripe-js';
import { Button } from '@/components/ui/button';
import { Checkbox } from '@/components/ui/checkbox';
import { Label } from '@/components/ui/label';
import { stripePromise } from '@/lib/stripe';
import { getStripeErrorMessage } from '@/lib/stripe-errors';
import { useI18n } from '@/context/i18n-context';

interface StripePaymentFormProps {
  /** From POST /api/payments/intents — controls the Elements provider. */
  clientSecret: string;
  /** Where Stripe sends the user after a redirect-based confirmation (3DS, wallet). */
  returnUrl: string;
  /** Initial value of the "save card" checkbox. */
  saveCard: boolean;
  /** Bubbles the checkbox change up; the parent decides if it triggers a refresh
   *  of the PaymentIntent (because setup_future_usage is read at intent creation). */
  onSaveCardChange: (next: boolean) => void;
  /** Allow disabling the save-card toggle (e.g. when the parent is refreshing
   *  the PaymentIntent in response to a previous toggle). */
  saveCardLocked?: boolean;
  /** Called when the PaymentIntent resolves SYNCHRONOUSLY with status=succeeded
   *  (most card payments without 3DS). The parent navigates to confirmation.
   *  For 3DS / wallet flows, Stripe redirects to returnUrl instead and this is
   *  never invoked — the parent handles the post-redirect via URL params. */
  onSuccess: () => void;
}

/**
 * Stripe-powered card form. The `<Elements>` provider needs a clientSecret
 * upfront because the appearance and supported methods are derived from the
 * server-side PaymentIntent.
 */
export function StripePaymentForm(props: StripePaymentFormProps) {
  const { locale } = useI18n();
  const options: StripeElementsOptionsClientSecret = {
    clientSecret: props.clientSecret,
    locale: locale === 'ar' ? 'auto' : locale as 'fr' | 'en' | 'ms' | 'auto',
    appearance: {
      theme: 'stripe',
      variables: {
        colorPrimary: '#0a8a8a',   // matches brand-primary in tailwind config
        borderRadius: '6px',
      },
    },
  };

  return (
    <Elements stripe={stripePromise} options={options}>
      <Inner {...props} />
    </Elements>
  );
}

/** Inner component — must live UNDER <Elements> to use useStripe / useElements. */
function Inner({ returnUrl, saveCard, onSaveCardChange, saveCardLocked, onSuccess }: StripePaymentFormProps) {
  const { t, locale } = useI18n();
  const stripe = useStripe();
  const elements = useElements();
  const [submitting, setSubmitting] = useState(false);
  /**
   * When non-null, we render an "Awaiting 3DS / authentication" panel and hide
   * the form. Stripe is about to redirect to its hosted challenge page; the
   * brief window between confirmPayment() resolving with requires_action and
   * the actual browser navigation is otherwise blank and confusing.
   */
  const [awaitingAction, setAwaitingAction] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!stripe || !elements) return;

    setSubmitting(true);
    setError(null);

    const { error: stripeError, paymentIntent } = await stripe.confirmPayment({
      elements,
      confirmParams: { return_url: returnUrl },
      // 'if_required' = handle 3DS / wallet redirects only when Stripe deems
      // them necessary. For non-3DS cards the promise resolves here with
      // the final paymentIntent.status; for 3DS, Stripe takes over the page
      // before this resolves and we never see paymentIntent locally.
      redirect: 'if_required',
    });

    if (stripeError) {
      // Translate via our dictionary first (covers MS/AR where Stripe falls
      // back to EN), then Stripe's own localized message, then a generic
      // fallback. See lib/stripe-errors.ts.
      setError(getStripeErrorMessage(stripeError, t));
      setSubmitting(false);
      return;
    }

    if (!paymentIntent) {
      // Unexpected: no error AND no PI. Treat as generic failure.
      setError(t('common.error'));
      setSubmitting(false);
      return;
    }

    // Surface the actual PaymentIntent status — Stripe doesn't always mean
    // "succeeded" when no error is thrown.
    switch (paymentIntent.status) {
      case 'succeeded':
        onSuccess();
        return;
      case 'processing':
        // Some bank methods (SEPA Direct Debit, multibanco) finalize async.
        // For us, only cards are enabled today, but the case can happen on
        // some test cards (4000 0000 0000 0077). Treat as "OK in progress",
        // the webhook will settle the order; show the confirmation page.
        onSuccess();
        return;
      case 'requires_action':
      case 'requires_confirmation':
        // Stripe should have redirected us already if redirect was needed.
        // Show a waiting state in case the redirect is delayed (slow network).
        setAwaitingAction(locale === 'fr'
          ? 'Validation en cours, ne fermez pas cet onglet…'
          : 'Authentication in progress, do not close this tab…');
        return;
      case 'requires_payment_method':
        setError(locale === 'fr'
          ? 'Le paiement a échoué. Veuillez réessayer avec une autre méthode.'
          : 'Payment failed. Please try a different method.');
        setSubmitting(false);
        return;
      default:
        setError(`${t('common.error')} (${paymentIntent.status})`);
        setSubmitting(false);
    }
  };

  if (awaitingAction) {
    return (
      <div className="p-6 bg-warning/5 border border-warning/30 rounded-md flex items-center gap-3">
        <Loader2 className="w-5 h-5 animate-spin text-warning" />
        <p className="text-sm text-warning">{awaitingAction}</p>
      </div>
    );
  }

  return (
    <form onSubmit={handleSubmit} className="space-y-4">
      <PaymentElement
        options={{
          layout: 'tabs',
          // We don't pre-fill the billing details — Stripe will collect
          // them from the user (and they may differ from our shipping addr).
        }}
      />

      <div className="flex items-center gap-2 pt-2">
        <Checkbox
          id="saveCard"
          checked={saveCard}
          disabled={saveCardLocked}
          onCheckedChange={(v) => onSaveCardChange(!!v)}
        />
        <Label htmlFor="saveCard" className="cursor-pointer text-sm">
          {locale === 'fr'
            ? 'Sauvegarder ma carte pour mes prochains achats'
            : 'Save this card for future purchases'}
        </Label>
      </div>

      {error && (
        <p className="text-sm text-error" role="alert">
          {error}
        </p>
      )}

      <Button
        type="submit"
        size="lg"
        disabled={!stripe || !elements || submitting}
        className="w-full bg-brand-primary hover:bg-brand-hover text-white"
      >
        {submitting ? <Loader2 className="w-5 h-5 animate-spin" /> : t('checkout.place_order')}
      </Button>
    </form>
  );
}
