import { api } from '@/lib/api';
import type { InvoiceDto, PaginatedResponse } from '@/lib/api-types';

export const invoicesService = {
  getAll: (page = 1, pageSize = 20) =>
    api.get<PaginatedResponse<InvoiceDto>>(`/invoices?page=${page}&pageSize=${pageSize}`),

  getById: (id: string) =>
    api.get<InvoiceDto>(`/invoices/${id}`),

  create: (data: { orderId: string; type: string; relatedInvoiceId?: string }) =>
    api.post<InvoiceDto>('/invoices', data),
};
