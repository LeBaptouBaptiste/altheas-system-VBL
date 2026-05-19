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
   */
  createIntent: (orderId: string, saveCard: boolean) =>
    api.post<CreatePaymentIntentResponse>('/payments/intents', { orderId, saveCard }),
};
