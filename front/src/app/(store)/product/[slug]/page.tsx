'use client';

import { use, useState, useMemo } from 'react';
import Link from 'next/link';
import Image from 'next/image';
import { ChevronLeft, ChevronRight, ShoppingCart, Minus, Plus } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { Badge } from '@/components/ui/badge';
import { Card, CardContent } from '@/components/ui/card';
import { Separator } from '@/components/ui/separator';
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/components/ui/tabs';
import { useI18n } from '@/context/i18n-context';
import { useCart } from '@/context/cart-context';
import { products, categories, getProductImage } from '@/mock';
import { formatPrice, calculateTTC, calculateVAT, toIntlLocale } from '@/lib/money';
import { VAT_RATES } from '@/lib/constants';
import { toast } from 'sonner';

export default function ProductPage({ params }: { params: Promise<{ slug: string }> }) {
  const { slug } = use(params);
  const { t, localized, locale } = useI18n();
  const { addItem } = useCart();
  const [imageIndex, setImageIndex] = useState(0);
  const [qty, setQty] = useState(1);

  const product = products.find(p => p.slug === slug);
  const fmt = (n: number) => formatPrice(n, toIntlLocale(locale));

  const similar = useMemo(() => {
    if (!product) return [];
    const sameCat = products.filter(p =>
      p.id !== product.id &&
      p.status === 'published' &&
      p.categories.some(c => product.categories.includes(c))
    );
    // Prefer in-stock, then use a stable pseudo-random based on id
    sameCat.sort((a, b) => {
      if (a.stockStatus === 'out_of_stock' && b.stockStatus !== 'out_of_stock') return 1;
      if (b.stockStatus === 'out_of_stock' && a.stockStatus !== 'out_of_stock') return -1;
      return a.id.localeCompare(b.id);
    });
    return sameCat.slice(0, 6);
  }, [product]);

  if (!product) {
    return <div className="container mx-auto px-4 py-16 text-center"><h1 className="text-2xl">{t('common.error')}</h1></div>;
  }

  const vatRate = VAT_RATES[product.vatRate];
  const priceTTC = calculateTTC(product.priceHT, vatRate);
  const vatAmount = calculateVAT(product.priceHT, vatRate);
  const isOOS = product.stockStatus === 'out_of_stock';
  const productCats = categories.filter(c => product.categories.includes(c.id));

  const handleAddToCart = () => {
    addItem(product.id, qty);
    const suffix: Record<string, string> = { fr: 'ajouté au panier', en: 'added to cart', ms: 'ditambah ke troli', ar: 'أُضيف إلى عربة التسوق' };
    toast.success(`${localized(product.name)} ${suffix[locale] ?? 'added to cart'} (x${qty})`);
  };

  return (
    <div className="container mx-auto px-4 py-8">
      {/* Breadcrumb */}
      <nav className="text-sm text-muted-foreground mb-6 flex flex-wrap gap-1" aria-label="Breadcrumb">
        <Link href="/" className="hover:text-brand-primary">{t('nav.home')}</Link>
        <span>/</span>
        {productCats[0] && <>
          <Link href={`/category/${productCats[0].slug}`} className="hover:text-brand-primary">{localized(productCats[0].name)}</Link>
          <span>/</span>
        </>}
        <span className="text-foreground">{localized(product.name)}</span>
      </nav>

      <div className="grid grid-cols-1 lg:grid-cols-2 gap-8">
        {/* Image Carousel */}
        <div className="relative">
          <div className="relative aspect-square rounded-lg overflow-hidden bg-gray-100">
            <Image src={getProductImage(product.images[imageIndex] || product.images[0])} alt={localized(product.name)} fill className="object-cover" priority />
          </div>
          {product.images.length > 1 && (
            <div className="flex gap-2 mt-4 justify-center">
              {product.images.map((img, i) => (
                <button key={i} onClick={() => setImageIndex(i)} className={`w-16 h-16 rounded border-2 overflow-hidden ${i === imageIndex ? 'border-brand-primary' : 'border-transparent'}`}>
                  <Image src={getProductImage(img)} alt="" width={64} height={64} className="object-cover w-full h-full" />
                </button>
              ))}
            </div>
          )}
        </div>

        {/* Product Info */}
        <div>
          <div className="flex flex-wrap gap-2 mb-3">
            {product.isNew && <Badge className="bg-brand-primary text-white">{t('product.new')}</Badge>}
            {product.stockStatus === 'in_stock' && <Badge className="bg-success text-white">{t('product.in_stock')}</Badge>}
            {product.stockStatus === 'low_stock' && <Badge className="bg-warning text-white">{t('product.low_stock')}</Badge>}
            {isOOS && <Badge className="bg-error text-white">{t('product.out_of_stock')}</Badge>}
          </div>

          <h1 className="text-2xl md:text-3xl text-brand-dark mb-2">{localized(product.name)}</h1>
          <p className="text-muted-foreground mb-4">{localized(product.description)}</p>

          <div className="bg-gray-50 rounded-lg p-4 mb-6">
            <div className="text-3xl font-bold text-brand-dark mb-1">{fmt(priceTTC)}</div>
            <div className="text-sm text-muted-foreground">
              {t('product.price_ht')}: {fmt(product.priceHT)} | {t('product.vat')} ({(vatRate * 100).toFixed(1)}%): {fmt(vatAmount)}
            </div>
          </div>

          {/* Quantity + Add to Cart */}
          <div className="flex items-center gap-4 mb-6">
            <div className="flex items-center border rounded-lg">
              <Button variant="ghost" size="icon" onClick={() => setQty(q => Math.max(1, q - 1))} disabled={isOOS} aria-label="Decrease quantity">
                <Minus className="w-4 h-4" />
              </Button>
              <span className="w-12 text-center font-medium">{qty}</span>
              <Button variant="ghost" size="icon" onClick={() => setQty(q => q + 1)} disabled={isOOS} aria-label="Increase quantity">
                <Plus className="w-4 h-4" />
              </Button>
            </div>
            <Button size="lg" className="flex-1 bg-brand-primary hover:bg-brand-hover text-white" onClick={handleAddToCart} disabled={isOOS}>
              <ShoppingCart className="w-5 h-5 mr-2" />
              {isOOS ? t('product.out_of_stock') : t('product.add_to_cart')}
            </Button>
          </div>

          <Separator className="my-6" />

          {/* Tabs: Description + Specs */}
          <Tabs defaultValue="description">
            <TabsList>
              <TabsTrigger value="description">{t('product.description')}</TabsTrigger>
              <TabsTrigger value="specs">{t('product.specs')}</TabsTrigger>
            </TabsList>
            <TabsContent value="description" className="mt-4">
              <p className="text-foreground leading-relaxed">{localized(product.longDescription)}</p>
            </TabsContent>
            <TabsContent value="specs" className="mt-4">
              <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
                {product.specs.map((spec, i) => (
                  <div key={i} className="flex justify-between p-3 bg-gray-50 rounded">
                    <span className="text-sm font-medium text-muted-foreground">{spec.label}</span>
                    <span className="text-sm font-semibold">{spec.value}</span>
                  </div>
                ))}
              </div>
            </TabsContent>
          </Tabs>
        </div>
      </div>

      {/* Similar Products */}
      {similar.length > 0 && (
        <section className="mt-16">
          <h2 className="text-2xl text-brand-dark mb-6">{t('product.similar')}</h2>
          <div className="grid grid-cols-2 md:grid-cols-3 lg:grid-cols-6 gap-4">
            {similar.map((p) => {
              const price = calculateTTC(p.priceHT, VAT_RATES[p.vatRate]);
              return (
                <Link key={p.id} href={`/product/${p.slug}`} className="group">
                  <Card className="overflow-hidden hover:shadow-md transition-shadow">
                    <div className="relative h-32">
                      <Image src={getProductImage(p.images[0])} alt={localized(p.name)} fill className="object-cover group-hover:scale-105 transition-transform" />
                    </div>
                    <CardContent className="p-3">
                      <h3 className="text-xs font-medium line-clamp-2 mb-1">{localized(p.name)}</h3>
                      <span className="text-sm font-bold text-brand-dark">{fmt(price)}</span>
                    </CardContent>
                  </Card>
                </Link>
              );
            })}
          </div>
        </section>
      )}
    </div>
  );
}
