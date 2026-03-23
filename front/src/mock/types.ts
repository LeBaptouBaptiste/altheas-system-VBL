import type { StockStatus, OrderStatus, PaymentStatus, PaymentMethod, ProductStatus, UserStatus, MessageStatus, TicketStatus, VatRateKey } from '@/lib/constants';

export interface LocalizedString {
  [key: string]: string;
  fr: string;
  en: string;
}

export interface Category {
  id: string;
  name: LocalizedString;
  slug: string;
  description: LocalizedString;
  image: string;
  parentId?: string;
  displayOrder: number;
  active: boolean;
}

export interface Product {
  id: string;
  slug: string;
  name: LocalizedString;
  description: LocalizedString;
  longDescription: LocalizedString;
  priceHT: number;
  vatRate: VatRateKey;
  stockQty: number;
  stockStatus: StockStatus;
  isNew: boolean;
  priorityRank: number; // 0 = not prioritized, 1+ = prioritized (lower = higher priority)
  categories: string[]; // category IDs
  images: string[]; // image keys
  specs: { label: string; value: string }[];
  status: ProductStatus;
  createdAt: string;
  updatedAt: string;
}

export interface Address {
  id: string;
  label: string;
  firstName: string;
  lastName: string;
  company?: string;
  street: string;
  street2?: string;
  city: string;
  postalCode: string;
  country: string;
  phone?: string;
}

export interface UserPaymentMethod {
  id: string;
  type: 'visa' | 'mastercard' | 'bank_transfer';
  label: string; // e.g. "Visa •••• 4242"
}

export interface User {
  id: string;
  name: string;
  email: string;
  status: UserStatus;
  anonymized: boolean;
  emailConfirmed: boolean;
  addresses: Address[];
  paymentMethods: UserPaymentMethod[];
  lastLogin: string;
  createdAt: string;
  role: 'customer' | 'admin';
  password?: string; // mock only
}

export interface OrderItem {
  productId: string;
  productName: LocalizedString;
  quantity: number;
  priceHT: number;
  vatRate: VatRateKey;
}

export interface OrderStatusChange {
  from: OrderStatus;
  to: OrderStatus;
  date: string;
  userId: string;
}

export interface Order {
  id: string;
  userId: string;
  date: string;
  status: OrderStatus;
  paymentStatus: PaymentStatus;
  paymentMethod: PaymentMethod;
  items: OrderItem[];
  billingAddress: Address;
  shippingAddress: Address;
  shippingMethod: string;
  shippingCost: number;
  statusHistory: OrderStatusChange[];
  createdAt: string;
  updatedAt: string;
}

export interface Invoice {
  id: string;
  orderId: string;
  date: string;
  amountHT: number;
  vatAmount: number;
  amountTTC: number;
  status: 'paid' | 'pending' | 'overdue' | 'cancelled';
  type: 'invoice' | 'credit_note';
  relatedInvoiceId?: string; // for credit notes
}

export interface ContactMessage {
  id: string;
  email: string;
  subject: string;
  message: string;
  status: MessageStatus;
  createdAt: string;
}

export interface ChatMessage {
  id: string;
  role: 'user' | 'bot';
  content: string;
  timestamp: string;
}

export interface ChatConversation {
  id: string;
  userId?: string;
  email?: string;
  messages: ChatMessage[];
  escalated: boolean;
  ticketId?: string;
  createdAt: string;
}

export interface SupportTicket {
  id: string;
  conversationId?: string;
  contactMessageId?: string;
  email: string;
  subject: string;
  status: TicketStatus;
  createdAt: string;
  updatedAt: string;
}

export interface SalesAnalytics {
  date: string;
  revenue: number;
  orders: number;
  categoryBreakdown: Record<string, number>;
}

export interface HeroSlide {
  id: string;
  image: string;
  title: LocalizedString;
  subtitle: LocalizedString;
  description: LocalizedString;
  cta: LocalizedString;
  link: string;
}

export interface StaticPage {
  id: string;
  slug: string;
  title: LocalizedString;
  content: LocalizedString;
  updatedAt: string;
}

export interface CartItem {
  productId: string;
  quantity: number;
}
