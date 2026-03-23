import type { SalesAnalytics } from './types';

// Last 7 days analytics
export const dailyAnalytics: SalesAnalytics[] = [
  { date: '2026-02-12', revenue: 5760, orders: 1, categoryBreakdown: { 'cat-4': 5760 } },
  { date: '2026-02-13', revenue: 0, orders: 0, categoryBreakdown: {} },
  { date: '2026-02-14', revenue: 11520, orders: 1, categoryBreakdown: { 'cat-6': 11520 } },
  { date: '2026-02-15', revenue: 79080, orders: 1, categoryBreakdown: { 'cat-5': 78000, 'cat-2': 1080 } },
  { date: '2026-02-16', revenue: 6942.75, orders: 1, categoryBreakdown: { 'cat-6': 6600, 'cat-3': 342.75 } },
  { date: '2026-02-17', revenue: 20400, orders: 1, categoryBreakdown: { 'cat-3': 20400 } },
  { date: '2026-02-18', revenue: 54187, orders: 1, categoryBreakdown: { 'cat-1': 54000, 'cat-5': 187 } },
];

// Last 5 weeks analytics
export const weeklyAnalytics: SalesAnalytics[] = [
  { date: '2026-01-20', revenue: 28500, orders: 5, categoryBreakdown: { 'cat-1': 12000, 'cat-3': 8500, 'cat-5': 4500, 'cat-8': 3500 } },
  { date: '2026-01-27', revenue: 45200, orders: 8, categoryBreakdown: { 'cat-2': 15000, 'cat-6': 12200, 'cat-4': 10000, 'cat-7': 8000 } },
  { date: '2026-02-03', revenue: 12905.60, orders: 3, categoryBreakdown: { 'cat-5': 5534.60, 'cat-8': 1371, 'cat-4': 6000 } },
  { date: '2026-02-10', revenue: 78960, orders: 4, categoryBreakdown: { 'cat-3': 67200, 'cat-1': 5760, 'cat-4': 6000 } },
  { date: '2026-02-17', revenue: 172129.75, orders: 5, categoryBreakdown: { 'cat-1': 54187, 'cat-3': 20400, 'cat-5': 79080, 'cat-6': 18462.75 } },
];

// Aggregated KPIs
export const kpis = {
  revenueToday: 54187,
  revenueWeek: 172129.75,
  revenueMonth: 337895.35,
  ordersToday: 1,
  stockAlerts: 5, // products with low_stock or out_of_stock
  unreadMessages: 5,
};
