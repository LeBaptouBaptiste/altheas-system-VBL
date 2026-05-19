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
function Inner({ returnUrl, saveCard, onSaveCardChange, saveCardLocked }: StripePaymentFormProps) {
  const { t, locale } = useI18n();
  const stripe = useStripe();
  const elements = useElements();
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!stripe || !elements) return;

    setSubmitting(true);
    setError(null);

    const { error: stripeError } = await stripe.confirmPayment({
      elements,
      confirmParams: { return_url: returnUrl },
      // If no redirect is needed (most card payments without 3DS), Stripe
      // resolves the promise here with the final status. With redirect:
      // 'if_required', we only redirect for 3DS / wallet flows that mandate it.
      redirect: 'if_required',
    });

    if (stripeError) {
      // card_declined, insufficient_funds, incorrect_cvc, expired_card, …
      // Display Stripe's localized message — it already speaks the user's
      // language thanks to the locale we passed to Elements above.
      setError(stripeError.message ?? t('common.error'));
      setSubmitting(false);
      return;
    }

    // Success without redirect (no 3DS challenge). The webhook will confirm
    // the order server-side; in the meantime we navigate to the confirmation
    // page using the same shape as the redirect flow.
    const url = new URL(returnUrl);
    url.searchParams.set('redirect_status', 'succeeded');
    window.location.href = url.toString();
  };

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
