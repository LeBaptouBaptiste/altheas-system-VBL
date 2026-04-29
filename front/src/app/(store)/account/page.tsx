'use client';

import { useState } from 'react';
import Link from 'next/link';
import { useRouter } from 'next/navigation';
import { Settings, Package, MapPin, CreditCard, Download, AlertTriangle, Shield } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Card, CardContent } from '@/components/ui/card';
import { Badge } from '@/components/ui/badge';
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/components/ui/tabs';
import { Separator } from '@/components/ui/separator';
import { Dialog, DialogContent, DialogHeader, DialogTitle, DialogTrigger } from '@/components/ui/dialog';
import { useI18n } from '@/context/i18n-context';
import { useAuth } from '@/context/auth-context';
import { orders, products, invoices } from '@/mock';
import { formatPrice, calculateTTC, toIntlLocale } from '@/lib/money';
import { VAT_RATES } from '@/lib/constants';
import { toast } from 'sonner';

const statusColors: Record<string, string> = {
  pending: 'bg-warning text-white',
  processing: 'bg-brand-primary text-white',
  shipped: 'bg-blue-500 text-white',
  delivered: 'bg-success text-white',
  cancelled: 'bg-error text-white',
};

export default function AccountPage() {
  const { t, locale, localized } = useI18n();
  const { user, isAuthenticated, updateUser, anonymizeAccount, logout } = useAuth();
  const router = useRouter();
  const fmt = (n: number) => formatPrice(n, toIntlLocale(locale));

  if (!isAuthenticated || !user) {
    router.push('/login');
    return null;
  }

  const userOrders = orders.filter(o => o.userId === user.id).sort((a, b) => new Date(b.date).getTime() - new Date(a.date).getTime());
  const ordersByYear = userOrders.reduce((acc, o) => {
    const year = o.date.slice(0, 4);
    (acc[year] = acc[year] || []).push(o);
    return acc;
  }, {} as Record<string, typeof userOrders>);

  return (
    <div className="container mx-auto px-4 py-8 max-w-4xl">
      <h1 className="text-3xl text-brand-dark mb-2">{t('account.title')}</h1>
      <p className="text-muted-foreground mb-8">{user.email}</p>

      <Tabs defaultValue="settings">
        <TabsList className="mb-6 flex-wrap">
          <TabsTrigger value="settings"><Settings className="w-4 h-4 mr-1" />{t('account.settings')}</TabsTrigger>
          <TabsTrigger value="orders"><Package className="w-4 h-4 mr-1" />{t('account.orders')}</TabsTrigger>
          <TabsTrigger value="addresses"><MapPin className="w-4 h-4 mr-1" />{t('account.addresses')}</TabsTrigger>
          <TabsTrigger value="payments"><CreditCard className="w-4 h-4 mr-1" />{t('account.payment_methods')}</TabsTrigger>
        </TabsList>

        {/* Settings */}
        <TabsContent value="settings">
          <Card><CardContent className="p-6 space-y-4">
            <div><Label>{t('auth.full_name')}</Label><Input defaultValue={user.name} /></div>
            <div><Label>{t('auth.email')}</Label><Input defaultValue={user.email} /><p className="text-xs text-muted-foreground mt-1">{{ fr: 'Modifier l\'email nécessite une confirmation', en: 'Changing email requires confirmation', ms: 'Menukar e-mel memerlukan pengesahan', ar: 'تغيير البريد الإلكتروني يتطلب تأكيداً' }[locale] ?? 'Changing email requires confirmation'}</p></div>
            <Separator />
            <div><Label>{t('auth.password')}</Label><Input type="password" placeholder="••••••••" /><p className="text-xs text-muted-foreground mt-1">{t('auth.password_rules')}</p></div>
            <Button className="bg-brand-primary hover:bg-brand-hover text-white" onClick={() => toast.success(t('account.save') + ' ✓')}>{t('account.save')}</Button>
            <Separator />
            <Dialog>
              <DialogTrigger asChild>
                <Button variant="destructive" className="mt-4"><AlertTriangle className="w-4 h-4 mr-2" />{t('account.anonymize')}</Button>
              </DialogTrigger>
              <DialogContent>
                <DialogHeader><DialogTitle>{t('account.anonymize')}</DialogTitle></DialogHeader>
                <p className="text-sm text-muted-foreground">{t('account.anonymize_warning')}</p>
                <div className="flex gap-4 mt-4">
                  <Button variant="outline" className="flex-1">{t('common.cancel')}</Button>
                  <Button variant="destructive" className="flex-1" onClick={() => { anonymizeAccount(); toast.success('Account anonymized'); router.push('/'); }}>{t('common.confirm')}</Button>
                </div>
              </DialogContent>
            </Dialog>
          </CardContent></Card>
        </TabsContent>

        {/* Orders */}
        <TabsContent value="orders">
          {Object.keys(ordersByYear).sort().reverse().map(year => (
            <div key={year} className="mb-8">
              <h3 className="text-lg font-semibold mb-4">{year}</h3>
              <div className="space-y-3">
                {ordersByYear[year].map(order => {
                  const total = order.items.reduce((s, i) => s + calculateTTC(i.priceHT, VAT_RATES[i.vatRate]) * i.quantity, 0) + order.shippingCost;
                  return (
                    <Card key={order.id}><CardContent className="p-4">
                      <div className="flex items-center justify-between flex-wrap gap-2">
                        <div>
                          <span className="font-medium text-brand-dark">{order.id}</span>
                          <span className="text-sm text-muted-foreground ml-3">{order.date}</span>
                        </div>
                        <div className="flex items-center gap-3">
                          <Badge className={statusColors[order.status] || 'bg-gray-200'}>{order.status}</Badge>
                          <span className="font-bold">{fmt(total)}</span>
                          <Button variant="outline" size="sm" onClick={() => toast.info('Invoice PDF (mock)')}>
                            <Download className="w-3 h-3 mr-1" />{t('account.download_invoice')}
                          </Button>
                        </div>
                      </div>
                      <div className="mt-2 text-sm text-muted-foreground">
                        {order.items.map((item, i) => (
                          <span key={i}>{localized(item.productName)} x{item.quantity}{i < order.items.length - 1 ? ', ' : ''}</span>
                        ))}
                      </div>
                    </CardContent></Card>
                  );
                })}
              </div>
            </div>
          ))}
          {userOrders.length === 0 && <p className="text-center text-muted-foreground py-8">{t('common.no_data')}</p>}
        </TabsContent>

        {/* Addresses */}
        <TabsContent value="addresses">
          <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
            {user.addresses.map(addr => (
              <Card key={addr.id}><CardContent className="p-4">
                <h3 className="font-medium mb-2">{addr.label}</h3>
                <p className="text-sm text-muted-foreground">{addr.firstName} {addr.lastName}</p>
                {addr.company && <p className="text-sm text-muted-foreground">{addr.company}</p>}
                <p className="text-sm text-muted-foreground">{addr.street}</p>
                <p className="text-sm text-muted-foreground">{addr.postalCode} {addr.city}, {addr.country}</p>
                <div className="flex gap-2 mt-3">
                  <Button variant="outline" size="sm">{t('common.edit')}</Button>
                  <Button variant="outline" size="sm" className="text-error">{t('common.delete')}</Button>
                </div>
              </CardContent></Card>
            ))}
          </div>
          <Button variant="outline" className="mt-4">{t('account.add_address')}</Button>
        </TabsContent>

        {/* Payment Methods */}
        <TabsContent value="payments">
          <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
            {user.paymentMethods.map(pm => (
              <Card key={pm.id}><CardContent className="p-4 flex items-center justify-between">
                <div className="flex items-center gap-3">
                  <Shield className="w-5 h-5 text-brand-primary" />
                  <span className="font-medium">{pm.label}</span>
                </div>
                <Button variant="outline" size="sm" className="text-error">{t('common.delete')}</Button>
              </CardContent></Card>
            ))}
          </div>
          <Button variant="outline" className="mt-4">{t('account.add_payment')}</Button>
          <p className="text-xs text-muted-foreground mt-2">{{ fr: 'Aucune donnée sensible n\'est stockée', en: 'No sensitive data is stored', ms: 'Tiada data sensitif disimpan', ar: 'لا يتم تخزين أي بيانات حساسة' }[locale] ?? 'No sensitive data is stored'}</p>
        </TabsContent>
      </Tabs>
    </div>
  );
}
