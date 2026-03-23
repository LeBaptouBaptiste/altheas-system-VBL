import { api } from '@/lib/api';
import type { CategoryDto } from '@/lib/api-types';

export const categoriesService = {
  getAll: () =>
    api.get<CategoryDto[]>('/categories'),

  getById: (id: string) =>
    api.get<CategoryDto>(`/categories/${id}`),

  getBySlug: (slug: string) =>
    api.get<CategoryDto>(`/categories/slug/${slug}`),

  create: (data: Record<string, unknown>) =>
    api.post<CategoryDto>('/categories', data),

  update: (id: string, data: Record<string, unknown>) =>
    api.put<CategoryDto>(`/categories/${id}`, data),

  delete: (id: string) =>
    api.delete(`/categories/${id}`),
};
