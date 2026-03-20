import type { Invoice } from './types';

export const invoices: Invoice[] = [
  { id: 'INV-2026-001', orderId: 'ORD-2026-001', date: '2026-02-18', amountHT: 45170, vatAmount: 9017, amountTTC: 54187, status: 'paid', type: 'invoice' },
  { id: 'INV-2026-002', orderId: 'ORD-2026-002', date: '2026-02-17', amountHT: 17000, vatAmount: 3400, amountTTC: 20400, status: 'pending', type: 'invoice' },
  { id: 'INV-2026-003', orderId: 'ORD-2026-003', date: '2026-02-16', amountHT: 5825, vatAmount: 1117.75, amountTTC: 6942.75, status: 'paid', type: 'invoice' },
  { id: 'INV-2026-004', orderId: 'ORD-2026-004', date: '2026-02-15', amountHT: 65900, vatAmount: 13180, amountTTC: 79080, status: 'pending', type: 'invoice' },
  { id: 'INV-2026-005', orderId: 'ORD-2026-005', date: '2026-02-14', amountHT: 9600, vatAmount: 1920, amountTTC: 11520, status: 'paid', type: 'invoice' },
  { id: 'INV-2026-006', orderId: 'ORD-2026-006', date: '2026-02-13', amountHT: 450000, vatAmount: 90000, amountTTC: 540000, status: 'cancelled', type: 'invoice' },
  { id: 'AV-2026-001', orderId: 'ORD-2026-006', date: '2026-02-13', amountHT: -450000, vatAmount: -90000, amountTTC: -540000, status: 'paid', type: 'credit_note', relatedInvoiceId: 'INV-2026-006' },
  { id: 'INV-2026-007', orderId: 'ORD-2026-007', date: '2026-02-10', amountHT: 4620, vatAmount: 914.60, amountTTC: 5534.60, status: 'paid', type: 'invoice' },
  { id: 'INV-2026-008', orderId: 'ORD-2026-008', date: '2026-02-08', amountHT: 4800, vatAmount: 960, amountTTC: 5760, status: 'paid', type: 'invoice' },
  { id: 'INV-2026-009', orderId: 'ORD-2026-009', date: '2026-02-06', amountHT: 56000, vatAmount: 11200, amountTTC: 67200, status: 'pending', type: 'invoice' },
  { id: 'INV-2026-010', orderId: 'ORD-2026-010', date: '2026-02-03', amountHT: 1300, vatAmount: 71.50, amountTTC: 1371.50, status: 'paid', type: 'invoice' },
  { id: 'INV-2025-011', orderId: 'ORD-2025-011', date: '2025-12-15', amountHT: 3700, vatAmount: 740, amountTTC: 4440, status: 'paid', type: 'invoice' },
  { id: 'INV-2025-012', orderId: 'ORD-2025-012', date: '2025-11-20', amountHT: 32000, vatAmount: 6400, amountTTC: 38400, status: 'paid', type: 'invoice' },
  { id: 'INV-2025-013', orderId: 'ORD-2025-013', date: '2025-10-05', amountHT: 18500, vatAmount: 3700, amountTTC: 22200, status: 'paid', type: 'invoice' },
  { id: 'INV-2025-014', orderId: 'ORD-2025-014', date: '2025-09-12', amountHT: 7800, vatAmount: 1560, amountTTC: 9360, status: 'paid', type: 'invoice' },
  { id: 'INV-2025-015', orderId: 'ORD-2025-015', date: '2025-08-20', amountHT: 540, vatAmount: 29.70, amountTTC: 569.70, status: 'paid', type: 'invoice' },
];
