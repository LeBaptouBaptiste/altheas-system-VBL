'use client';

import { use, useState, useEffect, useMemo } from 'react';
import Link from 'next/link';
import Image from 'next/image';
import { ShoppingCart, Loader2 } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { Card, CardContent } from '@/components/ui/card';
import { Badge } from '@/components/ui/badge';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';
import { useI18n } from '@/context/i18n-context';
import { useCart } from '@/context/cart-context';
import { categoriesService, productsService } from '@/lib/api-services';
import type { CategoryDto, ProductDto } from '@/lib/api-types';
import { toLocalized, getProductImageUrl, getCategoryImageUrl } from '@/lib/api-types';
import { formatPrice } from '@/lib/money';
import { ProductStatus, StockStatus, VatRate } from '@/lib/enums';
import { toast } from 'sonner';

const VAT_RATE_VALUES: Record<number, number> = {
  [VatRate.Standard]: 0.20, [VatRate.Intermediate]: 0.10, [VatRate.Reduced]: 0.055, [VatRate.Zero]: 0,
};

export default function CategoryPage({ params }: { params: Promise<{ slug: string }> }) {
  const { slug } = use(params);
  const { t, localized, locale } = useI18n();
  const { addItem } = useCart();
  const [sort, setSort] = useState('priority');
  const [page, setPage] = useState(1);
  const [category, setCategory] = useState<CategoryDto | null>(null);
  const [allProducts, setAllProducts] = useState<ProductDto[]>([]);
  const [loading, setLoading] = useState(true);
  const pageSize = 12;

  useEffect(() => {
    setLoading(true);
    categoriesService.getBySlug(slug)
      .then(async (cat) => {
        setCategory(cat);
        const prods = await productsService.getAll(1, 100, cat.id);
        setAllProducts(prods.data);
      })
      .catch(() => setCategory(null))
      .finally(() => setLoading(false));
  }, [slug]);

  const sortedProducts = useMemo(() => {
    const filtered = allProducts.filter(p => p.status === ProductStatus.Active);
    filtered.sort((a, b) => {
      if (a.stockStatus === StockStatus.OutOfStock && b.stockStatus !== StockStatus.OutOfStock) return 1;
      if (b.stockStatus === StockStatus.OutOfStock && a.stockStatus !== StockStatus.OutOfStock) return -1;
      if (sort === 'priority') {
        if (a.priorityRank > 0 && b.priorityRank === 0) return -1;
        if (b.priorityRank > 0 && a.priorityRank === 0) return 1;
        if (a.priorityRank > 0 && b.priorityRank > 0) return a.priorityRank - b.priorityRank;
      }
      switch (sort) {
        case 'price_asc': return a.priceHT - b.priceHT;
        case 'price_desc': return b.priceHT - a.priceHT;
        case 'newest': return new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime();
        default: return 0;
      }
    });
    return filtered;
  }, [allProducts, sort]);

  const totalPages = Math.ceil(sortedProducts.length / pageSize);
  const paginatedProducts = sortedProducts.slice((page - 1) * pageSize, page * pageSize);

  if (loading) return <div className="flex items-center justify-center py-32"><Loader2 className="w-8 h-8 animate-spin text-brand-primary" /></div>;
  if (!category) return <div className="container mx-auto px-4 py-16 text-center"><h1 className="text-2xl">{t('common.error')}</h1></div>;

  return (
    <div>
      {/* Category Header */}
      <div className="relative h-48 md:h-64">
        <Image src={getCategoryImageUrl(category)} alt={localized(toLocalized(category.nameFr, category.nameEn, category.nameMs, category.nameAr))} fill className="object-cover" />
        <div className="absolute inset-0 bg-brand-dark/70" />
        <div className="absolute inset-0 flex items-center">
          <div className="container mx-auto px-4">
            <h1 className="text-3xl md:text-4xl text-white">{localized(toLocalized(category.nameFr, category.nameEn, category.nameMs, category.nameAr))}</h1>
            <p className="text-gray-200 mt-2 max-w-2xl">{localized(toLocalized(category.descriptionFr, category.descriptionEn, category.descriptionMs, category.descriptionAr))}</p>
            <p className="text-brand-primary mt-1 text-sm">{sortedProducts.length} {t('category.products')}</p>
          </div>
        </div>
      </div>

      {/* Controls */}
      <div className="container mx-auto px-4 py-6">
        <div className="flex items-center justify-between mb-6">
          <p className="text-sm text-muted-foreground">{sortedProducts.length} {t('category.products')}</p>
          <Select value={sort} onValueChange={(v) => { setSort(v); setPage(1); }}>
            <SelectTrigger className="w-[200px]" aria-label={t('category.sort_by')}>
              <SelectValue placeholder={t('category.sort_by')} />
            </SelectTrigger>
            <SelectContent>
              <SelectItem value="priority">{t('category.sort.priority')}</SelectItem>
              <SelectItem value="price_asc">{t('category.sort.price_asc')}</SelectItem>
              <SelectItem value="price_desc">{t('category.sort.price_desc')}</SelectItem>
              <SelectItem value="newest">{t('category.sort.newest')}</SelectItem>
            </SelectContent>
          </Select>
        </div>

        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4 gap-6">
          {paginatedProducts.map((product) => {
            const vatRate = VAT_RATE_VALUES[product.vatRate] ?? 0.20;
            const priceTTC = product.priceHT * (1 + vatRate);
            const isOOS = product.stockStatus === StockStatus.OutOfStock;
            const name = localized(toLocalized(product.nameFr, product.nameEn, product.nameMs, product.nameAr));
            return (
              <Card key={product.id} className={`overflow-hidden hover:shadow-lg transition-shadow group ${isOOS ? 'opacity-60' : ''}`}>
                <Link href={`/product/${product.slug}`}>
                  <div className="relative h-48">
                    <Image src={getProductImageUrl(product)} alt={name} fill className="object-cover group-hover:scale-105 transition-transform duration-300" />
                    <div className="absolute top-2 start-2 flex gap-1 flex-wrap">
                      {product.isNew && <Badge className="bg-brand-primary text-white">{t('product.new')}</Badge>}
                      {product.stockStatus === StockStatus.InStock && <Badge className="bg-success text-white">{t('product.in_stock')}</Badge>}
                      {product.stockStatus === StockStatus.LowStock && <Badge className="bg-warning text-white">{t('product.low_stock')}</Badge>}
                      {isOOS && <Badge className="bg-error text-white">{t('product.out_of_stock')}</Badge>}
                    </div>
                  </div>
                </Link>
                <CardContent className="p-4">
                  <Link href={`/product/${product.slug}`}>
                    <h3 className="font-semibold text-brand-dark text-sm mb-1 line-clamp-2 hover:text-brand-primary transition-colors">{name}</h3>
                  </Link>
                  <p className="text-xs text-muted-foreground mb-3 line-clamp-2">{localized(toLocalized(product.descriptionFr, product.descriptionEn, product.descriptionMs, product.descriptionAr))}</p>
                  <div className="flex items-center justify-between">
                    <span className={`text-lg font-bold ${isOOS ? 'text-muted-foreground line-through' : 'text-brand-dark'}`}>
                      {formatPrice(priceTTC, locale === 'fr' ? 'fr-FR' : 'en-US')}
                    </span>
                    <Button size="sm" className="bg-brand-primary hover:bg-brand-hover text-white" disabled={isOOS}
                      onClick={() => { addItem(product.id); toast.success(name + (locale === 'fr' ? ' ajouté' : ' added')); }}>
                      <ShoppingCart className="w-4 h-4" />
                    </Button>
                  </div>
                </CardContent>
              </Card>
            );
          })}
        </div>

        {totalPages > 1 && (
          <div className="flex items-center justify-center gap-2 mt-8">
            <Button variant="outline" size="sm" disabled={page === 1} onClick={() => setPage(p => p - 1)}>{t('common.previous')}</Button>
            <span className="text-sm text-muted-foreground">{t('common.page')} {page} {t('common.of')} {totalPages}</span>
            <Button variant="outline" size="sm" disabled={page === totalPages} onClick={() => setPage(p => p + 1)}>{t('common.next')}</Button>
          </div>
        )}
      </div>
    </div>
  );
}
