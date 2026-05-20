import { api } from '@/lib/api';
import type { InvoiceDto, PaginatedResponse } from '@/lib/api-types';

export const invoicesService = {
  getAll: (page = 1, pageSize = 20) =>
    api.get<PaginatedResponse<InvoiceDto>>(`/invoices?page=${page}&pageSize=${pageSize}`),

  getById: (id: string) =>
    api.get<InvoiceDto>(`/invoices/${id}`),

  create: (data: { orderId: string; type: string; relatedInvoiceId?: string }) =>
    api.post<InvoiceDto>('/invoices', data),

  /**
   * Phase 6: issues a credit note (avoir) against the given paid invoice.
   * Admin-only + step-up gated. Returns the new InvoiceDto with type=1.
   * Server enforces business invariants (Paid, Type=Invoice, amount ≤
   * remaining) and surfaces failure as 400 with `reason` ∈
   * { not_an_invoice, not_paid, invalid_amount, exceeds_remaining }.
   */
  /**
   * `mode` is the CreditNoteMode (0=Refund via Stripe, 1=StoreCredit on the
   * customer's account). The server validates accordingly — Refund requires
   * the original order to have a Stripe PaymentIntent.
   */
  issueCreditNote: (
    originalInvoiceId: string,
    amountHT: number,
    mode: number,
    reason: string | null,
    stepUpToken?: string,
  ) =>
    api.post<InvoiceDto>(
      `/invoices/${originalInvoiceId}/credit-note`,
      { amountHT, mode, reason },
      stepUpToken ? { stepUpToken } : undefined,
    ),

  /**
   * Downloads the real PDF rendered server-side by QuestPDF. Returns the
   * raw Blob — caller is responsible for triggering the browser download
   * (see `downloadInvoicePdf` helper below).
   */
  downloadPdf: (id: string) =>
    api.getBlob(`/invoices/${id}/pdf`),
};

/**
 * Convenience helper: fetches the PDF and triggers a download via an
 * anchor click. Kept here rather than in a component so it can be
 * reused from the admin and customer order pages without duplication.
 */
export async function downloadInvoicePdf(invoiceId: string): Promise<void> {
  return downloadPdfWithPrefix(invoiceId, 'facture');
}

/**
 * Phase 6: same endpoint, different filename prefix. Lets the UI hand the
 * user a sensibly-named file (avoir-XXX.pdf instead of facture-XXX.pdf)
 * when downloading a credit note from /account/orders or /admin/invoices.
 */
export async function downloadCreditNotePdf(creditNoteId: string): Promise<void> {
  return downloadPdfWithPrefix(creditNoteId, 'avoir');
}

async function downloadPdfWithPrefix(id: string, prefix: 'facture' | 'avoir'): Promise<void> {
  const blob = await invoicesService.downloadPdf(id);
  const url = URL.createObjectURL(blob);
  const a = document.createElement('a');
  a.href = url;
  a.download = `${prefix}-${id.slice(0, 8).toUpperCase()}.pdf`;
  document.body.appendChild(a);  // Firefox requires the anchor be in the DOM
  a.click();
  document.body.removeChild(a);
  // Free the object URL after a tick so the click can resolve.
  setTimeout(() => URL.revokeObjectURL(url), 1000);
}
