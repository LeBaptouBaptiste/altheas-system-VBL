'use client';

import { useState, useEffect } from 'react';
import { useRouter } from 'next/navigation';
import { Settings, Package, MapPin, CreditCard, Download, AlertTriangle, Shield, Loader2 } from 'lucide-react';
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
import { ordersService } from '@/lib/api-services';
import type { OrderDto } from '@/lib/api-types';
import { toLocalized } from '@/lib/api-types';
import { formatPrice } from '@/lib/money';
import { OrderStatus, enumLabel } from '@/lib/enums';
import { toast } from 'sonner';

const statusColors: Record<number, string> = {
  [OrderStatus.Pending]: 'bg-warning text-white',
  [OrderStatus.Confirmed]: 'bg-blue-400 text-white',
  [OrderStatus.Processing]: 'bg-brand-primary text-white',
  [OrderStatus.Shipped]: 'bg-blue-500 text-white',
  [OrderStatus.Delivered]: 'bg-success text-white',
  [OrderStatus.Cancelled]: 'bg-error text-white',
  [OrderStatus.Returned]: 'bg-gray-500 text-white',
};

export default function AccountPage() {
  const { t, locale, localized } = useI18n();
  const { user, isAuthenticated, anonymizeAccount, logout, loading: authLoading } = useAuth();
  const router = useRouter();
  const fmt = (n: number) => formatPrice(n, locale === 'fr' ? 'fr-FR' : 'en-US');
  const [userOrders, setUserOrders] = useState<OrderDto[]>([]);
  const [ordersLoading, setOrdersLoading] = useState(true);

  useEffect(() => {
    if (!authLoading && !isAuthenticated) {
      router.push('/login');
    }
  }, [authLoading, isAuthenticated, router]);

  useEffect(() => {
    if (!user) return;
    ordersService.getAll(1, 50)
      .then(res => setUserOrders(res.data.filter(o => o.userId === user.id).sort((a, b) => new Date(b.date).getTime() - new Date(a.date).getTime())))
      .catch(() => {})
      .finally(() => setOrdersLoading(false));
  }, [user]);

  if (authLoading || !user) {
    return <div className="flex items-center justify-center py-32"><Loader2 className="w-8 h-8 animate-spin text-brand-primary" /></div>;
  }

  const ordersByYear = userOrders.reduce((acc, o) => {
    const year = o.date.slice(0, 4);
    (acc[year] = acc[year] || []).push(o);
    return acc;
  }, {} as Record<string, OrderDto[]>);

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
            <div><Label>{t('auth.email')}</Label><Input defaultValue={user.email} /><p className="text-xs text-muted-foreground mt-1">{locale === 'fr' ? 'Modifier l\'email nécessite une confirmation' : 'Changing email requires confirmation'}</p></div>
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
                  <Button variant="destructive" className="flex-1" onClick={async () => { await anonymizeAccount(); toast.success('Account anonymized'); router.push('/'); }}>{t('common.confirm')}</Button>
                </div>
              </DialogContent>
            </Dialog>
          </CardContent></Card>
        </TabsContent>

        {/* Orders */}
        <TabsContent value="orders">
          {ordersLoading ? (
            <div className="flex justify-center py-8"><Loader2 className="w-6 h-6 animate-spin text-brand-primary" /></div>
          ) : (
            <>
              {Object.keys(ordersByYear).sort().reverse().map(year => (
                <div key={year} className="mb-8">
                  <h3 className="text-lg font-semibold mb-4">{year}</h3>
                  <div className="space-y-3">
                    {ordersByYear[year].map(order => (
                      <Card key={order.id}><CardContent className="p-4">
                        <div className="flex items-center justify-between flex-wrap gap-2">
                          <div>
                            <span className="font-medium text-brand-dark">{order.id.slice(0, 8)}...</span>
                            <span className="text-sm text-muted-foreground ml-3">{order.date.slice(0, 10)}</span>
                          </div>
                          <div className="flex items-center gap-3">
                            <Badge className={statusColors[order.status] || 'bg-gray-200'}>{enumLabel('OrderStatus', order.status, locale)}</Badge>
                            <span className="font-bold">{fmt(order.totalTTC)}</span>
                            <Button variant="outline" size="sm" onClick={() => toast.info(locale === 'fr' ? 'Téléchargement de la facture...' : 'Downloading invoice...')}>
                              <Download className="w-3 h-3 mr-1" />{t('account.download_invoice')}
                            </Button>
                          </div>
                        </div>
                        <div className="mt-2 text-sm text-muted-foreground">
                          {order.items.map((item, i) => (
                            <span key={i}>
                              {localized(toLocalized(item.productNameFr, item.productNameEn))} x{item.quantity}
                              {i < order.items.length - 1 ? ', ' : ''}
                            </span>
                          ))}
                        </div>
                      </CardContent></Card>
                    ))}
                  </div>
                </div>
              ))}
              {userOrders.length === 0 && <p className="text-center text-muted-foreground py-8">{t('common.no_data')}</p>}
            </>
          )}
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
          <p className="text-xs text-muted-foreground mt-2">{locale === 'fr' ? 'Aucune donnée sensible n\'est stockée' : 'No sensitive data is stored'}</p>
        </TabsContent>
      </Tabs>
    </div>
  );
}
