'use client';

import { useState, useEffect, useCallback, Suspense } from 'react';
import { useSearchParams } from 'next/navigation';
import Link from 'next/link';
import Image from 'next/image';
import { ShoppingCart, SlidersHorizontal, Loader2 } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Card, CardContent } from '@/components/ui/card';
import { Badge } from '@/components/ui/badge';
import { Checkbox } from '@/components/ui/checkbox';
import { Label } from '@/components/ui/label';
import { Switch } from '@/components/ui/switch';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';
import { Sheet, SheetContent, SheetTrigger } from '@/components/ui/sheet';
import { Separator } from '@/components/ui/separator';
import { useI18n } from '@/context/i18n-context';
import { useCart } from '@/context/cart-context';
import { productsService, categoriesService } from '@/lib/api-services';
import type { ProductDto, CategoryDto } from '@/lib/api-types';
import { toLocalized, getProductImageUrl } from '@/lib/api-types';
import { formatPrice, calculateTTC } from '@/lib/money';
import { ProductStatus, StockStatus, VatRate } from '@/lib/enums';
import { toast } from 'sonner';

const VAT_RATE_VALUES: Record<number, number> = {
  [VatRate.Standard]: 0.20, [VatRate.Intermediate]: 0.10, [VatRate.Reduced]: 0.055, [VatRate.Zero]: 0,
};

interface FilterPanelProps {
  categories: CategoryDto[];
  selectedCategories: string[];
  toggleCategory: (catId: string) => void;
  priceMin: string;
  setPriceMin: (v: string) => void;
  priceMax: string;
  setPriceMax: (v: string) => void;
  availableOnly: boolean;
  setAvailableOnly: (v: boolean) => void;
  onReset: () => void;
  t: (key: string) => string;
  localized: (obj: Record<string, string> | undefined) => string;
}

function FilterPanel({
  categories,
  selectedCategories,
  toggleCategory,
  priceMin,
  setPriceMin,
  priceMax,
  setPriceMax,
  availableOnly,
  setAvailableOnly,
  onReset,
  t,
  localized,
}: FilterPanelProps) {
  return (
    <div className="space-y-6">
      <div>
        <Label className="text-sm font-semibold mb-2 block">{t('search.categories')}</Label>
        <div className="space-y-2">
          {categories.map(cat => (
            <div key={cat.id} className="flex items-center gap-2">
              <Checkbox id={cat.id} checked={selectedCategories.includes(cat.id)} onCheckedChange={() => toggleCategory(cat.id)} />
              <Label htmlFor={cat.id} className="text-sm font-normal cursor-pointer">{localized(toLocalized(cat.nameFr, cat.nameEn))}</Label>
            </div>
          ))}
        </div>
      </div>
      <Separator />
      <div>
        <Label className="text-sm font-semibold mb-2 block">{t('common.price')}</Label>
        <div className="flex gap-2">
          <Input type="number" placeholder={t('search.price_min')} value={priceMin} onChange={e => setPriceMin(e.target.value)} className="w-full" />
          <Input type="number" placeholder={t('search.price_max')} value={priceMax} onChange={e => setPriceMax(e.target.value)} className="w-full" />
        </div>
      </div>
      <Separator />
      <div className="flex items-center gap-2">
        <Switch id="available" checked={availableOnly} onCheckedChange={setAvailableOnly} />
        <Label htmlFor="available" className="text-sm cursor-pointer">{t('search.available_only')}</Label>
      </div>
      <Button variant="outline" className="w-full" onClick={onReset}>
        {t('search.reset')}
      </Button>
    </div>
  );
}

function SearchContent() {
  const searchParams = useSearchParams();
  const { t, localized, locale } = useI18n();
  const { addItem } = useCart();
  const fmt = (n: number) => formatPrice(n, locale === 'fr' ? 'fr-FR' : 'en-US');

  const [query, setQuery] = useState(searchParams.get('q') || '');
  const [priceMin, setPriceMin] = useState('');
  const [priceMax, setPriceMax] = useState('');
  const [selectedCategories, setSelectedCategories] = useState<string[]>([]);
  const [availableOnly, setAvailableOnly] = useState(false);
  const [sort, setSort] = useState('relevance');
  const [categories, setCategories] = useState<CategoryDto[]>([]);
  const [results, setResults] = useState<ProductDto[]>([]);
  const [loading, setLoading] = useState(true);

  // Load categories once
  useEffect(() => {
    categoriesService.getAll().then(cats => setCategories(cats.filter(c => c.active)));
  }, []);

  // Search products
  const doSearch = useCallback(async () => {
    setLoading(true);
    try {
      let data: ProductDto[];
      if (query.trim()) {
        const res = await productsService.search(query, 1, 100);
        data = res.data;
      } else {
        const res = await productsService.getAll(1, 100);
        data = res.data;
      }

      // Client-side filters
      let filtered = data.filter(p => p.status === ProductStatus.Active);
      if (selectedCategories.length > 0) {
        filtered = filtered.filter(p => p.categories.some(c => selectedCategories.includes(c.id)));
      }
      if (availableOnly) {
        filtered = filtered.filter(p => p.stockStatus !== StockStatus.OutOfStock);
      }
      if (priceMin) {
        const min = parseFloat(priceMin);
        filtered = filtered.filter(p => calculateTTC(p.priceHT, VAT_RATE_VALUES[p.vatRate] ?? 0.20) >= min);
      }
      if (priceMax) {
        const max = parseFloat(priceMax);
        filtered = filtered.filter(p => calculateTTC(p.priceHT, VAT_RATE_VALUES[p.vatRate] ?? 0.20) <= max);
      }
      switch (sort) {
        case 'price_asc': filtered.sort((a, b) => a.priceHT - b.priceHT); break;
        case 'price_desc': filtered.sort((a, b) => b.priceHT - a.priceHT); break;
        case 'newest': filtered.sort((a, b) => new Date(b.updatedAt).getTime() - new Date(a.updatedAt).getTime()); break;
      }
      setResults(filtered);
    } catch {
      toast.error(t('common.error'));
    } finally {
      setLoading(false);
    }
  }, [query, selectedCategories, availableOnly, priceMin, priceMax, sort, t]);

  useEffect(() => {
    const timer = setTimeout(doSearch, 300);
    return () => clearTimeout(timer);
  }, [doSearch]);

  const toggleCategory = useCallback((catId: string) => {
    setSelectedCategories(prev => prev.includes(catId) ? prev.filter(c => c !== catId) : [...prev, catId]);
  }, []);

  const resetFilters = useCallback(() => {
    setQuery('');
    setPriceMin('');
    setPriceMax('');
    setSelectedCategories([]);
    setAvailableOnly(false);
  }, []);

  const filterPanelProps: FilterPanelProps = {
    categories,
    selectedCategories,
    toggleCategory,
    priceMin,
    setPriceMin,
    priceMax,
    setPriceMax,
    availableOnly,
    setAvailableOnly,
    onReset: resetFilters,
    t,
    localized,
  };

  return (
    <div className="container mx-auto px-4 py-8">
      <h1 className="text-3xl text-brand-dark mb-6">{t('search.title')}</h1>
      <div className="flex gap-4 mb-6">
        <Input type="search" placeholder={t('header.search_placeholder')} value={query} onChange={e => setQuery(e.target.value)} className="flex-1" />
        <Select value={sort} onValueChange={setSort}>
          <SelectTrigger className="w-[200px]"><SelectValue /></SelectTrigger>
          <SelectContent>
            <SelectItem value="relevance">{t('category.sort.priority')}</SelectItem>
            <SelectItem value="price_asc">{t('category.sort.price_asc')}</SelectItem>
            <SelectItem value="price_desc">{t('category.sort.price_desc')}</SelectItem>
            <SelectItem value="newest">{t('category.sort.newest')}</SelectItem>
          </SelectContent>
        </Select>
        <Sheet>
          <SheetTrigger asChild>
            <Button variant="outline" className="md:hidden"><SlidersHorizontal className="w-4 h-4" /></Button>
          </SheetTrigger>
          <SheetContent side="left" className="w-[300px]">
            <h2 className="text-lg font-semibold mb-4">{t('search.filters')}</h2>
            <FilterPanel {...filterPanelProps} />
          </SheetContent>
        </Sheet>
      </div>

      <div className="flex gap-8">
        <aside className="hidden md:block w-64 shrink-0">
          <h2 className="text-lg font-semibold mb-4">{t('search.filters')}</h2>
          <FilterPanel {...filterPanelProps} />
        </aside>

        <div className="flex-1">
          {loading ? (
            <div className="flex justify-center py-16"><Loader2 className="w-8 h-8 animate-spin text-brand-primary" /></div>
          ) : (
            <>
              <p className="text-sm text-muted-foreground mb-4">{results.length} {t('search.results')}</p>
              {results.length === 0 ? (
                <div className="text-center py-16"><p className="text-lg text-muted-foreground">{t('search.no_results')}</p></div>
              ) : (
                <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-4">
                  {results.map(product => {
                    const vatRate = VAT_RATE_VALUES[product.vatRate] ?? 0.20;
                    const price = calculateTTC(product.priceHT, vatRate);
                    const isOOS = product.stockStatus === StockStatus.OutOfStock;
                    const name = localized(toLocalized(product.nameFr, product.nameEn));
                    return (
                      <Card key={product.id} className={`overflow-hidden hover:shadow-md transition-shadow ${isOOS ? 'opacity-60' : ''}`}>
                        <Link href={`/product/${product.slug}`}>
                          <div className="relative h-40">
                            <Image src={getProductImageUrl(product)} alt={name} fill className="object-cover" />
                            <div className="absolute top-2 start-2 flex gap-1">
                              {product.isNew && <Badge className="bg-brand-primary text-white text-xs">{t('product.new')}</Badge>}
                              {isOOS && <Badge className="bg-error text-white text-xs">{t('product.out_of_stock')}</Badge>}
                            </div>
                          </div>
                        </Link>
                        <CardContent className="p-3">
                          <Link href={`/product/${product.slug}`}>
                            <h3 className="text-sm font-medium line-clamp-2 hover:text-brand-primary">{name}</h3>
                          </Link>
                          <div className="flex items-center justify-between mt-2">
                            <span className="font-bold text-brand-dark">{fmt(price)}</span>
                            <Button size="sm" variant="ghost" className="text-brand-primary" disabled={isOOS}
                              onClick={() => { addItem(product.id); toast.success(name + (locale === 'fr' ? ' ajouté' : ' added')); }}>
                              <ShoppingCart className="w-4 h-4" />
                            </Button>
                          </div>
                        </CardContent>
                      </Card>
                    );
                  })}
                </div>
              )}
            </>
          )}
        </div>
      </div>
    </div>
  );
}

export default function SearchPage() {
  return (
    <Suspense fallback={<div className="container mx-auto px-4 py-16 text-center">Loading...</div>}>
      <SearchContent />
    </Suspense>
  );
}
