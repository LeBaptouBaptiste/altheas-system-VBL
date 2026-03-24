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

function getToken(): string | null {
  if (typeof window === 'undefined') return null;
  return localStorage.getItem('althea-token');
}

export function setToken(token: string): void {
  localStorage.setItem('althea-token', token);
}

export function clearToken(): void {
  localStorage.removeItem('althea-token');
}

async function apiFetch<T>(
  endpoint: string,
  options: RequestInit = {}
): Promise<T> {
  const token = getToken();

  const headers: Record<string, string> = {
    ...(options.headers as Record<string, string>),
  };

  if (token) {
    headers['Authorization'] = `Bearer ${token}`;
  }

  // Set Content-Type for non-GET requests with body
  if (options.body && typeof options.body === 'string') {
    headers['Content-Type'] = 'application/json';
  }

  const response = await fetch(`${API_BASE_URL}${endpoint}`, {
    ...options,
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

    try {
      const errorBody = await response.json();
      message = errorBody.message || message;
      errors = errorBody.errors;
    } catch {
      // response body not JSON
    }

    // Clear token on 401
    if (response.status === 401) {
      clearToken();
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
  get: <T>(endpoint: string) =>
    apiFetch<T>(endpoint),

  post: <T>(endpoint: string, body?: unknown) =>
    apiFetch<T>(endpoint, {
      method: 'POST',
      body: body ? JSON.stringify(body) : undefined,
    }),

  put: <T>(endpoint: string, body?: unknown) =>
    apiFetch<T>(endpoint, {
      method: 'PUT',
      body: body ? JSON.stringify(body) : undefined,
    }),

  delete: (endpoint: string) =>
    apiFetch<void>(endpoint, { method: 'DELETE' }),
};
