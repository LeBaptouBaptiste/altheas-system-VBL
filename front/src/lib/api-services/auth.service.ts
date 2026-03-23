import { api } from '@/lib/api';
import type { AuthResponse, UserDto } from '@/lib/api-types';

export const authService = {
  login: (email: string, password: string) =>
    api.post<AuthResponse>('/auth/login', { email, password }),

  register: (name: string, email: string, password: string, confirmPassword: string) =>
    api.post<AuthResponse>('/auth/register', { name, email, password, confirmPassword }),

  getMe: () =>
    api.get<UserDto>('/auth/me'),

  confirmEmail: (token: string) =>
    api.post<void>('/auth/confirm-email', { token }),

  forgotPassword: (email: string) =>
    api.post<void>('/auth/forgot-password', { email }),

  resetPassword: (token: string, newPassword: string, confirmPassword: string) =>
    api.post<void>('/auth/reset-password', { token, newPassword, confirmPassword }),

  verify2fa: (code: string) =>
    api.post<void>('/auth/verify-2fa', { code }),
};
