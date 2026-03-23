import { api } from '@/lib/api';
import type { DashboardKpiDto, SalesAnalyticsDto } from '@/lib/api-types';

export const analyticsService = {
  getDashboard: () =>
    api.get<DashboardKpiDto>('/analytics/dashboard'),

  getSales: (from?: string, to?: string) => {
    let endpoint = '/analytics/sales';
    const params = new URLSearchParams();
    if (from) params.set('from', from);
    if (to) params.set('to', to);
    if (params.toString()) endpoint += `?${params}`;
    return api.get<SalesAnalyticsDto[]>(endpoint);
  },
};
