'use client';

import { use, useState, useMemo } from 'react';
import Link from 'next/link';
import Image from 'next/image';
import { ShoppingCart } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { Card, CardContent } from '@/components/ui/card';
import { Badge } from '@/components/ui/badge';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';
import { useI18n } from '@/context/i18n-context';
import { useCart } from '@/context/cart-context';
import { products, categories, getCategoryImage, getProductImage } from '@/mock';
import { formatPrice } from '@/lib/money';
import { VAT_RATES, PAGINATION } from '@/lib/constants';
import { toast } from 'sonner';

export default function CategoryPage({ params }: { params: Promise<{ slug: string }> }) {
  const { slug } = use(params);
  const { t, localized, locale } = useI18n();
  const { addItem } = useCart();
  const [sort, setSort] = useState('priority');
  const [page, setPage] = useState(1);
  const pageSize = PAGINATION.DEFAULT_PAGE_SIZE;

  const category = categories.find(c => c.slug === slug);

  const categoryProducts = useMemo(() => {
    if (!category) return [];
    let filtered = products.filter(p => p.categories.includes(category.id) && p.status === 'published');

    // Sort: priority first, then out_of_stock last, then by selected sort
    filtered.sort((a, b) => {
      // Out of stock always last
      if (a.stockStatus === 'out_of_stock' && b.stockStatus !== 'out_of_stock') return 1;
      if (b.stockStatus === 'out_of_stock' && a.stockStatus !== 'out_of_stock') return -1;

      // Priority products first
      if (sort === 'priority') {
        if (a.priorityRank > 0 && b.priorityRank === 0) return -1;
        if (b.priorityRank > 0 && a.priorityRank === 0) return 1;
        if (a.priorityRank > 0 && b.priorityRank > 0) return a.priorityRank - b.priorityRank;
      }

      switch (sort) {
        case 'price_asc': return a.priceHT - b.priceHT;
        case 'price_desc': return b.priceHT - a.priceHT;
        case 'newest': return new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime();
        case 'availability':
          const order = { in_stock: 0, low_stock: 1, out_of_stock: 2 };
          return order[a.stockStatus] - order[b.stockStatus];
        default: return 0;
      }
    });

    return filtered;
  }, [category, sort]);

  const totalPages = Math.ceil(categoryProducts.length / pageSize);
  const paginatedProducts = categoryProducts.slice((page - 1) * pageSize, page * pageSize);

  if (!category) {
    return <div className="container mx-auto px-4 py-16 text-center"><h1 className="text-2xl">{t('common.error')}</h1></div>;
  }

  return (
    <div>
      {/* Category Header */}
      <div className="relative h-48 md:h-64">
        <Image src={getCategoryImage(category.id)} alt={localized(category.name)} fill className="object-cover" />
        <div className="absolute inset-0 bg-brand-dark/70" />
        <div className="absolute inset-0 flex items-center">
          <div className="container mx-auto px-4">
            <h1 className="text-3xl md:text-4xl text-white">{localized(category.name)}</h1>
            <p className="text-gray-200 mt-2 max-w-2xl">{localized(category.description)}</p>
            <p className="text-brand-primary mt-1 text-sm">{categoryProducts.length} {t('category.products')}</p>
          </div>
        </div>
      </div>

      {/* Controls */}
      <div className="container mx-auto px-4 py-6">
        <div className="flex items-center justify-between mb-6">
          <p className="text-sm text-muted-foreground">{categoryProducts.length} {t('category.products')}</p>
          <Select value={sort} onValueChange={setSort}>
            <SelectTrigger className="w-[200px]" aria-label={t('category.sort_by')}>
              <SelectValue placeholder={t('category.sort_by')} />
            </SelectTrigger>
            <SelectContent>
              <SelectItem value="priority">{t('category.sort.priority')}</SelectItem>
              <SelectItem value="price_asc">{t('category.sort.price_asc')}</SelectItem>
              <SelectItem value="price_desc">{t('category.sort.price_desc')}</SelectItem>
              <SelectItem value="newest">{t('category.sort.newest')}</SelectItem>
              <SelectItem value="availability">{t('category.sort.availability')}</SelectItem>
            </SelectContent>
          </Select>
        </div>

        {/* Products Grid (desktop) / List (mobile) */}
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4 gap-6">
          {paginatedProducts.map((product) => {
            const priceTTC = product.priceHT * (1 + VAT_RATES[product.vatRate]);
            const isOOS = product.stockStatus === 'out_of_stock';
            return (
              <Card key={product.id} className={`overflow-hidden hover:shadow-lg transition-shadow group ${isOOS ? 'opacity-60' : ''}`}>
                <Link href={`/product/${product.slug}`}>
                  <div className="relative h-48">
                    <Image src={getProductImage(product.images[0])} alt={localized(product.name)} fill className="object-cover group-hover:scale-105 transition-transform duration-300" />
                    <div className="absolute top-2 left-2 flex gap-1 flex-wrap">
                      {product.isNew && <Badge className="bg-brand-primary text-white">{t('product.new')}</Badge>}
                      {product.stockStatus === 'in_stock' && <Badge className="bg-success text-white">{t('product.in_stock')}</Badge>}
                      {product.stockStatus === 'low_stock' && <Badge className="bg-warning text-white">{t('product.low_stock')}</Badge>}
                      {isOOS && <Badge className="bg-error text-white">{t('product.out_of_stock')}</Badge>}
                    </div>
                  </div>
                </Link>
                <CardContent className="p-4">
                  <Link href={`/product/${product.slug}`}>
                    <h3 className="font-semibold text-brand-dark text-sm mb-1 line-clamp-2 hover:text-brand-primary transition-colors">
                      {localized(product.name)}
                    </h3>
                  </Link>
                  <p className="text-xs text-muted-foreground mb-3 line-clamp-2">{localized(product.description)}</p>
                  <div className="flex items-center justify-between">
                    <span className={`text-lg font-bold ${isOOS ? 'text-muted-foreground line-through' : 'text-brand-dark'}`}>
                      {formatPrice(priceTTC, locale === 'fr' ? 'fr-FR' : 'en-US')}
                    </span>
                    <Button
                      size="sm"
                      className="bg-brand-primary hover:bg-brand-hover text-white"
                      disabled={isOOS}
                      onClick={() => { addItem(product.id); toast.success(localized(product.name) + (locale === 'fr' ? ' ajouté' : ' added')); }}
                    >
                      <ShoppingCart className="w-4 h-4" />
                    </Button>
                  </div>
                </CardContent>
              </Card>
            );
          })}
        </div>

        {/* Pagination */}
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
