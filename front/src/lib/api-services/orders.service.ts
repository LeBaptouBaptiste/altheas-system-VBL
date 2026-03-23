import { api } from '@/lib/api';
import type { OrderDto, PaginatedResponse } from '@/lib/api-types';

export const ordersService = {
  getAll: (page = 1, pageSize = 20, userId?: string) => {
    let endpoint = `/orders?page=${page}&pageSize=${pageSize}`;
    if (userId) endpoint += `&userId=${userId}`;
    return api.get<PaginatedResponse<OrderDto>>(endpoint);
  },

  getById: (id: string) =>
    api.get<OrderDto>(`/orders/${id}`),

  create: (data: {
    billingAddressId: string;
    shippingAddressId: string;
    shippingMethod: number;
    paymentMethod: number;
    items: { productId: string; quantity: number }[];
  }) =>
    api.post<OrderDto>('/orders', data),

  updateStatus: (id: string, status: number) =>
    api.put<OrderDto>(`/orders/${id}/status`, { status }),
};
