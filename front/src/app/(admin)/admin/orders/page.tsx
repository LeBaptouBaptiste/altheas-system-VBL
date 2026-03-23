'use client';

import { useState, useEffect, useMemo } from 'react';
import { Search, ChevronDown, Clock, Loader2 } from 'lucide-react';
import { Card, CardContent } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Badge } from '@/components/ui/badge';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';
import { Dialog, DialogContent, DialogHeader, DialogTitle } from '@/components/ui/dialog';
import { Separator } from '@/components/ui/separator';
import { useI18n } from '@/context/i18n-context';
import { ordersService } from '@/lib/api-services';
import type { OrderDto } from '@/lib/api-types';
import { toLocalized } from '@/lib/api-types';
import { formatPrice } from '@/lib/money';
import { toast } from 'sonner';

const VAT_RATE_VALUES: Record<string, number> = { Standard: 0.20, Intermediate: 0.10, Reduced: 0.055, Zero: 0 };

const STATUS_COLORS: Record<string, string> = {
  Pending: 'bg-warning/10 text-warning border-warning/20',
  Confirmed: 'bg-blue-50 text-blue-600 border-blue-200',
  Processing: 'bg-blue-50 text-blue-600 border-blue-200',
  Shipped: 'bg-brand-primary/10 text-brand-primary border-brand-primary/20',
  Delivered: 'bg-success/10 text-success border-success/20',
  Cancelled: 'bg-error/10 text-error border-error/20',
  Returned: 'bg-gray-100 text-muted-foreground',
};

const PAYMENT_COLORS: Record<string, string> = {
  Paid: 'text-success',
  Pending: 'text-warning',
  Failed: 'text-error',
  Refunded: 'text-muted-foreground',
};

type OrderStatusValue = OrderDto['status'];

export default function AdminOrdersPage() {
  const { locale, localized } = useI18n();
  const fmt = (n: number) => formatPrice(n, locale === 'fr' ? 'fr-FR' : 'en-US');

  const [ordersList, setOrdersList] = useState<OrderDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [search, setSearch] = useState('');
  const [statusFilter, setStatusFilter] = useState('all');
  const [detailOrder, setDetailOrder] = useState<OrderDto | null>(null);
  const [page, setPage] = useState(1);
  const pageSize = 10;

  useEffect(() => {
    const load = async () => {
      try {
        const res = await ordersService.getAll(1, 200);
        setOrdersList(res.data);
      } catch (err) {
        console.error('Failed to load orders', err);
      } finally {
        setLoading(false);
      }
    };
    load();
  }, []);

  const filtered = useMemo(() => {
    let list = [...ordersList].sort((a, b) => b.createdAt.localeCompare(a.createdAt));
    if (search) {
      const q = search.toLowerCase();
      list = list.filter(o => o.id.toLowerCase().includes(q) || o.userName.toLowerCase().includes(q));
    }
    if (statusFilter !== 'all') list = list.filter(o => o.status === statusFilter);
    return list;
  }, [ordersList, search, statusFilter]);

  const paged = filtered.slice((page - 1) * pageSize, page * pageSize);
  const totalPages = Math.ceil(filtered.length / pageSize);

  const handleStatusChange = async (orderId: string, newStatus: OrderStatusValue) => {
    try {
      const updated = await ordersService.updateStatus(orderId, newStatus);
      setOrdersList(prev => prev.map(o => o.id === orderId ? updated : o));
      if (detailOrder?.id === orderId) {
        setDetailOrder(updated);
      }
      toast.success(locale === 'fr' ? 'Statut mis à jour' : 'Status updated');
    } catch (err) {
      console.error('Failed to update order status', err);
      toast.error(locale === 'fr' ? 'Erreur lors de la mise à jour' : 'Failed to update status');
    }
  };

  const statusLabel = (s: string) => {
    const labels: Record<string, Record<string, string>> = {
      Pending: { fr: 'En attente', en: 'Pending' },
      Confirmed: { fr: 'Confirmée', en: 'Confirmed' },
      Processing: { fr: 'En traitement', en: 'Processing' },
      Shipped: { fr: 'Expédiée', en: 'Shipped' },
      Delivered: { fr: 'Livrée', en: 'Delivered' },
      Cancelled: { fr: 'Annulée', en: 'Cancelled' },
      Returned: { fr: 'Retournée', en: 'Returned' },
    };
    return labels[s]?.[locale] || s;
  };

  if (loading) {
    return (
      <div className="flex items-center justify-center h-64">
        <Loader2 className="w-8 h-8 animate-spin text-brand-primary" />
      </div>
    );
  }

  return (
    <div className="space-y-4">
      <div className="flex flex-wrap items-center gap-3">
        <div className="relative flex-1 min-w-[200px]">
          <Search className="absolute left-3 top-1/2 -translate-y-1/2 w-4 h-4 text-muted-foreground" />
          <Input value={search} onChange={e => setSearch(e.target.value)} placeholder={locale === 'fr' ? 'Rechercher par n° ou client...' : 'Search by # or customer...'} className="pl-9" />
        </div>
        <Select value={statusFilter} onValueChange={setStatusFilter}>
          <SelectTrigger className="w-[160px]"><SelectValue /></SelectTrigger>
          <SelectContent>
            <SelectItem value="all">{locale === 'fr' ? 'Tous statuts' : 'All statuses'}</SelectItem>
            <SelectItem value="Pending">{statusLabel('Pending')}</SelectItem>
            <SelectItem value="Processing">{statusLabel('Processing')}</SelectItem>
            <SelectItem value="Shipped">{statusLabel('Shipped')}</SelectItem>
            <SelectItem value="Delivered">{statusLabel('Delivered')}</SelectItem>
            <SelectItem value="Cancelled">{statusLabel('Cancelled')}</SelectItem>
          </SelectContent>
        </Select>
      </div>

      <Card>
        <CardContent className="p-0">
          <div className="overflow-x-auto">
            <table className="w-full text-sm">
              <thead>
                <tr className="border-b bg-gray-50">
                  <th className="p-3 text-left">{locale === 'fr' ? 'Commande' : 'Order'}</th>
                  <th className="p-3 text-left">Client</th>
                  <th className="p-3 text-left">Date</th>
                  <th className="p-3 text-right">Total TTC</th>
                  <th className="p-3 text-center">{locale === 'fr' ? 'Paiement' : 'Payment'}</th>
                  <th className="p-3 text-center">Status</th>
                  <th className="p-3 text-right">Actions</th>
                </tr>
              </thead>
              <tbody>
                {paged.map(order => (
                  <tr key={order.id} className="border-b hover:bg-gray-50/50">
                    <td className="p-3">
                      <button onClick={() => setDetailOrder(order)} className="text-brand-primary hover:underline font-medium">
                        #{order.id.split('-').pop()}
                      </button>
                    </td>
                    <td className="p-3">
                      <span className="text-brand-dark">{order.userName}</span>
                    </td>
                    <td className="p-3 text-muted-foreground">
                      {new Date(order.date).toLocaleDateString(locale === 'fr' ? 'fr-FR' : 'en-US')}
                    </td>
                    <td className="p-3 text-right font-medium">{fmt(order.totalTTC)}</td>
                    <td className="p-3 text-center">
                      <span className={`text-xs font-medium ${PAYMENT_COLORS[order.paymentStatus] || ''}`}>
                        {order.paymentStatus}
                      </span>
                    </td>
                    <td className="p-3 text-center">
                      <Badge variant="outline" className={STATUS_COLORS[order.status] || ''}>
                        {statusLabel(order.status)}
                      </Badge>
                    </td>
                    <td className="p-3 text-right">
                      <Select value={order.status} onValueChange={v => handleStatusChange(order.id, v as OrderStatusValue)}>
                        <SelectTrigger className="h-7 w-[120px] text-xs"><SelectValue /></SelectTrigger>
                        <SelectContent>
                          <SelectItem value="Pending">{statusLabel('Pending')}</SelectItem>
                          <SelectItem value="Processing">{statusLabel('Processing')}</SelectItem>
                          <SelectItem value="Shipped">{statusLabel('Shipped')}</SelectItem>
                          <SelectItem value="Delivered">{statusLabel('Delivered')}</SelectItem>
                          <SelectItem value="Cancelled">{statusLabel('Cancelled')}</SelectItem>
                        </SelectContent>
                      </Select>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
          <div className="flex items-center justify-between p-3 border-t">
            <span className="text-xs text-muted-foreground">{filtered.length} {locale === 'fr' ? 'commandes' : 'orders'}</span>
            <div className="flex gap-1">
              {Array.from({ length: totalPages }, (_, i) => (
                <Button key={i} size="sm" variant={page === i + 1 ? 'default' : 'outline'} className={page === i + 1 ? 'bg-brand-primary text-white' : ''} onClick={() => setPage(i + 1)}>
                  {i + 1}
                </Button>
              ))}
            </div>
          </div>
        </CardContent>
      </Card>

      {/* Order Detail Dialog */}
      <Dialog open={!!detailOrder} onOpenChange={() => setDetailOrder(null)}>
        <DialogContent className="max-w-lg max-h-[90vh] overflow-y-auto">
          {detailOrder && (() => {
            return (
              <>
                <DialogHeader>
                  <DialogTitle>{locale === 'fr' ? 'Commande' : 'Order'} #{detailOrder.id.split('-').pop()}</DialogTitle>
                </DialogHeader>
                <div className="space-y-4 text-sm">
                  <div className="grid grid-cols-2 gap-4">
                    <div>
                      <p className="text-muted-foreground mb-1">Client</p>
                      <p className="font-medium">{detailOrder.userName}</p>
                    </div>
                    <div>
                      <p className="text-muted-foreground mb-1">Date</p>
                      <p>{new Date(detailOrder.date).toLocaleDateString(locale === 'fr' ? 'fr-FR' : 'en-US')}</p>
                    </div>
                  </div>

                  <Separator />

                  <div>
                    <p className="font-medium mb-2">{locale === 'fr' ? 'Articles' : 'Items'}</p>
                    {detailOrder.items.map((item, i) => {
                      const vatRate = VAT_RATE_VALUES[item.vatRate] || 0;
                      return (
                        <div key={i} className="flex justify-between py-1">
                          <span>{localized(toLocalized(item.productNameFr, item.productNameEn))} x{item.quantity}</span>
                          <span className="font-medium">{fmt(item.priceHT * item.quantity * (1 + vatRate))}</span>
                        </div>
                      );
                    })}
                    <div className="flex justify-between py-1 text-muted-foreground">
                      <span>{locale === 'fr' ? 'Livraison' : 'Shipping'}</span>
                      <span>{fmt(detailOrder.shippingCost)}</span>
                    </div>
                    <Separator className="my-2" />
                    <div className="flex justify-between font-bold">
                      <span>Total TTC</span>
                      <span>{fmt(detailOrder.totalTTC)}</span>
                    </div>
                  </div>

                  <Separator />

                  <div>
                    <p className="font-medium mb-2 flex items-center gap-1">
                      <Clock className="w-4 h-4" /> {locale === 'fr' ? 'Historique des statuts' : 'Status History'}
                    </p>
                    <div className="space-y-2">
                      {detailOrder.statusHistory.map((sh, i) => (
                        <div key={i} className="flex items-center gap-3 text-xs">
                          <span className="text-muted-foreground w-24">{new Date(sh.date).toLocaleDateString(locale === 'fr' ? 'fr-FR' : 'en-US')}</span>
                          <Badge variant="outline" className={STATUS_COLORS[sh.from] || ''}>{statusLabel(sh.from)}</Badge>
                          <ChevronDown className="w-3 h-3 -rotate-90" />
                          <Badge variant="outline" className={STATUS_COLORS[sh.to] || ''}>{statusLabel(sh.to)}</Badge>
                        </div>
                      ))}
                    </div>
                  </div>

                  <Separator />

                  <div className="grid grid-cols-2 gap-4 text-xs">
                    <div>
                      <p className="text-muted-foreground mb-1">{locale === 'fr' ? 'Adresse de facturation' : 'Billing Address'}</p>
                      <p>{detailOrder.billingAddress.firstName} {detailOrder.billingAddress.lastName}</p>
                      <p>{detailOrder.billingAddress.street}</p>
                      <p>{detailOrder.billingAddress.postalCode} {detailOrder.billingAddress.city}</p>
                    </div>
                    <div>
                      <p className="text-muted-foreground mb-1">{locale === 'fr' ? 'Adresse de livraison' : 'Shipping Address'}</p>
                      <p>{detailOrder.shippingAddress.firstName} {detailOrder.shippingAddress.lastName}</p>
                      <p>{detailOrder.shippingAddress.street}</p>
                      <p>{detailOrder.shippingAddress.postalCode} {detailOrder.shippingAddress.city}</p>
                    </div>
                  </div>
                </div>
              </>
            );
          })()}
        </DialogContent>
      </Dialog>
    </div>
  );
}
