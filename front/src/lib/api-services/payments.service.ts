import { api } from '@/lib/api';

/**
 * Server returns this after creating a PaymentIntent for an order.
 * `clientSecret` is consumed by stripe.confirmPayment() on the browser.
 * Treat it like a short-lived credential: never log or send anywhere else.
 */
export interface CreatePaymentIntentResponse {
  clientSecret: string;
  paymentIntentId: string;
  amount: number;       // smallest unit (cents for EUR)
  currency: string;     // ISO 4217 lowercase, e.g. "eur"
}

export const paymentsService = {
  /**
   * Asks the API to create (or refresh) the PaymentIntent for an order.
   * The amount is recomputed server-side from order.Items — anything we
   * could pass here would be ignored, by design.
   *
   * `creditAppliedCents` is the store credit (cents EUR) to deduct from
   * the Stripe charge. Server validates against the user's balance and
   * persists the value on Order.CreditAppliedCents — so re-calling this
   * endpoint with a different credit value updates the order in-place
   * (the front uses this when the customer toggles "use my credit"
   * after reaching the payment step).
   */
  createIntent: (orderId: string, saveCard: boolean, creditAppliedCents: number = 0) =>
    api.post<CreatePaymentIntentResponse>('/payments/intents', {
      orderId,
      saveCard,
      creditAppliedCents,
    }),
};
