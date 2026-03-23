import { api } from '@/lib/api';
import type { HeroSlideDto, StaticPageDto } from '@/lib/api-types';

export const contentService = {
  // Hero slides / Carousel
  getSlides: (activeOnly = true) =>
    api.get<HeroSlideDto[]>(`/carousel?activeOnly=${activeOnly}`),

  getSlide: (id: string) =>
    api.get<HeroSlideDto>(`/carousel/${id}`),

  createSlide: (data: Record<string, unknown>) =>
    api.post<HeroSlideDto>('/carousel', data),

  updateSlide: (id: string, data: Record<string, unknown>) =>
    api.put<HeroSlideDto>(`/carousel/${id}`, data),

  deleteSlide: (id: string) =>
    api.delete(`/carousel/${id}`),

  // Static pages
  getPages: () =>
    api.get<StaticPageDto[]>('/pages'),

  getPage: (id: string) =>
    api.get<StaticPageDto>(`/pages/${id}`),

  getPageBySlug: (slug: string) =>
    api.get<StaticPageDto>(`/pages/slug/${slug}`),

  createPage: (data: Record<string, unknown>) =>
    api.post<StaticPageDto>('/pages', data),

  updatePage: (id: string, data: Record<string, unknown>) =>
    api.put<StaticPageDto>(`/pages/${id}`, data),

  deletePage: (id: string) =>
    api.delete(`/pages/${id}`),
};
