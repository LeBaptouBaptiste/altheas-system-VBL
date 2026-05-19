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
  const blob = await invoicesService.downloadPdf(invoiceId);
  const url = URL.createObjectURL(blob);
  const a = document.createElement('a');
  a.href = url;
  a.download = `facture-${invoiceId.slice(0, 8).toUpperCase()}.pdf`;
  document.body.appendChild(a);  // Firefox requires the anchor be in the DOM
  a.click();
  document.body.removeChild(a);
  // Free the object URL after a tick so the click can resolve.
  setTimeout(() => URL.revokeObjectURL(url), 1000);
}
