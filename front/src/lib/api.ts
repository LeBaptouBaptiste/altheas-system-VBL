const API_BASE_URL = process.env.NEXT_PUBLIC_API_URL || 'http://localhost:5207/api';

export class ApiError extends Error {
  constructor(
    public statusCode: number,
    message: string,
    public errors?: Record<string, string[]>
  ) {
    super(message);
    this.name = 'ApiError';
  }
}

/**
 * Thrown when the API returns 403 with `{ reason: "step_up_required" }`.
 * Callers should open the step-up modal, obtain a fresh step-up token, and
 * retry the original request with `{ stepUpToken }` in the options.
 */
export class StepUpRequiredError extends Error {
  constructor() {
    super('Step-up authentication required.');
    this.name = 'StepUpRequiredError';
  }
}

export function isStepUpRequired(err: unknown): err is StepUpRequiredError {
  return err instanceof StepUpRequiredError;
}

/**
 * Thrown when the API returns 429 with `{ reason: "account_locked" }` —
 * the account hit too many failed 2FA attempts. The UI should display the
 * remaining time and disable retry until then.
 */
export class AccountLockedError extends Error {
  constructor(public retryAfterSeconds: number) {
    super(`Account temporarily locked. Try again in ${Math.ceil(retryAfterSeconds / 60)} min.`);
    this.name = 'AccountLockedError';
  }
}

export function isAccountLocked(err: unknown): err is AccountLockedError {
  return err instanceof AccountLockedError;
}

/** True if the message coming from the server is something other than the default placeholder. */
function errorBodyHasMessage(message: string): boolean {
  return message !== 'An unexpected error occurred';
}

/**
 * Per-request overrides. Both fields are optional; omitting them keeps the
 * default behavior (Authorization header drawn from localStorage, no
 * X-Step-Up-Token header).
 */
export interface ApiRequestOptions {
  /**
   * Overrides the bearer token for this single request. Useful for the admin
   * 2FA setup flow, where the access token isn't yet in localStorage and
   * the `setupToken` from /auth/login must be sent instead.
   */
  bearerToken?: string;
  /**
   * Set when retrying a request after a successful step-up — sends the
   * step-up JWT in the X-Step-Up-Token header for the [RequireStepUp] filter.
   */
  stepUpToken?: string;
}

function getToken(): string | null {
  if (typeof window === 'undefined') return null;
  return localStorage.getItem('althea-token');
}

// ── Ambient step-up token ────────────────────────────
// The admin layout sets this when the admin completes the Admin step-up;
// every subsequent API call automatically includes it as X-Step-Up-Token,
// which is required by [RequireStepUp(Admin)] on every /api/admin* endpoint.
// Cleared (set to null) when the admin layout unmounts (= admin leaves the
// admin area). Per-request `stepUpToken` in ApiRequestOptions takes priority
// over this ambient value.

let _ambientStepUpToken: string | null = null;

export function setAmbientStepUpToken(token: string | null): void {
  _ambientStepUpToken = token;
}

export function setToken(token: string): void {
  localStorage.setItem('althea-token', token);
}

export function clearToken(): void {
  localStorage.removeItem('althea-token');
}

async function apiFetch<T>(
  endpoint: string,
  init: RequestInit = {},
  options: ApiRequestOptions = {}
): Promise<T> {
  const bearer = options.bearerToken ?? getToken();

  const headers: Record<string, string> = {
    ...(init.headers as Record<string, string>),
  };

  if (bearer) {
    headers['Authorization'] = `Bearer ${bearer}`;
  }

  // Per-request override wins over the ambient admin token.
  const stepUp = options.stepUpToken ?? _ambientStepUpToken;
  if (stepUp) {
    headers['X-Step-Up-Token'] = stepUp;
  }

  // Set Content-Type for non-GET requests with body
  if (init.body && typeof init.body === 'string') {
    headers['Content-Type'] = 'application/json';
  }

  const response = await fetch(`${API_BASE_URL}${endpoint}`, {
    ...init,
    headers,
  });

  // Handle 204 No Content
  if (response.status === 204) {
    return undefined as T;
  }

  // Handle errors
  if (!response.ok) {
    let message = 'An unexpected error occurred';
    let errors: Record<string, string[]> | undefined;
    let reason: string | undefined;
    let retryAfterSeconds: number | undefined;

    try {
      const errorBody = await response.json();
      message = errorBody.message || message;
      errors = errorBody.errors;
      reason = errorBody.reason;
      if (typeof errorBody.retryAfterSeconds === 'number') {
        retryAfterSeconds = errorBody.retryAfterSeconds;
      }
    } catch {
      // response body not JSON
    }

    // Step-up required: throw a typed error so callers can pop the modal
    // and retry the request with a stepUpToken option.
    if (response.status === 403 && reason === 'step_up_required') {
      throw new StepUpRequiredError();
    }

    // Account locked after too many failed 2FA attempts.
    if (response.status === 429 && reason === 'account_locked') {
      throw new AccountLockedError(retryAfterSeconds ?? 900);
    }

    // Clear token on 401 — but only when the request actually used the
    // localStorage token AND the failure indicates a bad/expired JWT.
    // A 401 with reason="invalid_credentials" means the user typed a wrong
    // code/password, NOT that their session is broken — keep their token.
    if (response.status === 401
        && options.bearerToken === undefined
        && reason !== 'invalid_credentials') {
      clearToken();
    }

    // ASP.NET sends an empty body on 429 by default; surface a friendlier
    // message so the UI can tell the user to wait instead of just "an
    // unexpected error occurred".
    if (response.status === 429 && !errorBodyHasMessage(message)) {
      message = 'Too many requests. Please wait a moment and try again.';
    }

    throw new ApiError(response.status, message, errors);
  }

  const data = await response.json();
  return normalizeEnums(data) as T;
}

// ── Enum normalization ───────────────────────────────
// The API may return enums as strings ("Active") or numbers (0)
// depending on the .NET version/environment. This normalizer
// converts string enum values to their numeric equivalents
// so the front always works with numbers.
//
// NOTE: `outcome` (LoginOutcome) is intentionally NOT mapped here — it
// stays as a string discriminator ('Authenticated' | 'TwoFactorRequired' |
// 'TwoFactorSetupRequired') for readability in the login flow.

const ENUM_STRING_TO_NUMBER: Record<string, Record<string, number>> = {
  role: { Customer: 0, Admin: 1 },
  status: { Active: 0, Inactive: 1, Draft: 2, Pending: 0, Confirmed: 1, Processing: 2, Shipped: 3, Delivered: 4, Cancelled: 5, Returned: 6, Open: 0, InProgress: 1, Resolved: 2, Closed: 3, Unread: 0, Read: 1, Replied: 2, Archived: 3 },
  vatRate: { Standard: 0, Intermediate: 1, Reduced: 2, Zero: 3 },
  stockStatus: { InStock: 0, LowStock: 1, OutOfStock: 2 },
  paymentStatus: { Pending: 0, Paid: 1, Failed: 2, Refunded: 3 },
  paymentMethod: { Card: 0, BankTransfer: 1, PayPal: 2 },
  shippingMethod: { Standard: 0, Express: 1, Overnight: 2 },
  type: { Invoice: 0, CreditNote: 1 },
};

function normalizeEnums(data: unknown): unknown {
  if (data === null || data === undefined) return data;
  if (Array.isArray(data)) return data.map(normalizeEnums);
  if (typeof data === 'object') {
    const obj = data as Record<string, unknown>;
    const result: Record<string, unknown> = {};
    for (const [key, value] of Object.entries(obj)) {
      if (typeof value === 'string' && ENUM_STRING_TO_NUMBER[key]) {
        const mapped = ENUM_STRING_TO_NUMBER[key][value];
        result[key] = mapped !== undefined ? mapped : value;
      } else {
        result[key] = normalizeEnums(value);
      }
    }
    return result;
  }
  return data;
}

export const api = {
  get: <T>(endpoint: string, options?: ApiRequestOptions) =>
    apiFetch<T>(endpoint, {}, options),

  post: <T>(endpoint: string, body?: unknown, options?: ApiRequestOptions) =>
    apiFetch<T>(endpoint, {
      method: 'POST',
      body: body ? JSON.stringify(body) : undefined,
    }, options),

  put: <T>(endpoint: string, body?: unknown, options?: ApiRequestOptions) =>
    apiFetch<T>(endpoint, {
      method: 'PUT',
      body: body ? JSON.stringify(body) : undefined,
    }, options),

  delete: (endpoint: string, options?: ApiRequestOptions) =>
    apiFetch<void>(endpoint, { method: 'DELETE' }, options),
};
