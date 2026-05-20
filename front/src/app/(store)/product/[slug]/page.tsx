'use client';

import { use, useState, useEffect } from 'react';
import Link from 'next/link';
import Image from 'next/image';
import { ShoppingCart, Minus, Plus, Loader2 } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { Badge } from '@/components/ui/badge';
import { Card, CardContent } from '@/components/ui/card';
import { Separator } from '@/components/ui/separator';
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/components/ui/tabs';
import { useI18n } from '@/context/i18n-context';
import { useCart } from '@/context/cart-context';
import { productsService } from '@/lib/api-services';
import type { ProductDto } from '@/lib/api-types';
import { toLocalized, getProductImageUrl, getImageUrl } from '@/lib/api-types';
import { formatPrice, calculateTTC, calculateVAT } from '@/lib/money';
import { ProductStatus, StockStatus, VatRate } from '@/lib/enums';
import { toast } from 'sonner';

const VAT_RATE_VALUES: Record<number, number> = {
  [VatRate.Standard]: 0.20, [VatRate.Intermediate]: 0.10, [VatRate.Reduced]: 0.055, [VatRate.Zero]: 0,
};

export default function ProductPage({ params }: { params: Promise<{ slug: string }> }) {
  const { slug } = use(params);
  const { t, localized, locale } = useI18n();
  const { addItem } = useCart();
  const [imageIndex, setImageIndex] = useState(0);
  const [qty, setQty] = useState(1);
  const [product, setProduct] = useState<ProductDto | null>(null);
  const [similar, setSimilar] = useState<ProductDto[]>([]);
  const [loading, setLoading] = useState(true);

  const fmt = (n: number) => formatPrice(n, locale === 'fr' ? 'fr-FR' : 'en-US');

  useEffect(() => {
    setLoading(true);
    setImageIndex(0);
    setQty(1);
    productsService.getBySlug(slug)
      .then(async (prod) => {
        setProduct(prod);
        // Fetch similar products from same category
        if (prod.categories.length > 0) {
          try {
            const catProds = await productsService.getAll(1, 7, prod.categories[0].id);
            setSimilar(catProds.data.filter(p => p.id !== prod.id && p.status === ProductStatus.Active).slice(0, 6));
          } catch { setSimilar([]); }
        }
      })
      .catch(() => setProduct(null))
      .finally(() => setLoading(false));
  }, [slug]);

  if (loading) return <div className="flex items-center justify-center py-32"><Loader2 className="w-8 h-8 animate-spin text-brand-primary" /></div>;
  if (!product) return <div className="container mx-auto px-4 py-16 text-center"><h1 className="text-2xl">{t('common.error')}</h1></div>;

  const vatRate = VAT_RATE_VALUES[product.vatRate] ?? 0.20;
  const priceTTC = calculateTTC(product.priceHT, vatRate);
  const vatAmount = calculateVAT(product.priceHT, vatRate);
  const isOOS = product.stockStatus === StockStatus.OutOfStock;
  const name = localized(toLocalized(product.nameFr, product.nameEn, product.nameMs, product.nameAr));

  const handleAddToCart = () => {
    addItem(product.id, qty);
    toast.success(`${name} ${locale === 'fr' ? 'ajouté au panier' : 'added to cart'} (x${qty})`);
  };

  return (
    <div className="container mx-auto px-4 py-8">
      {/* Breadcrumb */}
      <nav className="text-sm text-muted-foreground mb-6 flex flex-wrap gap-1" aria-label="Breadcrumb">
        <Link href="/" className="hover:text-brand-primary">{t('nav.home')}</Link>
        <span>/</span>
        {product.categories[0] && <>
          <Link href={`/category/${product.categories[0].slug}`} className="hover:text-brand-primary">
            {localized(toLocalized(product.categories[0].nameFr, product.categories[0].nameEn))}
          </Link>
          <span>/</span>
        </>}
        <span className="text-foreground">{name}</span>
      </nav>

      <div className="grid grid-cols-1 lg:grid-cols-2 gap-8">
        {/* Image Carousel */}
        <div className="relative">
          <div className="relative aspect-square rounded-lg overflow-hidden bg-gray-100">
            <Image src={getImageUrl(product.images[imageIndex] || product.images[0])} alt={name} fill className="object-cover" priority />
          </div>
          {product.images.length > 1 && (
            <div className="flex gap-2 mt-4 justify-center">
              {product.images.map((img, i) => (
                <button key={i} onClick={() => setImageIndex(i)} className={`w-16 h-16 rounded border-2 overflow-hidden ${i === imageIndex ? 'border-brand-primary' : 'border-transparent'}`}>
                  <Image src={getImageUrl(img)} alt="" width={64} height={64} className="object-cover w-full h-full" />
                </button>
              ))}
            </div>
          )}
        </div>

        {/* Product Info */}
        <div>
          <div className="flex flex-wrap gap-2 mb-3">
            {product.isNew && <Badge className="bg-brand-primary text-white">{t('product.new')}</Badge>}
            {product.stockStatus === StockStatus.InStock && <Badge className="bg-success text-white">{t('product.in_stock')}</Badge>}
            {product.stockStatus === StockStatus.LowStock && <Badge className="bg-warning text-white">{t('product.low_stock')}</Badge>}
            {isOOS && <Badge className="bg-error text-white">{t('product.out_of_stock')}</Badge>}
          </div>

          <h1 className="text-2xl md:text-3xl text-brand-dark mb-2">{name}</h1>
          <p className="text-muted-foreground mb-4">{localized(toLocalized(product.descriptionFr, product.descriptionEn, product.descriptionMs, product.descriptionAr))}</p>

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
              <ShoppingCart className="w-5 h-5 me-2" />
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
              <p className="text-foreground leading-relaxed">{localized(toLocalized(product.longDescriptionFr, product.longDescriptionEn, product.longDescriptionMs, product.longDescriptionAr))}</p>
            </TabsContent>
            <TabsContent value="specs" className="mt-4">
              <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
                {product.specs.map((spec, i) => (
                  <div key={i} className="flex justify-between p-3 bg-gray-50 rounded">
                    <span className="text-sm font-medium text-muted-foreground">
                      {localized(toLocalized(spec.label, spec.labelEn ?? spec.label, spec.labelMs, spec.labelAr))}
                    </span>
                    <span className="text-sm font-semibold">
                      {localized(toLocalized(spec.value, spec.valueEn ?? spec.value, spec.valueMs, spec.valueAr))}
                    </span>
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
              const vr = VAT_RATE_VALUES[p.vatRate] ?? 0.20;
              const price = calculateTTC(p.priceHT, vr);
              return (
                <Link key={p.id} href={`/product/${p.slug}`} className="group">
                  <Card className="overflow-hidden hover:shadow-md transition-shadow">
                    <div className="relative h-32">
                      <Image src={getProductImageUrl(p)} alt={localized(toLocalized(p.nameFr, p.nameEn, p.nameMs, p.nameAr))} fill className="object-cover group-hover:scale-105 transition-transform" />
                    </div>
                    <CardContent className="p-3">
                      <h3 className="text-xs font-medium line-clamp-2 mb-1">{localized(toLocalized(p.nameFr, p.nameEn, p.nameMs, p.nameAr))}</h3>
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
