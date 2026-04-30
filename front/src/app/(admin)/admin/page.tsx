'use client';

import { useState, useEffect } from 'react';
import Link from 'next/link';
import { DollarSign, ShoppingCart, AlertTriangle, Mail, Plus, Package, Eye, Download, Loader2 } from 'lucide-react';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { useI18n } from '@/context/i18n-context';
import { analyticsService, categoriesService } from '@/lib/api-services';
import type { DashboardKpiDto, SalesAnalyticsDto, CategoryDto } from '@/lib/api-types';
import { toLocalized } from '@/lib/api-types';
import { formatPrice } from '@/lib/money';
import { PieChart, Pie, Cell, BarChart, Bar, XAxis, YAxis, Tooltip, ResponsiveContainer, CartesianGrid } from 'recharts';

const COLORS = ['#00A8B5', '#33BFC9', '#003D5C', '#10B981', '#F59E0B', '#EF4444', '#8B5CF6', '#EC4899'];

export default function AdminDashboard() {
  const { t, locale, localized } = useI18n();
  const fmt = (n: number) => formatPrice(n, locale === 'fr' ? 'fr-FR' : 'en-US');

  const [kpis, setKpis] = useState<DashboardKpiDto | null>(null);
  const [categories, setCategories] = useState<CategoryDto[]>([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    const load = async () => {
      try {
        const [dashboardData, cats] = await Promise.all([
          analyticsService.getDashboard(),
          categoriesService.getAll(),
        ]);
        setKpis(dashboardData);
        setCategories(cats);
      } catch (err) {
        console.error('Failed to load dashboard data', err);
      } finally {
        setLoading(false);
      }
    };
    load();
  }, []);

  if (loading || !kpis) {
    return (
      <div className="flex items-center justify-center h-64">
        <Loader2 className="w-8 h-8 animate-spin text-brand-primary" />
      </div>
    );
  }

  // KPI cards
  const kpiCards = [
    { label: t('admin.revenue_today'), value: fmt(kpis.totalRevenue), icon: DollarSign, color: 'text-success' },
    { label: t('admin.revenue_week'), value: fmt(kpis.averageOrderValue), icon: DollarSign, color: 'text-brand-primary' },
    { label: t('admin.revenue_month'), value: fmt(kpis.totalRevenue), icon: DollarSign, color: 'text-brand-dark' },
    { label: t('admin.orders_today'), value: String(kpis.totalOrders), icon: ShoppingCart, color: 'text-brand-primary' },
    { label: t('admin.stock_alerts'), value: String(kpis.totalProducts), icon: AlertTriangle, color: 'text-warning' },
    { label: t('admin.unread_messages'), value: String(kpis.totalCustomers), icon: Mail, color: 'text-error' },
  ];

  // Pie chart data: aggregate category breakdown from daily sales
  const catBreakdown: Record<string, number> = {};
  kpis.dailySales.forEach(d => {
    Object.entries(d.categoryBreakdown).forEach(([catId, amount]) => {
      catBreakdown[catId] = (catBreakdown[catId] || 0) + amount;
    });
  });
  const pieData = Object.entries(catBreakdown)
    .map(([catId, value]) => {
      const cat = categories.find(c => c.id === catId);
      return { name: cat ? localized(toLocalized(cat.nameFr, cat.nameEn)) : catId, value };
    })
    .sort((a, b) => b.value - a.value);

  // Bar chart: daily revenue
  const barData = kpis.dailySales.map(d => ({
    date: new Date(d.date).toLocaleDateString(locale === 'fr' ? 'fr-FR' : 'en-US', { weekday: 'short', day: 'numeric' }),
    revenue: d.revenue,
  }));

  return (
    <div className="space-y-6">
      {/* KPI Grid */}
      <div className="grid grid-cols-2 lg:grid-cols-3 xl:grid-cols-6 gap-4">
        {kpiCards.map((kpi, i) => {
          const Icon = kpi.icon;
          return (
            <Card key={i}>
              <CardContent className="p-4">
                <div className="flex items-center justify-between mb-2">
                  <Icon className={`w-5 h-5 ${kpi.color}`} />
                </div>
                <p className="text-2xl font-bold text-brand-dark">{kpi.value}</p>
                <p className="text-xs text-muted-foreground mt-1">{kpi.label}</p>
              </CardContent>
            </Card>
          );
        })}
      </div>

      {/* Charts */}
      <div className="grid lg:grid-cols-2 gap-6">
        {/* Pie Chart */}
        <Card>
          <CardHeader>
            <CardTitle className="text-base">{t('admin.sales_by_category')}</CardTitle>
            <p className="text-xs text-muted-foreground">{t('admin.last_5_weeks')}</p>
          </CardHeader>
          <CardContent>
            <div className="h-[280px]">
              <ResponsiveContainer width="100%" height="100%">
                <PieChart>
                  <Pie data={pieData} dataKey="value" nameKey="name" cx="50%" cy="50%" outerRadius={90} label={({ name, percent }: { name?: string; percent?: number }) => `${(name || '').split(' ')[0]} ${((percent || 0) * 100).toFixed(0)}%`} labelLine={false} fontSize={11}>
                    {pieData.map((_, i) => <Cell key={i} fill={COLORS[i % COLORS.length]} />)}
                  </Pie>
                  <Tooltip formatter={(value) => fmt(Number(value))} />
                </PieChart>
              </ResponsiveContainer>
            </div>
          </CardContent>
        </Card>

        {/* Bar Chart */}
        <Card>
          <CardHeader>
            <CardTitle className="text-base">{t('admin.sales_by_day')}</CardTitle>
            <p className="text-xs text-muted-foreground">{t('admin.last_7_days')}</p>
          </CardHeader>
          <CardContent>
            <div className="h-[280px]">
              <ResponsiveContainer width="100%" height="100%">
                <BarChart data={barData}>
                  <CartesianGrid strokeDasharray="3 3" stroke="#f0f0f0" />
                  <XAxis dataKey="date" fontSize={11} />
                  <YAxis fontSize={11} tickFormatter={(v) => `${(v / 1000).toFixed(0)}k`} />
                  <Tooltip formatter={(value) => fmt(Number(value))} />
                  <Bar dataKey="revenue" fill="#00A8B5" radius={[4, 4, 0, 0]} />
                </BarChart>
              </ResponsiveContainer>
            </div>
          </CardContent>
        </Card>
      </div>

      {/* Quick Actions */}
      <Card>
        <CardHeader>
          <CardTitle className="text-base">{t('admin.quick_actions')}</CardTitle>
        </CardHeader>
        <CardContent>
          <div className="flex flex-wrap gap-3">
            <Link href="/admin/products?action=new">
              <Button size="sm" className="bg-brand-primary hover:bg-brand-hover text-white">
                <Plus className="w-4 h-4 me-1" />{t('admin.add_product')}
              </Button>
            </Link>
            <Link href="/admin/orders">
              <Button size="sm" variant="outline">
                <ShoppingCart className="w-4 h-4 me-1" />{t('admin.orders')}
              </Button>
            </Link>
            <Link href="/admin/messages">
              <Button size="sm" variant="outline">
                <Eye className="w-4 h-4 me-1" />{t('admin.view_messages')}
              </Button>
            </Link>
            <Button size="sm" variant="outline" onClick={() => {
              const csv = 'Date,Revenue,Orders\n' + kpis.dailySales.map(d => `${d.date},${d.revenue},${d.orderCount}`).join('\n');
              const blob = new Blob([csv], { type: 'text/csv' });
              const url = URL.createObjectURL(blob);
              const a = document.createElement('a');
              a.href = url; a.download = 'analytics.csv'; a.click();
              URL.revokeObjectURL(url);
            }}>
              <Download className="w-4 h-4 me-1" />{t('admin.export_csv')}
            </Button>
          </div>
        </CardContent>
      </Card>
    </div>
  );
}
