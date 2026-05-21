// ── Shared ────────────────────────────────────────────

export interface LocalizedString {
  [key: string]: string;
  fr: string;
  en: string;
}

/**
 * Build a LocalizedString from up to 4 locales. `fr` and `en` are required
 * (every product/category has them); `ms` and `ar` are optional — when null
 * or undefined, `localized()` falls back to French.
 *
 * Both null and undefined are treated as "no translation" so the seeder can
 * pass null for unset translations and admin update payloads can omit them.
 */
export function toLocalized(
  fr: string,
  en: string,
  ms?: string | null,
  ar?: string | null,
): LocalizedString {
  const out: LocalizedString = { fr, en };
  if (ms) out.ms = ms;
  if (ar) out.ar = ar;
  return out;
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

/**
 * Returned by POST /auth/register since phase 2. Unlike before, registration
 * does NOT issue access / refresh tokens — the user must confirm their email
 * via the link mailed to them and then log in.
 */
export interface RegisterResponse {
  user: UserDto;
}

// LoginOutcome is serialized as a string by the API (JsonStringEnumConverter).
// We keep it as a string union here — `outcome` is NOT in the api.ts
// normalizeEnums mapping, so the value passes through untouched.
export type LoginOutcome =
  | 'Authenticated'
  | 'TwoFactorRequired'
  | 'TwoFactorSetupRequired'
  // Phase 2: password OK but the user hasn't clicked the confirmation link
  // yet. No tokens are issued — the front shows a "check your inbox" screen.
  | 'EmailConfirmationRequired';

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
  /**
   * Phase 4b: populated only when outcome='TwoFactorRequired'.
   * 0 = None (shouldn't happen with that outcome), 1 = Authenticator, 2 = Email.
   * Drives the challenge-screen copy.
   */
  twoFactorMethod: number | null;
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
  /**
   * Phase 4b: which 2FA method the user has configured.
   * 0 = None, 1 = Authenticator, 2 = Email.
   * Drives the security-settings UI and the login challenge copy.
   */
  method: number;
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
  /**
   * Phase 7: store credit balance in cents EUR. Increments on credit notes
   * issued with Mode=StoreCredit; decrements on checkout when applied.
   */
  creditBalanceCents: number;
  /**
   * Two-letter locale for transactional emails: 'fr' | 'en' | 'ms' | 'ar'.
   * Null = system default (French). Editable via account preferences.
   */
  preferredLocale: string | null;
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
  /**
   * Default address — pre-selected at checkout, badged in /account/addresses.
   * Exactly one non-archived address per user holds this flag.
   */
  isDefault: boolean;
}

export interface PaymentMethodDto {
  id: string;
  type: string;
  label: string;
  // Stripe-side identifier for re-charging a saved card. Always set for
  // cards saved through the Stripe flow (commit 18 webhook handler).
  stripePaymentMethodId: string | null;
  brand: string | null;
  last4: string | null;
  expMonth: number | null;
  expYear: number | null;
}

// ── Products ──────────────────────────────────────────

export interface ProductDto {
  id: string;
  slug: string;
  nameFr: string;
  nameEn: string;
  nameMs: string | null;
  nameAr: string | null;
  descriptionFr: string;
  descriptionEn: string;
  descriptionMs: string | null;
  descriptionAr: string | null;
  longDescriptionFr: string;
  longDescriptionEn: string;
  longDescriptionMs: string | null;
  longDescriptionAr: string | null;
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
  nameMs: string | null;
  nameAr: string | null;
}

export interface ProductSpecDto {
  label: string;
  value: string;
  labelEn: string | null;
  labelMs: string | null;
  labelAr: string | null;
  valueEn: string | null;
  valueMs: string | null;
  valueAr: string | null;
}

// ── Categories ────────────────────────────────────────

export interface CategoryDto {
  id: string;
  slug: string;
  nameFr: string;
  nameEn: string;
  nameMs: string | null;
  nameAr: string | null;
  descriptionFr: string;
  descriptionEn: string;
  descriptionMs: string | null;
  descriptionAr: string | null;
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
  // ID of the latest "real" invoice (Type=Invoice, excluding credit notes)
  // attached to this order, or null if none has been issued yet. Used to
  // enable/disable the "Télécharger la facture" button on /account/orders.
  latestInvoiceId: string | null;
  // Phase 6: every invoice + credit note attached to this order (oldest
  // first). /account/orders renders one row per entry with type-aware
  // icon + individual download button.
  invoices: OrderInvoiceSummaryDto[];
  /**
   * Phase 7: store credit applied at checkout (cents EUR). 0 = none.
   * Stripe charged TTC*100 − creditAppliedCents on this order.
   */
  creditAppliedCents: number;
}

export interface OrderInvoiceSummaryDto {
  id: string;
  /** Human-readable number ({ClientCode}-{YYYY}-{MM}-{NNNN}). Shown in UI. */
  number: string;
  /** 0 = Invoice, 1 = CreditNote — matches lib/enums.ts InvoiceType. */
  type: number;
  date: string;
  amountTTC: number;
  status: number;
  /** Set on credit notes — points at the original invoice id. */
  relatedInvoiceId: string | null;
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
  /**
   * Human-readable invoice number in the format
   * `{ClientCode}-{YYYY}-{MM}-{NNNN}` (e.g. "A3F8B2C1-2026-05-0001").
   * Generated by the API at invoice creation; immutable. This is what
   * the UI displays (admin tables, account history, PDF filenames).
   * Credit notes share the same format — distinguish via `type`.
   */
  number: string;
  date: string;
  amountHT: number;
  vatAmount: number;
  amountTTC: number;
  status: number;
  type: number;
  relatedInvoiceId: string | null;
  /**
   * Phase 7: how the credit was settled. 0=Refund (Stripe), 1=StoreCredit.
   * Null for regular invoices.
   */
  mode: number | null;
  /** Phase 7: Stripe refund id (re_xxx) when mode=Refund. */
  stripeRefundId: string | null;
  /** Phase 7: Stripe refund status verbatim ("succeeded" / "pending" / …). */
  refundStatus: string | null;
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
  titleMs: string | null;
  titleAr: string | null;
  subtitleFr: string;
  subtitleEn: string;
  subtitleMs: string | null;
  subtitleAr: string | null;
  descriptionFr: string;
  descriptionEn: string;
  descriptionMs: string | null;
  descriptionAr: string | null;
  ctaFr: string;
  ctaEn: string;
  ctaMs: string | null;
  ctaAr: string | null;
  link: string;
  displayOrder: number;
  active: boolean;
}

export interface StaticPageDto {
  id: string;
  slug: string;
  titleFr: string;
  titleEn: string;
  titleMs: string | null;
  titleAr: string | null;
  contentFr: string;
  contentEn: string;
  contentMs: string | null;
  contentAr: string | null;
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

// 43 unique Unsplash photos curated for the medical-equipment catalog.
// One image per product (32) + per category (8) + hero slide (3). No two
// URLs share the same photo-* prefix — each entry is a distinct shot so
// the homepage / search / category pages don't end up with identical
// thumbnails across half the catalog (which was the case before — only
// 9 images for 32 products, with several products sharing the same URL).
const IMAGE_MAP: Record<string, string> = {
  // ── Hero slides (w=1200) ──────────────────────────────
  'hero-1': 'https://images.unsplash.com/photo-1516549655169-df83a0774514?crop=entropy&cs=tinysrgb&fit=max&fm=jpg&q=80&w=1200',
  'hero-2': 'https://images.unsplash.com/photo-1666214275099-0ca566aefe26?crop=entropy&cs=tinysrgb&fit=max&fm=jpg&q=80&w=1200',
  'hero-3': 'https://images.unsplash.com/photo-1638598124048-10c5b3f2f964?crop=entropy&cs=tinysrgb&fit=max&fm=jpg&q=80&w=1200',

  // ── Categories (w=800) ────────────────────────────────
  'imaging': 'https://images.unsplash.com/photo-1631562501312-044a38befa5c?crop=entropy&cs=tinysrgb&fit=max&fm=jpg&q=80&w=800',
  'monitors': 'https://images.unsplash.com/photo-1587230307094-7ea936b24278?crop=entropy&cs=tinysrgb&fit=max&fm=jpg&q=80&w=800',
  'sterilization': 'https://images.unsplash.com/photo-1612246963308-0441bc24af0e?crop=entropy&cs=tinysrgb&fit=max&fm=jpg&q=80&w=800',
  'surgical': 'https://images.unsplash.com/photo-1560269941-141b145a1b57?crop=entropy&cs=tinysrgb&fit=max&fm=jpg&q=80&w=800',
  'furniture': 'https://images.unsplash.com/photo-1611587266737-cc128ffe2946?crop=entropy&cs=tinysrgb&fit=max&fm=jpg&q=80&w=800',
  'respiratory': 'https://images.unsplash.com/photo-1615486510988-2c6ecc66ceba?crop=entropy&cs=tinysrgb&fit=max&fm=jpg&q=80&w=800',
  'consumables': 'https://images.unsplash.com/photo-1628235176517-71013205a2de?crop=entropy&cs=tinysrgb&fit=max&fm=jpg&q=80&w=800',
  'laboratory': 'https://images.unsplash.com/photo-1614308457932-e16d85c5d053?crop=entropy&cs=tinysrgb&fit=max&fm=jpg&q=80&w=800',

  // ── Products (w=600) — one unique photo per product ───
  // Imaging
  'imaging-1': 'https://images.unsplash.com/photo-1631563020241-09beac7791b7?crop=entropy&cs=tinysrgb&fit=max&fm=jpg&q=80&w=600',
  'imaging-2': 'https://images.unsplash.com/photo-1691933880082-8ca234497bf4?crop=entropy&cs=tinysrgb&fit=max&fm=jpg&q=80&w=600',
  'imaging-3': 'https://images.unsplash.com/photo-1666214280352-db292c05fd80?crop=entropy&cs=tinysrgb&fit=max&fm=jpg&q=80&w=600',
  'imaging-4': 'https://images.unsplash.com/photo-1626878880028-0438b1403b3f?crop=entropy&cs=tinysrgb&fit=max&fm=jpg&q=80&w=600',
  // Monitors & Diagnostics
  'monitors-1': 'https://images.unsplash.com/photo-1630531210974-dab9b07c4eff?crop=entropy&cs=tinysrgb&fit=max&fm=jpg&q=80&w=600',
  'monitors-2': 'https://images.unsplash.com/photo-1700832082200-af7deeb63d9b?crop=entropy&cs=tinysrgb&fit=max&fm=jpg&q=80&w=600',
  'monitors-3': 'https://images.unsplash.com/photo-1598532037823-2f11cc6a866c?crop=entropy&cs=tinysrgb&fit=max&fm=jpg&q=80&w=600',
  'monitors-4': 'https://images.unsplash.com/photo-1657028551158-fd08eb69e15b?crop=entropy&cs=tinysrgb&fit=max&fm=jpg&q=80&w=600',
  // Sterilization & Hygiene
  'sterilization-1': 'https://images.unsplash.com/photo-1720180244494-c56d7f57e2b5?crop=entropy&cs=tinysrgb&fit=max&fm=jpg&q=80&w=600',
  'sterilization-2': 'https://images.unsplash.com/photo-1583912372059-83f2d324c524?crop=entropy&cs=tinysrgb&fit=max&fm=jpg&q=80&w=600',
  'sterilization-3': 'https://images.unsplash.com/photo-1623986854265-3f4f4082c4c2?crop=entropy&cs=tinysrgb&fit=max&fm=jpg&q=80&w=600',
  'sterilization-4': 'https://images.unsplash.com/photo-1624711078613-aa19b0797855?crop=entropy&cs=tinysrgb&fit=max&fm=jpg&q=80&w=600',
  // Surgical instruments
  'surgical-1': 'https://images.unsplash.com/photo-1691935443892-c08791a3e944?crop=entropy&cs=tinysrgb&fit=max&fm=jpg&q=80&w=600',
  'surgical-2': 'https://images.unsplash.com/photo-1593086586351-1673fca190cf?crop=entropy&cs=tinysrgb&fit=max&fm=jpg&q=80&w=600',
  'surgical-3': 'https://images.unsplash.com/photo-1643660527074-0ddcec3bda96?crop=entropy&cs=tinysrgb&fit=max&fm=jpg&q=80&w=600',
  'surgical-4': 'https://images.unsplash.com/photo-1647113412291-6d91b48f7f7c?crop=entropy&cs=tinysrgb&fit=max&fm=jpg&q=80&w=600',
  // Medical furniture
  'furniture-1': 'https://images.unsplash.com/photo-1614101062781-09a8dfb90dce?crop=entropy&cs=tinysrgb&fit=max&fm=jpg&q=80&w=600',
  'furniture-2': 'https://images.unsplash.com/photo-1612037418575-55c54d942c5a?crop=entropy&cs=tinysrgb&fit=max&fm=jpg&q=80&w=600',
  'furniture-3': 'https://images.unsplash.com/photo-1611073061541-842812eaa37c?crop=entropy&cs=tinysrgb&fit=max&fm=jpg&q=80&w=600',
  'furniture-4': 'https://images.unsplash.com/photo-1631507623442-fee09e89c6a8?crop=entropy&cs=tinysrgb&fit=max&fm=jpg&q=80&w=600',
  // Respiratory equipment
  'respiratory-1': 'https://images.unsplash.com/photo-1606166187734-a4cb74079037?crop=entropy&cs=tinysrgb&fit=max&fm=jpg&q=80&w=600',
  'respiratory-2': 'https://images.unsplash.com/photo-1615486510940-4e96763c7f6d?crop=entropy&cs=tinysrgb&fit=max&fm=jpg&q=80&w=600',
  'respiratory-3': 'https://images.unsplash.com/photo-1645273474760-c5f8b0d86d0f?crop=entropy&cs=tinysrgb&fit=max&fm=jpg&q=80&w=600',
  'respiratory-4': 'https://images.unsplash.com/photo-1776104501594-4d4212ba118c?crop=entropy&cs=tinysrgb&fit=max&fm=jpg&q=80&w=600',
  // Consumables (VAT reduced 5.5%)
  'consumables-1': 'https://images.unsplash.com/photo-1598300188480-626f2f79ab8d?crop=entropy&cs=tinysrgb&fit=max&fm=jpg&q=80&w=600',
  'consumables-2': 'https://images.unsplash.com/photo-1552572633-716616a6ad07?crop=entropy&cs=tinysrgb&fit=max&fm=jpg&q=80&w=600',
  'consumables-3': 'https://images.unsplash.com/photo-1544531664-12a4ea82b7d4?crop=entropy&cs=tinysrgb&fit=max&fm=jpg&q=80&w=600',
  'consumables-4': 'https://images.unsplash.com/photo-1651493803684-03a332c42014?crop=entropy&cs=tinysrgb&fit=max&fm=jpg&q=80&w=600',
  // Laboratory equipment
  'laboratory-1': 'https://images.unsplash.com/photo-1582560475213-c396393eab98?crop=entropy&cs=tinysrgb&fit=max&fm=jpg&q=80&w=600',
  'laboratory-2': 'https://images.unsplash.com/photo-1707944745824-c038557dfd7c?crop=entropy&cs=tinysrgb&fit=max&fm=jpg&q=80&w=600',
  'laboratory-3': 'https://images.unsplash.com/photo-1614308459036-779d0dfe51ff?crop=entropy&cs=tinysrgb&fit=max&fm=jpg&q=80&w=600',
  'laboratory-4': 'https://images.unsplash.com/photo-1663363912772-b2f34992c805?crop=entropy&cs=tinysrgb&fit=max&fm=jpg&q=80&w=600',
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
