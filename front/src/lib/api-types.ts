// ── Shared ────────────────────────────────────────────

export interface LocalizedString {
  [key: string]: string;
  fr: string;
  en: string;
}

export function toLocalized(fr: string, en: string): LocalizedString {
  return { fr, en };
}

export interface PaginatedResponse<T> {
  data: T[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
}

export interface CartItem {
  productId: string;
  quantity: number;
}

// ── Auth ──────────────────────────────────────────────

export interface AuthResponse {
  accessToken: string;
  refreshToken: string;
  user: UserDto;
}

// ── Users ─────────────────────────────────────────────

export interface UserDto {
  id: string;
  name: string;
  email: string;
  role: 'Customer' | 'Admin';
  status: 'Active' | 'Inactive';
  anonymized: boolean;
  emailConfirmed: boolean;
  twoFactorEnabled: boolean;
  lastLogin: string | null;
  createdAt: string;
  addresses: AddressDto[];
  paymentMethods: PaymentMethodDto[];
}

export interface AddressDto {
  id: string;
  label: string;
  firstName: string;
  lastName: string;
  company: string | null;
  street: string;
  street2: string | null;
  city: string;
  postalCode: string;
  country: string;
  phone: string | null;
}

export interface PaymentMethodDto {
  id: string;
  type: string;
  label: string;
}

// ── Products ──────────────────────────────────────────

export interface ProductDto {
  id: string;
  slug: string;
  nameFr: string;
  nameEn: string;
  descriptionFr: string;
  descriptionEn: string;
  longDescriptionFr: string;
  longDescriptionEn: string;
  priceHT: number;
  vatRate: 'Standard' | 'Intermediate' | 'Reduced' | 'Zero';
  stockQty: number;
  stockStatus: 'InStock' | 'LowStock' | 'OutOfStock';
  isNew: boolean;
  priorityRank: number;
  images: string[];
  status: 'Active' | 'Inactive' | 'Draft';
  createdAt: string;
  updatedAt: string;
  categories: CategorySummaryDto[];
  specs: ProductSpecDto[];
}

export interface CategorySummaryDto {
  id: string;
  slug: string;
  nameFr: string;
  nameEn: string;
}

export interface ProductSpecDto {
  label: string;
  value: string;
}

// ── Categories ────────────────────────────────────────

export interface CategoryDto {
  id: string;
  slug: string;
  nameFr: string;
  nameEn: string;
  descriptionFr: string;
  descriptionEn: string;
  image: string;
  parentId: string | null;
  displayOrder: number;
  active: boolean;
  productCount: number;
}

// ── Orders ────────────────────────────────────────────

export interface OrderDto {
  id: string;
  userId: string;
  userName: string;
  date: string;
  status: 'Pending' | 'Confirmed' | 'Processing' | 'Shipped' | 'Delivered' | 'Cancelled' | 'Returned';
  paymentStatus: 'Pending' | 'Paid' | 'Failed' | 'Refunded';
  paymentMethod: 'Card' | 'BankTransfer' | 'PayPal';
  billingAddress: AddressDto;
  shippingAddress: AddressDto;
  shippingMethod: 'Standard' | 'Express' | 'Overnight';
  shippingCost: number;
  totalHT: number;
  totalVAT: number;
  totalTTC: number;
  createdAt: string;
  updatedAt: string;
  items: OrderItemDto[];
  statusHistory: OrderStatusChangeDto[];
}

export interface OrderItemDto {
  productId: string;
  productNameFr: string;
  productNameEn: string;
  quantity: number;
  priceHT: number;
  vatRate: string;
}

export interface OrderStatusChangeDto {
  from: string;
  to: string;
  date: string;
  userId: string;
}

// ── Invoices ──────────────────────────────────────────

export interface InvoiceDto {
  id: string;
  orderId: string;
  date: string;
  amountHT: number;
  vatAmount: number;
  amountTTC: number;
  status: 'Paid' | 'Pending' | 'Overdue' | 'Cancelled';
  type: 'Invoice' | 'CreditNote';
  relatedInvoiceId: string | null;
}

// ── Messaging ─────────────────────────────────────────

export interface ContactMessageDto {
  id: string;
  email: string;
  subject: string;
  message: string;
  status: 'Unread' | 'Read' | 'Replied' | 'Archived';
  createdAt: string;
}

export interface ChatConversationDto {
  id: string;
  userId: string | null;
  email: string | null;
  escalated: boolean;
  ticketId: string | null;
  createdAt: string;
  messages: ChatMessageDto[];
}

export interface ChatMessageDto {
  id: string;
  role: 'User' | 'Bot';
  content: string;
  timestamp: string;
}

export interface SupportTicketDto {
  id: string;
  conversationId: string | null;
  contactMessageId: string | null;
  email: string;
  subject: string;
  status: 'Open' | 'InProgress' | 'Resolved' | 'Closed';
  createdAt: string;
  updatedAt: string;
}

// ── Content ───────────────────────────────────────────

export interface HeroSlideDto {
  id: string;
  image: string;
  titleFr: string;
  titleEn: string;
  subtitleFr: string;
  subtitleEn: string;
  descriptionFr: string;
  descriptionEn: string;
  ctaFr: string;
  ctaEn: string;
  link: string;
  displayOrder: number;
  active: boolean;
}

export interface StaticPageDto {
  id: string;
  slug: string;
  titleFr: string;
  titleEn: string;
  contentFr: string;
  contentEn: string;
  updatedAt: string;
}

// ── Analytics ─────────────────────────────────────────

export interface SalesAnalyticsDto {
  date: string;
  revenue: number;
  orderCount: number;
  categoryBreakdown: Record<string, number>;
}

export interface DashboardKpiDto {
  totalRevenue: number;
  totalOrders: number;
  totalCustomers: number;
  totalProducts: number;
  averageOrderValue: number;
  dailySales: SalesAnalyticsDto[];
}

// ── Shipping ──────────────────────────────────────────

export interface ShippingMethodDto {
  method: string;
  labelFr: string;
  labelEn: string;
  cost: number;
  estimatedDeliveryFr: string;
  estimatedDeliveryEn: string;
}

// ── Health ────────────────────────────────────────────

export interface HealthResponse {
  status: string;
  version: string;
  timestamp: string;
}

// ── Image helpers (moved from mock/images.ts) ─────────

const IMAGE_MAP: Record<string, string> = {
  'imaging-1': 'https://images.unsplash.com/photo-1587010580103-fd86b8ea14ca?crop=entropy&cs=tinysrgb&fit=max&fm=jpg&q=80&w=600',
  'monitors-1': 'https://images.unsplash.com/photo-1721114989769-0423619f03d2?crop=entropy&cs=tinysrgb&fit=max&fm=jpg&q=80&w=600',
  'sterilization-1': 'https://images.unsplash.com/photo-1758653500328-1c4474a8adfe?crop=entropy&cs=tinysrgb&fit=max&fm=jpg&q=80&w=600',
  'surgical-1': 'https://images.unsplash.com/photo-1560269941-141b145a1b57?crop=entropy&cs=tinysrgb&fit=max&fm=jpg&q=80&w=600',
  'furniture-1': 'https://images.unsplash.com/photo-1710074213374-e68503a1b795?crop=entropy&cs=tinysrgb&fit=max&fm=jpg&q=80&w=600',
  'respiratory-1': 'https://images.unsplash.com/photo-1721114989769-0423619f03d2?crop=entropy&cs=tinysrgb&fit=max&fm=jpg&q=80&w=600',
  'consumables-1': 'https://images.unsplash.com/photo-1758653500328-1c4474a8adfe?crop=entropy&cs=tinysrgb&fit=max&fm=jpg&q=80&w=600',
  'laboratory-1': 'https://images.unsplash.com/photo-1766299892549-b56b257d1ddd?crop=entropy&cs=tinysrgb&fit=max&fm=jpg&q=80&w=600',
};

const DEFAULT_IMAGE = 'https://images.unsplash.com/photo-1766299892693-2370a8d47e23?crop=entropy&cs=tinysrgb&fit=max&fm=jpg&q=80&w=600';

export function getImageUrl(key: string): string {
  return IMAGE_MAP[key] || DEFAULT_IMAGE;
}

export function getProductImageUrl(product: ProductDto): string {
  if (!product.images || product.images.length === 0) return DEFAULT_IMAGE;
  return IMAGE_MAP[product.images[0]] || DEFAULT_IMAGE;
}

export function getCategoryImageUrl(category: CategoryDto): string {
  return category.image ? (IMAGE_MAP[category.image] || DEFAULT_IMAGE) : DEFAULT_IMAGE;
}
