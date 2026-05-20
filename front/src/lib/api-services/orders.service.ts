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
    /**
     * Phase 7: store credit to apply at checkout (cents EUR). 0 or omitted = none.
     * Server caps at user.creditBalanceCents and refuses if it would leave
     * the Stripe charge under 0.50 € (reason='credit_too_large').
     */
    creditAppliedCents?: number;
  }) =>
    api.post<OrderDto>('/orders', data),

  updateStatus: (id: string, status: number) =>
    api.put<OrderDto>(`/orders/${id}/status`, { status }),
};
