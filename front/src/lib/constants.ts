// VAT rates available in the system
export const VAT_RATES = {
  STANDARD: 0.20,
  INTERMEDIATE: 0.10,
  REDUCED: 0.055,
  ZERO: 0,
} as const;

export type VatRateKey = keyof typeof VAT_RATES;

// Order statuses
export const ORDER_STATUSES = {
  PENDING: 'pending',
  PROCESSING: 'processing',
  SHIPPED: 'shipped',
  DELIVERED: 'delivered',
  CANCELLED: 'cancelled',
} as const;

export type OrderStatus = (typeof ORDER_STATUSES)[keyof typeof ORDER_STATUSES];

// Payment statuses
export const PAYMENT_STATUSES = {
  VALIDATED: 'validated',
  PENDING: 'pending',
  FAILED: 'failed',
  REFUNDED: 'refunded',
} as const;

export type PaymentStatus = (typeof PAYMENT_STATUSES)[keyof typeof PAYMENT_STATUSES];

// Payment methods
export const PAYMENT_METHODS = {
  CARD: 'card',
  BANK_TRANSFER: 'bank_transfer',
  ADMIN_MANDATE: 'admin_mandate',
} as const;

export type PaymentMethod = (typeof PAYMENT_METHODS)[keyof typeof PAYMENT_METHODS];

// Stock statuses
export const STOCK_STATUSES = {
  IN_STOCK: 'in_stock',
  LOW_STOCK: 'low_stock',
  OUT_OF_STOCK: 'out_of_stock',
} as const;

export type StockStatus = (typeof STOCK_STATUSES)[keyof typeof STOCK_STATUSES];

// Product statuses (admin)
export const PRODUCT_STATUSES = {
  PUBLISHED: 'published',
  DRAFT: 'draft',
} as const;

export type ProductStatus = (typeof PRODUCT_STATUSES)[keyof typeof PRODUCT_STATUSES];

// User account statuses
export const USER_STATUSES = {
  ACTIVE: 'active',
  INACTIVE: 'inactive',
  PENDING: 'pending',
} as const;

export type UserStatus = (typeof USER_STATUSES)[keyof typeof USER_STATUSES];

// Contact message statuses
export const MESSAGE_STATUSES = {
  UNREAD: 'unread',
  READ: 'read',
  TREATED: 'treated',
} as const;

export type MessageStatus = (typeof MESSAGE_STATUSES)[keyof typeof MESSAGE_STATUSES];

// Ticket statuses
export const TICKET_STATUSES = {
  OPEN: 'open',
  IN_PROGRESS: 'in_progress',
  CLOSED: 'closed',
} as const;

export type TicketStatus = (typeof TICKET_STATUSES)[keyof typeof TICKET_STATUSES];

// Pagination defaults
export const PAGINATION = {
  DEFAULT_PAGE_SIZE: 12,
  ADMIN_PAGE_SIZES: [10, 25, 50] as const,
} as const;

// Shipping methods
export const SHIPPING_METHODS = [
  { id: 'standard', label: { fr: 'Livraison standard (5-7 jours)', en: 'Standard shipping (5-7 days)' }, price: 15.00 },
  { id: 'express', label: { fr: 'Livraison express (2-3 jours)', en: 'Express shipping (2-3 days)' }, price: 35.00 },
  { id: 'overnight', label: { fr: 'Livraison 24h', en: 'Overnight delivery' }, price: 75.00 },
] as const;
