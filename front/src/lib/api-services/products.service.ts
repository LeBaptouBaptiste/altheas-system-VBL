import { api } from '@/lib/api';
import type { ProductDto, PaginatedResponse } from '@/lib/api-types';

export const productsService = {
  getAll: (page = 1, pageSize = 12, categoryId?: string) => {
    let endpoint = `/products?page=${page}&pageSize=${pageSize}`;
    if (categoryId) endpoint += `&categoryId=${categoryId}`;
    return api.get<PaginatedResponse<ProductDto>>(endpoint);
  },

  getById: (id: string) =>
    api.get<ProductDto>(`/products/${id}`),

  getBySlug: (slug: string) =>
    api.get<ProductDto>(`/products/slug/${slug}`),

  search: (q: string, page = 1, pageSize = 12) =>
    api.get<PaginatedResponse<ProductDto>>(`/products/search?q=${encodeURIComponent(q)}&page=${page}&pageSize=${pageSize}`),

  create: (data: Record<string, unknown>) =>
    api.post<ProductDto>('/products', data),

  update: (id: string, data: Record<string, unknown>) =>
    api.put<ProductDto>(`/products/${id}`, data),

  delete: (id: string) =>
    api.delete(`/products/${id}`),
};
