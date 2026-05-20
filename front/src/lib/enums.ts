// ── API Enum Values (numeric) ────────────────────────
// These match the C# enum values from the backend.

export const UserRole = { Customer: 0, Admin: 1 } as const;
export type UserRole = (typeof UserRole)[keyof typeof UserRole];

export const UserStatus = { Active: 0, Inactive: 1 } as const;
export type UserStatus = (typeof UserStatus)[keyof typeof UserStatus];

export const ProductStatus = { Active: 0, Inactive: 1, Draft: 2 } as const;
export type ProductStatus = (typeof ProductStatus)[keyof typeof ProductStatus];

export const StockStatus = { InStock: 0, LowStock: 1, OutOfStock: 2 } as const;
export type StockStatus = (typeof StockStatus)[keyof typeof StockStatus];

export const VatRate = { Standard: 0, Intermediate: 1, Reduced: 2, Zero: 3 } as const;
export type VatRate = (typeof VatRate)[keyof typeof VatRate];

export const OrderStatus = { Pending: 0, Confirmed: 1, Processing: 2, Shipped: 3, Delivered: 4, Cancelled: 5, Returned: 6 } as const;
export type OrderStatus = (typeof OrderStatus)[keyof typeof OrderStatus];

export const PaymentStatus = { Pending: 0, Paid: 1, Failed: 2, Refunded: 3 } as const;
export type PaymentStatus = (typeof PaymentStatus)[keyof typeof PaymentStatus];

export const PaymentMethod = { Card: 0, BankTransfer: 1, PayPal: 2 } as const;
export type PaymentMethod = (typeof PaymentMethod)[keyof typeof PaymentMethod];

export const ShippingMethod = { Standard: 0, Express: 1, Overnight: 2 } as const;
export type ShippingMethod = (typeof ShippingMethod)[keyof typeof ShippingMethod];

export const MessageStatus = { Unread: 0, Read: 1, Replied: 2, Archived: 3 } as const;
export type MessageStatus = (typeof MessageStatus)[keyof typeof MessageStatus];

export const TicketStatus = { Open: 0, InProgress: 1, Resolved: 2, Closed: 3 } as const;
export type TicketStatus = (typeof TicketStatus)[keyof typeof TicketStatus];

export const InvoiceStatus = { Paid: 0, Pending: 1, Overdue: 2, Cancelled: 3 } as const;
export type InvoiceStatus = (typeof InvoiceStatus)[keyof typeof InvoiceStatus];

export const InvoiceType = { Invoice: 0, CreditNote: 1 } as const;
export type InvoiceType = (typeof InvoiceType)[keyof typeof InvoiceType];

// Phase 7: how a credit note's money moves. Mirrors backend CreditNoteMode.
// Only set when InvoiceType=CreditNote.
export const CreditNoteMode = { Refund: 0, StoreCredit: 1 } as const;
export type CreditNoteMode = (typeof CreditNoteMode)[keyof typeof CreditNoteMode];

// Phase 4b: 2FA method picker. Indices MUST match
// API_Althea-systems/Common/Enums/TwoFactorMethod.cs. Even though the backend
// serialises this as a string (JsonStringEnumConverter), the numeric values
// are used for client-side comparisons via the api.ts normaliseEnums layer.
export const TwoFactorMethod = { None: 0, Authenticator: 1, Email: 2 } as const;
export type TwoFactorMethod = (typeof TwoFactorMethod)[keyof typeof TwoFactorMethod];

// ── Display labels (for UI rendering) ────────────────
// Usage: enumLabel(ProductStatus, product.status) => "Active"

const LABELS: Record<string, Record<number, Record<string, string>>> = {
  UserRole: {
    0: { fr: 'Client', en: 'Customer' },
    1: { fr: 'Admin', en: 'Admin' },
  },
  UserStatus: {
    0: { fr: 'Actif', en: 'Active' },
    1: { fr: 'Inactif', en: 'Inactive' },
  },
  ProductStatus: {
    0: { fr: 'Actif', en: 'Active' },
    1: { fr: 'Inactif', en: 'Inactive' },
    2: { fr: 'Brouillon', en: 'Draft' },
  },
  StockStatus: {
    0: { fr: 'En stock', en: 'In Stock' },
    1: { fr: 'Stock faible', en: 'Low Stock' },
    2: { fr: 'Rupture', en: 'Out of Stock' },
  },
  VatRate: {
    0: { fr: 'TVA 20%', en: 'VAT 20%' },
    1: { fr: 'TVA 10%', en: 'VAT 10%' },
    2: { fr: 'TVA 5.5%', en: 'VAT 5.5%' },
    3: { fr: 'Exonéré', en: 'Exempt' },
  },
  OrderStatus: {
    0: { fr: 'En attente', en: 'Pending' },
    1: { fr: 'Confirmée', en: 'Confirmed' },
    2: { fr: 'En traitement', en: 'Processing' },
    3: { fr: 'Expédiée', en: 'Shipped' },
    4: { fr: 'Livrée', en: 'Delivered' },
    5: { fr: 'Annulée', en: 'Cancelled' },
    6: { fr: 'Retournée', en: 'Returned' },
  },
  PaymentStatus: {
    0: { fr: 'En attente', en: 'Pending' },
    1: { fr: 'Payé', en: 'Paid' },
    2: { fr: 'Échoué', en: 'Failed' },
    3: { fr: 'Remboursé', en: 'Refunded' },
  },
  PaymentMethod: {
    0: { fr: 'Carte', en: 'Card' },
    1: { fr: 'Virement', en: 'Bank Transfer' },
    2: { fr: 'PayPal', en: 'PayPal' },
  },
  ShippingMethod: {
    0: { fr: 'Standard', en: 'Standard' },
    1: { fr: 'Express', en: 'Express' },
    2: { fr: '24h', en: 'Overnight' },
  },
  MessageStatus: {
    0: { fr: 'Non lu', en: 'Unread' },
    1: { fr: 'Lu', en: 'Read' },
    2: { fr: 'Répondu', en: 'Replied' },
    3: { fr: 'Archivé', en: 'Archived' },
  },
  TicketStatus: {
    0: { fr: 'Ouvert', en: 'Open' },
    1: { fr: 'En cours', en: 'In Progress' },
    2: { fr: 'Résolu', en: 'Resolved' },
    3: { fr: 'Fermé', en: 'Closed' },
  },
  InvoiceStatus: {
    0: { fr: 'Payée', en: 'Paid' },
    1: { fr: 'En attente', en: 'Pending' },
    2: { fr: 'En retard', en: 'Overdue' },
    3: { fr: 'Annulée', en: 'Cancelled' },
  },
  InvoiceType: {
    0: { fr: 'Facture', en: 'Invoice' },
    1: { fr: 'Avoir', en: 'Credit Note' },
  },
};

export function enumLabel(enumName: string, value: number, locale: string = 'fr'): string {
  return LABELS[enumName]?.[value]?.[locale] ?? String(value);
}
