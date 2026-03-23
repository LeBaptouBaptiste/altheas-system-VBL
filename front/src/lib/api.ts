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

  return response.json();
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
