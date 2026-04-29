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

// LoginOutcome is serialized as a string by the API (JsonStringEnumConverter).
// We keep it as a string union here — `outcome` is NOT in the api.ts
// normalizeEnums mapping, so the value passes through untouched.
export type LoginOutcome =
  | 'Authenticated'
  | 'TwoFactorRequired'
  | 'TwoFactorSetupRequired';

/**
 * Discriminated union returned by POST /auth/login. Use `outcome` to branch:
 *   - Authenticated:           `auth` is populated; store accessToken
 *   - TwoFactorRequired:       call POST /auth/2fa/verify with `challengeToken`
 *   - TwoFactorSetupRequired:  admin enrollment — call /2fa/setup + /enable
 *                              using `setupToken` as bearer
 */
export interface LoginResponse {
  outcome: LoginOutcome;
  auth: AuthResponse | null;
  challengeToken: string | null;
  setupToken: string | null;
}

export interface VerifyTwoFactorChallengeRequest {
  challengeToken: string;
  code: string;
}

// ── Two-Factor (TOTP) ─────────────────────────────────

export interface TwoFactorSetupResult {
  /** Base32-encoded TOTP secret. Show as a fallback for users who can't scan. */
  secret: string;
  /** otpauth:// URI — render as a QR code. */
  otpAuthUri: string;
}

export interface TwoFactorEnableResponse {
  /** One-shot recovery codes (4-4-4-4 hex). Show ONCE, then they vanish. */
  recoveryCodes: string[];
  /** Fresh access token with amr=mfa, plus the user dto. */
  auth: AuthResponse;
}

export interface TwoFactorStatus {
  enabled: boolean;
  enabledAt: string | null;
  recoveryCodesRemaining: number;
}

export interface RegenerateRecoveryCodesResponse {
  recoveryCodes: string[];
}

// ── Step-up ───────────────────────────────────────────

export type StepUpPurpose = 'Action' | 'Admin';

export interface StepUpRequest {
  purpose: StepUpPurpose;
  /** TOTP code (6 digits) OR recovery code (xxxx-xxxx-xxxx-xxxx). */
  code?: string;
  /** Only valid for purpose='Action' on accounts without 2FA. */
  password?: string;
}

export interface StepUpResponse {
  token: string;
  /** Seconds until the token expires (60 for Action, 1800 for Admin). */
  expiresIn: number;
}

// ── Users ─────────────────────────────────────────────

export interface UserDto {
  id: string;
  name: string;
  email: string;
  role: number;
  status: number;
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
  vatRate: number;
  stockQty: number;
  stockStatus: number;
  isNew: boolean;
  priorityRank: number;
  images: string[];
  status: number;
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
  status: number;
  paymentStatus: number;
  paymentMethod: number;
  billingAddress: AddressDto;
  shippingAddress: AddressDto;
  shippingMethod: number;
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
  vatRate: number;
}

export interface OrderStatusChangeDto {
  from: number;
  to: number;
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
  status: number;
  type: number;
  relatedInvoiceId: string | null;
}

// ── Messaging ─────────────────────────────────────────

export interface ContactMessageDto {
  id: string;
  email: string;
  subject: string;
  message: string;
  status: number;
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
  role: number;
  content: string;
  timestamp: string;
}

export interface SupportTicketDto {
  id: string;
  conversationId: string | null;
  contactMessageId: string | null;
  email: string;
  subject: string;
  status: number;
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
  // Hero slides
  'hero-1': 'https://images.unsplash.com/photo-1587010580103-fd86b8ea14ca?crop=entropy&cs=tinysrgb&fit=max&fm=jpg&q=80&w=1200',
  'hero-2': 'https://images.unsplash.com/photo-1721114989769-0423619f03d2?crop=entropy&cs=tinysrgb&fit=max&fm=jpg&q=80&w=1200',
  'hero-3': 'https://images.unsplash.com/photo-1758653500328-1c4474a8adfe?crop=entropy&cs=tinysrgb&fit=max&fm=jpg&q=80&w=1200',
  // Products
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
