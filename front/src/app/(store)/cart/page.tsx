'use client';

import Link from 'next/link';
import Image from 'next/image';
import { Trash2, Minus, Plus, ShoppingBag, AlertTriangle, Loader2 } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { Card, CardContent } from '@/components/ui/card';
import { Separator } from '@/components/ui/separator';
import { useI18n } from '@/context/i18n-context';
import { useCart } from '@/context/cart-context';
import { toLocalized, getProductImageUrl } from '@/lib/api-types';
import { formatPrice, calculateTTC } from '@/lib/money';

const VAT_RATE_VALUES: Record<string, number> = {
  Standard: 0.20, Intermediate: 0.10, Reduced: 0.055, Zero: 0,
};

export default function CartPage() {
  const { t, localized, locale } = useI18n();
  const { items, updateQuantity, removeItem, subtotalHT, totalVAT, totalTTC, hasUnavailableItems, loading, productCache } = useCart();
  const fmt = (n: number) => formatPrice(n, locale === 'fr' ? 'fr-FR' : 'en-US');

  if (loading) {
    return <div className="flex items-center justify-center py-32"><Loader2 className="w-8 h-8 animate-spin text-brand-primary" /></div>;
  }

  if (items.length === 0) {
    return (
      <div className="container mx-auto px-4 py-16 text-center">
        <ShoppingBag className="w-16 h-16 mx-auto text-muted-foreground mb-4" />
        <h1 className="text-2xl text-brand-dark mb-4">{t('cart.empty')}</h1>
        <Link href="/"><Button className="bg-brand-primary hover:bg-brand-hover text-white">{t('cart.continue_shopping')}</Button></Link>
      </div>
    );
  }

  return (
    <div className="container mx-auto px-4 py-8">
      <h1 className="text-3xl text-brand-dark mb-8">{t('cart.title')}</h1>
      <div className="grid grid-cols-1 lg:grid-cols-3 gap-8">
        <div className="lg:col-span-2 space-y-4">
          {items.map(item => {
            const product = productCache.get(item.productId);
            if (!product) return null;
            const vatRate = VAT_RATE_VALUES[product.vatRate] ?? 0.20;
            const priceTTC = calculateTTC(product.priceHT, vatRate);
            const lineTTC = priceTTC * item.quantity;
            const isOOS = product.stockStatus === 'OutOfStock';
            const name = localized(toLocalized(product.nameFr, product.nameEn));
            return (
              <Card key={item.productId} className={isOOS ? 'border-error/50 bg-error/5' : ''}>
                <CardContent className="p-4">
                  <div className="flex gap-4">
                    <Link href={`/product/${product.slug}`} className="shrink-0">
                      <div className="relative w-20 h-20 rounded overflow-hidden">
                        <Image src={getProductImageUrl(product)} alt={name} fill className="object-cover" />
                      </div>
                    </Link>
                    <div className="flex-1 min-w-0">
                      <Link href={`/product/${product.slug}`} className="font-medium text-brand-dark hover:text-brand-primary line-clamp-1">{name}</Link>
                      <p className="text-sm text-muted-foreground">{fmt(priceTTC)} / {t('common.quantity').toLowerCase()}</p>
                      {isOOS && (
                        <div className="flex items-center gap-1 text-error text-sm mt-1">
                          <AlertTriangle className="w-4 h-4" />
                          {t('cart.unavailable_warning')}
                        </div>
                      )}
                      <div className="flex items-center justify-between mt-2">
                        <div className="flex items-center border rounded">
                          <Button variant="ghost" size="icon" className="h-8 w-8" onClick={() => updateQuantity(product.id, item.quantity - 1)}>
                            <Minus className="w-3 h-3" />
                          </Button>
                          <span className="w-10 text-center text-sm">{item.quantity}</span>
                          <Button variant="ghost" size="icon" className="h-8 w-8" onClick={() => updateQuantity(product.id, item.quantity + 1)}>
                            <Plus className="w-3 h-3" />
                          </Button>
                        </div>
                        <div className="flex items-center gap-4">
                          <span className="font-bold text-brand-dark">{fmt(lineTTC)}</span>
                          <Button variant="ghost" size="icon" className="text-error hover:bg-error/10" onClick={() => removeItem(product.id)} aria-label={t('cart.remove')}>
                            <Trash2 className="w-4 h-4" />
                          </Button>
                        </div>
                      </div>
                    </div>
                  </div>
                </CardContent>
              </Card>
            );
          })}
        </div>

        <div>
          <Card>
            <CardContent className="p-6">
              <h2 className="text-lg font-semibold text-brand-dark mb-4">{t('common.total')}</h2>
              <div className="space-y-2 text-sm">
                <div className="flex justify-between"><span>{t('cart.subtotal')}</span><span>{fmt(subtotalHT)}</span></div>
                <div className="flex justify-between"><span>{t('cart.vat')}</span><span>{fmt(totalVAT)}</span></div>
                <Separator className="my-3" />
                <div className="flex justify-between text-lg font-bold"><span>{t('cart.total')}</span><span className="text-brand-dark">{fmt(totalTTC)}</span></div>
              </div>
              <Link href="/checkout" className="block mt-6">
                <Button className="w-full bg-brand-primary hover:bg-brand-hover text-white" size="lg" disabled={hasUnavailableItems}>
                  {t('cart.checkout')}
                </Button>
              </Link>
              {hasUnavailableItems && <p className="text-sm text-error mt-2 text-center">{t('cart.unavailable_warning')}</p>}
              <Link href="/" className="block mt-3">
                <Button variant="outline" className="w-full">{t('cart.continue_shopping')}</Button>
              </Link>
            </CardContent>
          </Card>
        </div>
      </div>
    </div>
  );
}
