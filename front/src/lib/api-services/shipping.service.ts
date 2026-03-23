import { api } from '@/lib/api';
import type { ShippingMethodDto } from '@/lib/api-types';

export const shippingService = {
  getMethods: () =>
    api.get<ShippingMethodDto[]>('/shipping/methods'),
};
