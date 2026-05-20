import { api } from '@/lib/api';
import type { UserDto, AddressDto, PaymentMethodDto, PaginatedResponse } from '@/lib/api-types';

export const usersService = {
  getAll: (page = 1, pageSize = 20) =>
    api.get<PaginatedResponse<UserDto>>(`/users?page=${page}&pageSize=${pageSize}`),

  getById: (id: string) =>
    api.get<UserDto>(`/users/${id}`),

  update: (id: string, data: { name?: string; email?: string; status?: string }) =>
    api.put<UserDto>(`/users/${id}`, data),

  delete: (id: string) =>
    api.delete(`/users/${id}`),

  /**
   * GDPR self-delete equivalent. Backend requires a step-up Action token —
   * pass it via `stepUpToken` (callers should obtain it via `useStepUp()`).
   */
  anonymize: (id: string, stepUpToken?: string) =>
    api.post<UserDto>(`/users/${id}/anonymize`, undefined, stepUpToken ? { stepUpToken } : undefined),

  // Addresses
  addAddress: (userId: string, data: Omit<AddressDto, 'id'>) =>
    api.post<AddressDto>(`/users/${userId}/addresses`, data),

  updateAddress: (userId: string, addressId: string, data: Omit<AddressDto, 'id'>) =>
    api.put<AddressDto>(`/users/${userId}/addresses/${addressId}`, data),

  deleteAddress: (userId: string, addressId: string) =>
    api.delete(`/users/${userId}/addresses/${addressId}`),

  /**
   * Marks the address as the user's default. Unsets the flag on others
   * atomically. Returns the updated AddressDto with IsDefault=true.
   */
  setDefaultAddress: (userId: string, addressId: string) =>
    api.put<AddressDto>(`/users/${userId}/addresses/${addressId}/default`, {}),

  // Payment methods
  listPaymentMethods: (userId: string) =>
    api.get<PaymentMethodDto[]>(`/users/${userId}/payment-methods`),

  addPaymentMethod: (userId: string, data: { type: string; label: string }) =>
    api.post<PaymentMethodDto>(`/users/${userId}/payment-methods`, data),

  /**
   * Removes a saved card. Backend requires a step-up Action token
   * ([RequireStepUp(Action)]) — pass it via `stepUpToken` (callers should
   * obtain it via `useStepUp()`'s `withStepUp` wrapper).
   */
  deletePaymentMethod: (userId: string, paymentMethodId: string, stepUpToken?: string) =>
    api.delete(
      `/users/${userId}/payment-methods/${paymentMethodId}`,
      stepUpToken ? { stepUpToken } : undefined,
    ),
};
