'use client';

import { useState, useMemo, Suspense } from 'react';
import { useSearchParams } from 'next/navigation';
import Link from 'next/link';
import Image from 'next/image';
import { ShoppingCart, SlidersHorizontal } from 'lucide-react';
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
import { products, categories, getProductImage } from '@/mock';
import { formatPrice, calculateTTC } from '@/lib/money';
import { VAT_RATES } from '@/lib/constants';
import { getMatchPriority, type MatchPriority } from '@/lib/search';
import { toast } from 'sonner';

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

  const toggleCategory = (catId: string) => {
    setSelectedCategories(prev => prev.includes(catId) ? prev.filter(c => c !== catId) : [...prev, catId]);
  };

  const results = useMemo(() => {
    let filtered = products.filter(p => p.status === 'published');

    // Category filter
    if (selectedCategories.length > 0) {
      filtered = filtered.filter(p => p.categories.some(c => selectedCategories.includes(c)));
    }

    // Availability filter
    if (availableOnly) {
      filtered = filtered.filter(p => p.stockStatus !== 'out_of_stock');
    }

    // Price filter (TTC)
    if (priceMin) {
      const min = parseFloat(priceMin);
      filtered = filtered.filter(p => calculateTTC(p.priceHT, VAT_RATES[p.vatRate]) >= min);
    }
    if (priceMax) {
      const max = parseFloat(priceMax);
      filtered = filtered.filter(p => calculateTTC(p.priceHT, VAT_RATES[p.vatRate]) <= max);
    }

    // Text search with priority matching
    if (query.trim()) {
      const scored = filtered.map(p => {
        const namePri = getMatchPriority(query, localized(p.name));
        const descPri = getMatchPriority(query, localized(p.description));
        const longDescPri = getMatchPriority(query, localized(p.longDescription));
        let specPri: MatchPriority = 0;
        for (const s of p.specs) {
          const sp = getMatchPriority(query, `${s.label} ${s.value}`);
          if (sp > 0 && (specPri === 0 || sp < specPri)) specPri = sp as MatchPriority;
        }
        const priorities = [namePri, descPri, longDescPri, specPri].filter(x => x > 0);
        const best = priorities.length > 0 ? Math.min(...priorities) as MatchPriority : 0;
        return { product: p, priority: best };
      }).filter(r => r.priority > 0);

      scored.sort((a, b) => a.priority - b.priority);
      filtered = scored.map(s => s.product);
    }

    // Sort
    switch (sort) {
      case 'price_asc': filtered.sort((a, b) => a.priceHT - b.priceHT); break;
      case 'price_desc': filtered.sort((a, b) => b.priceHT - a.priceHT); break;
      case 'newest': filtered.sort((a, b) => new Date(b.updatedAt).getTime() - new Date(a.updatedAt).getTime()); break;
      case 'availability': filtered.sort((a, b) => {
        const o = { in_stock: 0, low_stock: 1, out_of_stock: 2 };
        return o[a.stockStatus] - o[b.stockStatus];
      }); break;
    }

    return filtered;
  }, [query, priceMin, priceMax, selectedCategories, availableOnly, sort, localized]);

  const FilterPanel = () => (
    <div className="space-y-6">
      <div>
        <Label className="text-sm font-semibold mb-2 block">{t('search.categories')}</Label>
        <div className="space-y-2">
          {categories.filter(c => c.active).map(cat => (
            <div key={cat.id} className="flex items-center gap-2">
              <Checkbox id={cat.id} checked={selectedCategories.includes(cat.id)} onCheckedChange={() => toggleCategory(cat.id)} />
              <Label htmlFor={cat.id} className="text-sm font-normal cursor-pointer">{localized(cat.name)}</Label>
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
      <Button variant="outline" className="w-full" onClick={() => { setQuery(''); setPriceMin(''); setPriceMax(''); setSelectedCategories([]); setAvailableOnly(false); }}>
        {t('search.reset')}
      </Button>
    </div>
  );

  return (
    <div className="container mx-auto px-4 py-8">
      <h1 className="text-3xl text-brand-dark mb-6">{t('search.title')}</h1>

      {/* Search Bar */}
      <div className="flex gap-4 mb-6">
        <Input type="search" placeholder={t('header.search_placeholder')} value={query} onChange={e => setQuery(e.target.value)} className="flex-1" />
        <Select value={sort} onValueChange={setSort}>
          <SelectTrigger className="w-[200px]"><SelectValue /></SelectTrigger>
          <SelectContent>
            <SelectItem value="relevance">{t('category.sort.priority')}</SelectItem>
            <SelectItem value="price_asc">{t('category.sort.price_asc')}</SelectItem>
            <SelectItem value="price_desc">{t('category.sort.price_desc')}</SelectItem>
            <SelectItem value="newest">{t('category.sort.newest')}</SelectItem>
            <SelectItem value="availability">{t('category.sort.availability')}</SelectItem>
          </SelectContent>
        </Select>
        {/* Mobile filter button */}
        <Sheet>
          <SheetTrigger asChild>
            <Button variant="outline" className="md:hidden"><SlidersHorizontal className="w-4 h-4" /></Button>
          </SheetTrigger>
          <SheetContent side="left" className="w-[300px]">
            <h2 className="text-lg font-semibold mb-4">{t('search.filters')}</h2>
            <FilterPanel />
          </SheetContent>
        </Sheet>
      </div>

      <div className="flex gap-8">
        {/* Desktop Filters */}
        <aside className="hidden md:block w-64 shrink-0">
          <h2 className="text-lg font-semibold mb-4">{t('search.filters')}</h2>
          <FilterPanel />
        </aside>

        {/* Results */}
        <div className="flex-1">
          <p className="text-sm text-muted-foreground mb-4">{results.length} {t('search.results')}</p>
          {results.length === 0 ? (
            <div className="text-center py-16">
              <p className="text-lg text-muted-foreground">{t('search.no_results')}</p>
            </div>
          ) : (
            <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-4">
              {results.map(product => {
                const price = calculateTTC(product.priceHT, VAT_RATES[product.vatRate]);
                const isOOS = product.stockStatus === 'out_of_stock';
                return (
                  <Card key={product.id} className={`overflow-hidden hover:shadow-md transition-shadow ${isOOS ? 'opacity-60' : ''}`}>
                    <Link href={`/product/${product.slug}`}>
                      <div className="relative h-40">
                        <Image src={getProductImage(product.images[0])} alt={localized(product.name)} fill className="object-cover" />
                        <div className="absolute top-2 left-2 flex gap-1">
                          {product.isNew && <Badge className="bg-brand-primary text-white text-xs">{t('product.new')}</Badge>}
                          {isOOS && <Badge className="bg-error text-white text-xs">{t('product.out_of_stock')}</Badge>}
                        </div>
                      </div>
                    </Link>
                    <CardContent className="p-3">
                      <Link href={`/product/${product.slug}`}>
                        <h3 className="text-sm font-medium line-clamp-2 hover:text-brand-primary">{localized(product.name)}</h3>
                      </Link>
                      <div className="flex items-center justify-between mt-2">
                        <span className="font-bold text-brand-dark">{fmt(price)}</span>
                        <Button size="sm" variant="ghost" className="text-brand-primary" disabled={isOOS}
                          onClick={() => { addItem(product.id); toast.success(localized(product.name) + (locale === 'fr' ? ' ajouté' : ' added')); }}>
                          <ShoppingCart className="w-4 h-4" />
                        </Button>
                      </div>
                    </CardContent>
                  </Card>
                );
              })}
            </div>
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
