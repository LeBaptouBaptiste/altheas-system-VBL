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

  anonymize: (id: string) =>
    api.post<UserDto>(`/users/${id}/anonymize`),

  // Addresses
  addAddress: (userId: string, data: Omit<AddressDto, 'id'>) =>
    api.post<AddressDto>(`/users/${userId}/addresses`, data),

  updateAddress: (userId: string, addressId: string, data: Omit<AddressDto, 'id'>) =>
    api.put<AddressDto>(`/users/${userId}/addresses/${addressId}`, data),

  deleteAddress: (userId: string, addressId: string) =>
    api.delete(`/users/${userId}/addresses/${addressId}`),

  // Payment methods
  addPaymentMethod: (userId: string, data: { type: string; label: string }) =>
    api.post<PaymentMethodDto>(`/users/${userId}/payment-methods`, data),

  deletePaymentMethod: (userId: string, paymentMethodId: string) =>
    api.delete(`/users/${userId}/payment-methods/${paymentMethodId}`),
};
