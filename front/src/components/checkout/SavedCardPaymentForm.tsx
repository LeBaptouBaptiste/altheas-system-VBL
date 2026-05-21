'use client';

import { useState } from 'react';
import { Loader2 } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { stripePromise } from '@/lib/stripe';
import { getStripeErrorMessage } from '@/lib/stripe-errors';
import { useI18n } from '@/context/i18n-context';

interface SavedCardPaymentFormProps {
  /** From POST /api/payments/intents — the same PaymentIntent we'd use with Elements. */
  clientSecret: string;
  /** Stripe PaymentMethod id (pm_xxx) saved on the user's profile. */
  stripePaymentMethodId: string;
  /** Where Stripe redirects after 3DS / wallet — same shape as the Elements flow. */
  returnUrl: string;
  /** Display label shown next to the confirm button, e.g. "Visa •••• 4242". */
  label: string;
  /** Called when the PaymentIntent resolves sync with status=succeeded. */
  onSuccess: () => void;
}

/**
 * Confirmation form for a payment using a previously saved card. No Elements
 * needed since we don't collect any new card data — just a "Pay now" button
 * that calls stripe.confirmCardPayment(clientSecret, { payment_method }).
 *
 * If 3DS is required, Stripe routes through return_url; the post-redirect
 * parsing in checkout/page.tsx picks it up on mount.
 */
export function SavedCardPaymentForm({
  clientSecret,
  stripePaymentMethodId,
  returnUrl,
  label,
  onSuccess,
}: SavedCardPaymentFormProps) {
  const { t } = useI18n();
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setSubmitting(true);
    setError(null);

    const stripe = await stripePromise;
    if (!stripe) {
      setError(t('common.error'));
      setSubmitting(false);
      return;
    }

    const { paymentIntent, error: stripeError } = await stripe.confirmCardPayment(
      clientSecret,
      {
        payment_method: stripePaymentMethodId,
        return_url: returnUrl,
      },
    );

    if (stripeError) {
      setError(getStripeErrorMessage(stripeError, t));
      setSubmitting(false);
      return;
    }

    if (!paymentIntent) {
      setError(t('common.error'));
      setSubmitting(false);
      return;
    }

    // Same status switch as StripePaymentForm. We rely on the webhook to
    // settle the order server-side; this is just the UX path.
    switch (paymentIntent.status) {
      case 'succeeded':
      case 'processing':
        onSuccess();
        return;
      case 'requires_action':
      case 'requires_confirmation':
        // Stripe should have redirected for 3DS; show transition state.
        setError(t('payment.authenticating'));
        return;
      case 'requires_payment_method':
        setError(t('payment.payment_failed_card'));
        setSubmitting(false);
        return;
      default:
        setError(`${t('common.error')} (${paymentIntent.status})`);
        setSubmitting(false);
    }
  };

  return (
    <form onSubmit={handleSubmit} className="space-y-4">
      <div className="p-4 bg-blue-50 border border-blue-200 rounded-md text-sm text-brand-dark">
        {t('payment.about_to_pay_with')}{' '}
        <span className="font-semibold">{label}</span>
        .
      </div>

      {error && (
        <p className="text-sm text-error" role="alert">
          {error}
        </p>
      )}

      <Button
        type="submit"
        size="lg"
        disabled={submitting}
        className="w-full bg-brand-primary hover:bg-brand-hover text-white"
      >
        {submitting ? <Loader2 className="w-5 h-5 animate-spin" /> : t('checkout.place_order')}
      </Button>
    </form>
  );
}
