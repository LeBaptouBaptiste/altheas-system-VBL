'use client';

import { useState, useEffect } from 'react';
import { useRouter } from 'next/navigation';
import { Settings, Package, MapPin, CreditCard, Download, AlertTriangle, Shield, Loader2, RotateCcw, FileText } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Card, CardContent } from '@/components/ui/card';
import { Badge } from '@/components/ui/badge';
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/components/ui/tabs';
import { Separator } from '@/components/ui/separator';
import { Dialog, DialogClose, DialogContent, DialogHeader, DialogTitle, DialogTrigger } from '@/components/ui/dialog';
import { useI18n } from '@/context/i18n-context';
import { useAuth } from '@/context/auth-context';
import { ordersService, usersService, authService, downloadInvoicePdf, downloadCreditNotePdf } from '@/lib/api-services';
import { TwoFactorSection } from '@/components/account/two-factor-section';
import { useStepUp } from '@/components/two-factor/step-up-provider';
import { getErrorMessage } from '@/lib/api-errors';
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
  const { user, isAuthenticated, anonymizeAccount, logout, refreshUser, loading: authLoading } = useAuth();
  const { withStepUp } = useStepUp();
  const router = useRouter();
  const fmt = (n: number) => formatPrice(n, locale === 'fr' ? 'fr-FR' : 'en-US');
  const [userOrders, setUserOrders] = useState<OrderDto[]>([]);
  const [ordersLoading, setOrdersLoading] = useState(true);
  const [editName, setEditName] = useState('');
  const [editEmail, setEditEmail] = useState('');
  const [newPassword, setNewPassword] = useState('');
  const [saving, setSaving] = useState(false);
  // Per-invoice download state. Keyed by the invoice/credit-note id (not the
  // order id) so a customer with both a facture and an avoir on the same
  // order can download them independently — only the clicked row spins.
  const [downloadingInvoiceId, setDownloadingInvoiceId] = useState<string | null>(null);

  const handleDownloadInvoice = async (invoiceId: string, type: number) => {
    if (downloadingInvoiceId) return;
    setDownloadingInvoiceId(invoiceId);
    try {
      // 1 = CreditNote, 0 = Invoice (see lib/enums.ts InvoiceType).
      const isCreditNote = type === 1;
      if (isCreditNote) {
        await downloadCreditNotePdf(invoiceId);
        toast.success(locale === 'fr' ? 'Avoir téléchargé' : 'Credit note downloaded');
      } else {
        await downloadInvoicePdf(invoiceId);
        toast.success(locale === 'fr' ? 'Facture téléchargée' : 'Invoice downloaded');
      }
    } catch (err) {
      console.error('Invoice download failed', err);
      toast.error(locale === 'fr' ? 'Échec du téléchargement' : 'Download failed');
    } finally {
      setDownloadingInvoiceId(null);
    }
  };

  useEffect(() => {
    if (!authLoading && !isAuthenticated) {
      router.push('/login');
    }
  }, [authLoading, isAuthenticated, router]);

  useEffect(() => {
    if (user) {
      setEditName(user.name);
      setEditEmail(user.email);
    }
  }, [user]);

  useEffect(() => {
    if (!user) return;
    ordersService.getAll(1, 50)
      .then(res => setUserOrders(res.data.filter(o => o.userId === user.id).sort((a, b) => new Date(b.date).getTime() - new Date(a.date).getTime())))
      .catch((err: unknown) => {
        console.error('Failed to load orders', err);
        toast.error(getErrorMessage(err, t));
      })
      .finally(() => setOrdersLoading(false));
  }, [user, t]);

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
          <TabsTrigger value="settings"><Settings className="w-4 h-4 me-1" />{t('account.settings')}</TabsTrigger>
          <TabsTrigger value="security"><Shield className="w-4 h-4 me-1" />{t('account.security')}</TabsTrigger>
          <TabsTrigger value="orders"><Package className="w-4 h-4 me-1" />{t('account.orders')}</TabsTrigger>
          <TabsTrigger value="addresses"><MapPin className="w-4 h-4 me-1" />{t('account.addresses')}</TabsTrigger>
          <TabsTrigger value="payments"><CreditCard className="w-4 h-4 me-1" />{t('account.payment_methods')}</TabsTrigger>
        </TabsList>

        {/* Settings */}
        <TabsContent value="settings">
          <Card><CardContent className="p-6 space-y-4">
            <div><Label>{t('auth.full_name')}</Label><Input value={editName} onChange={e => setEditName(e.target.value)} /></div>
            <div><Label>{t('auth.email')}</Label><Input value={editEmail} onChange={e => setEditEmail(e.target.value)} /><p className="text-xs text-muted-foreground mt-1">{locale === 'fr' ? 'Modifier l\'email nécessite une confirmation' : 'Changing email requires confirmation'}</p></div>
            <Separator />
            <div><Label>{t('auth.password')}</Label><Input type="password" placeholder="••••••••" value={newPassword} onChange={e => setNewPassword(e.target.value)} /><p className="text-xs text-muted-foreground mt-1">{t('auth.password_rules')}</p></div>
            <Button className="bg-brand-primary hover:bg-brand-hover text-white" disabled={saving} onClick={async () => {
              setSaving(true);
              try {
                // Update name/email if changed
                if (editName !== user.name || editEmail !== user.email) {
                  await usersService.update(user.id, { name: editName, email: editEmail });
                }
                // Update password if provided
                if (newPassword) {
                  await authService.resetPassword(user.email, newPassword, newPassword);
                  setNewPassword('');
                }
                await refreshUser();
                toast.success(t('account.save') + ' ✓');
              } catch {
                toast.error(t('common.error'));
              } finally {
                setSaving(false);
              }
            }}>{saving ? '...' : t('account.save')}</Button>
            <Separator />
            <Dialog>
              <DialogTrigger asChild>
                <Button variant="destructive" className="mt-4"><AlertTriangle className="w-4 h-4 me-2" />{t('account.anonymize')}</Button>
              </DialogTrigger>
              <DialogContent>
                <DialogHeader><DialogTitle>{t('account.anonymize')}</DialogTitle></DialogHeader>
                <p className="text-sm text-muted-foreground">{t('account.anonymize_warning')}</p>
                <div className="flex gap-4 mt-4">
                  <DialogClose asChild>
                    <Button variant="outline" className="flex-1">{t('common.cancel')}</Button>
                  </DialogClose>
                  <Button
                    variant="destructive"
                    className="flex-1"
                    onClick={async () => {
                      try {
                        // Sensitive op: gated by [RequireStepUp(Action)] server-side.
                        // withStepUp handles the 1st-call → 403 → modal → retry pattern.
                        // anonymizeAccount returns Promise<void>, so withStepUp
                        // resolves with `undefined` on success and `null` on user cancel.
                        const out = await withStepUp((token) => anonymizeAccount(token));
                        if (out !== null) {
                          toast.success(t('account.anonymize_success'));
                          router.push('/');
                        }
                      } catch (err) {
                        toast.error(getErrorMessage(err, t));
                      }
                    }}
                  >
                    {t('common.confirm')}
                  </Button>
                </div>
              </DialogContent>
            </Dialog>
          </CardContent></Card>
        </TabsContent>

        {/* Security (2FA) */}
        <TabsContent value="security">
          <TwoFactorSection />
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
                            <span className="text-sm text-muted-foreground ms-3">{order.date.slice(0, 10)}</span>
                          </div>
                          <div className="flex items-center gap-3">
                            <Badge className={statusColors[order.status] || 'bg-gray-200'}>{enumLabel('OrderStatus', order.status, locale)}</Badge>
                            <span className="font-bold">{fmt(order.totalTTC)}</span>
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

                        {/* Phase 6: list ALL invoices + credit notes attached
                            to this order. Each row gets its own download
                            button so the customer can grab the facture AND
                            any avoirs émis. */}
                        {order.invoices && order.invoices.length > 0 && (
                          <div className="mt-3 pt-3 border-t space-y-1.5">
                            {order.invoices.map((inv) => {
                              const isCreditNote = inv.type === 1; // InvoiceType.CreditNote
                              return (
                                <div key={inv.id} className="flex items-center justify-between gap-2 text-sm">
                                  <div className="flex items-center gap-2 min-w-0">
                                    {isCreditNote
                                      ? <RotateCcw className="w-3.5 h-3.5 text-error shrink-0" />
                                      : <FileText className="w-3.5 h-3.5 text-brand-primary shrink-0" />}
                                    <span className={isCreditNote ? 'text-error' : 'text-brand-dark'}>
                                      {isCreditNote
                                        ? (locale === 'fr' ? 'Avoir' : 'Credit note')
                                        : (locale === 'fr' ? 'Facture' : 'Invoice')}
                                      {' '}
                                      <span className="font-mono text-xs text-muted-foreground">
                                        #{inv.id.slice(0, 8).toUpperCase()}
                                      </span>
                                    </span>
                                    <span className={`text-xs ${isCreditNote ? 'text-error' : 'text-muted-foreground'}`}>
                                      {isCreditNote ? '−' : ''}{fmt(inv.amountTTC)}
                                    </span>
                                  </div>
                                  <Button
                                    variant="ghost"
                                    size="sm"
                                    className="h-7"
                                    disabled={downloadingInvoiceId === inv.id}
                                    onClick={() => handleDownloadInvoice(inv.id, inv.type)}
                                    title={isCreditNote
                                      ? (locale === 'fr' ? "Télécharger l'avoir (PDF)" : 'Download credit note (PDF)')
                                      : (locale === 'fr' ? 'Télécharger la facture (PDF)' : 'Download invoice (PDF)')}
                                  >
                                    {downloadingInvoiceId === inv.id
                                      ? <Loader2 className="w-3 h-3 animate-spin" />
                                      : <Download className="w-3 h-3" />}
                                  </Button>
                                </div>
                              );
                            })}
                          </div>
                        )}
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
                  <Button variant="outline" size="sm" className="text-error" onClick={async () => {
                    try {
                      await usersService.deleteAddress(user.id, addr.id);
                      await refreshUser();
                      toast.success(locale === 'fr' ? 'Adresse supprimée' : 'Address deleted');
                    } catch { toast.error(t('common.error')); }
                  }}>{t('common.delete')}</Button>
                </div>
              </CardContent></Card>
            ))}
          </div>
          {user.addresses.length === 0 && <p className="text-muted-foreground text-sm py-4">{t('common.no_data')}</p>}
        </TabsContent>

        {/* Payment Methods */}
        <TabsContent value="payments">
          <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
            {user.paymentMethods.map(pm => {
              // Detect expired cards (the user can still see them but the
              // chip is colored differently and a tooltip explains).
              const now = new Date();
              const isExpired = pm.expYear !== null && pm.expMonth !== null && (
                pm.expYear < now.getFullYear() ||
                (pm.expYear === now.getFullYear() && pm.expMonth < now.getMonth() + 1)
              );
              return (
                <Card key={pm.id} className={isExpired ? 'border-warning/50 bg-warning/5' : ''}>
                  <CardContent className="p-4 flex items-center justify-between">
                    <div className="flex items-center gap-3 flex-1 min-w-0">
                      <CreditCard className={`w-5 h-5 shrink-0 ${isExpired ? 'text-warning' : 'text-brand-primary'}`} />
                      <div className="min-w-0">
                        <p className="font-medium truncate">{pm.label}</p>
                        {pm.expMonth !== null && pm.expYear !== null && (
                          <p className={`text-xs ${isExpired ? 'text-warning font-medium' : 'text-muted-foreground'}`}>
                            {isExpired
                              ? (locale === 'fr' ? 'Expirée — ' : 'Expired — ')
                              : ''}
                            {String(pm.expMonth).padStart(2, '0')}/{String(pm.expYear).slice(-2)}
                          </p>
                        )}
                      </div>
                    </div>
                    <Button
                      variant="outline"
                      size="sm"
                      className="text-error border-error/30 hover:bg-error/10"
                      onClick={async () => {
                        // Sensitive op — gated by [RequireStepUp(Action)] server-side.
                        // withStepUp handles the 1st-call → 403 → modal → retry pattern.
                        const out = await withStepUp((token) =>
                          usersService.deletePaymentMethod(user.id, pm.id, token));
                        if (out === null) return; // user cancelled the step-up modal
                        try {
                          await refreshUser();
                          toast.success(locale === 'fr' ? 'Moyen de paiement supprimé' : 'Payment method deleted');
                        } catch {
                          toast.error(t('common.error'));
                        }
                      }}
                    >
                      {t('common.delete')}
                    </Button>
                  </CardContent>
                </Card>
              );
            })}
          </div>
          {user.paymentMethods.length === 0 && (
            <p className="text-muted-foreground text-sm py-4">
              {locale === 'fr'
                ? 'Aucune carte enregistrée. Cochez "Sauvegarder ma carte" lors d\'un prochain paiement.'
                : 'No saved cards. Tick "Save my card" on your next payment to add one.'}
            </p>
          )}
          <p className="text-xs text-muted-foreground mt-2">
            {locale === 'fr'
              ? 'Aucune donnée sensible n\'est stockée (PAN, CVC). Seuls la marque, les 4 derniers chiffres et la date d\'expiration.'
              : 'No sensitive data is stored (PAN, CVC). Only the brand, last 4 digits, and expiry.'}
          </p>
        </TabsContent>
      </Tabs>
    </div>
  );
}
