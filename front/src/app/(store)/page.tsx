'use client';

import { useState, useEffect } from 'react';
import Link from 'next/link';
import Image from 'next/image';
import { ArrowRight, ShoppingCart, Loader2 } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { Card, CardContent } from '@/components/ui/card';
import { Badge } from '@/components/ui/badge';
import { useI18n } from '@/context/i18n-context';
import { useCart } from '@/context/cart-context';
import { contentService, productsService, categoriesService } from '@/lib/api-services';
import type { HeroSlideDto, ProductDto, CategoryDto } from '@/lib/api-types';
import { toLocalized, getProductImageUrl, getCategoryImageUrl, getImageUrl } from '@/lib/api-types';
import { formatPrice, toIntlLocale } from '@/lib/money';
import { ProductStatus, StockStatus, VatRate } from '@/lib/enums';
import { toast } from 'sonner';

const VAT_RATE_VALUES: Record<number, number> = {
  [VatRate.Standard]: 0.20,
  [VatRate.Intermediate]: 0.10,
  [VatRate.Reduced]: 0.055,
  [VatRate.Zero]: 0,
};

export default function HomePage() {
  const { t, localized, locale } = useI18n();
  const { addItem } = useCart();
  const [currentSlide, setCurrentSlide] = useState(0);
  const [slides, setSlides] = useState<HeroSlideDto[]>([]);
  const [topProducts, setTopProducts] = useState<ProductDto[]>([]);
  const [categories, setCategories] = useState<CategoryDto[]>([]);
  const [loading, setLoading] = useState(true);
  // Pauses the carousel autoplay. Required for WCAG 2.2.2 ("Pause, Stop,
  // Hide") on auto-rotating content; also a nicer UX when the user is
  // reading a slide. Triggered on hover and on keyboard focus so both
  // mouse and keyboard users benefit.
  const [paused, setPaused] = useState(false);

  useEffect(() => {
    Promise.all([
      contentService.getSlides(true),
      productsService.getAll(1, 8),
      categoriesService.getAll(),
    ])
      .then(([slidesData, productsData, categoriesData]) => {
        setSlides(slidesData);
        setTopProducts(productsData.data.filter(p => p.status === ProductStatus.Active).slice(0, 8));
        setCategories(categoriesData.filter(c => c.active).sort((a, b) => a.displayOrder - b.displayOrder));
      })
      .catch(() => toast.error(t('common.error')))
      .finally(() => setLoading(false));
  }, [t]);

  useEffect(() => {
    if (paused || slides.length === 0) return;
    const timer = setInterval(() => {
      setCurrentSlide((prev) => (prev + 1) % slides.length);
    }, 5000);
    return () => clearInterval(timer);
  }, [slides.length, paused]);

  const handleAddToCart = (productId: string, name: string) => {
    addItem(productId);
    toast.success(t('cart.added_to_cart').replace('{name}', name));
  };

  if (loading) {
    return (
      <div className="flex items-center justify-center py-32">
        <Loader2 className="w-8 h-8 animate-spin text-brand-primary" />
      </div>
    );
  }

  return (
    <div>
      {/* Hero Carousel */}
      {slides.length > 0 && (
        <section
          className="relative h-[400px] md:h-[550px] overflow-hidden bg-brand-light"
          aria-label="Hero carousel"
          aria-roledescription="carousel"
          onMouseEnter={() => setPaused(true)}
          onMouseLeave={() => setPaused(false)}
          onFocus={() => setPaused(true)}
          onBlur={() => setPaused(false)}
        >
          {slides.map((slide, index) => (
            <div key={slide.id} className={`absolute inset-0 transition-opacity duration-700 ${index === currentSlide ? 'opacity-100' : 'opacity-0 pointer-events-none'}`}>
              <div className="absolute inset-0 bg-gradient-to-r from-brand-dark/90 to-brand-dark/40 z-10" />
              <Image src={getImageUrl(slide.image)} alt={localized(toLocalized(slide.titleFr, slide.titleEn))} fill className="object-cover" priority={index === 0} />
              <div className="absolute inset-0 z-20 flex items-center">
                <div className="container mx-auto px-4">
                  <div className="max-w-2xl text-white">
                    <h1 className="text-3xl md:text-5xl mb-3">{localized(toLocalized(slide.titleFr, slide.titleEn))}</h1>
                    <p className="text-lg md:text-2xl mb-2 text-brand-primary">{localized(toLocalized(slide.subtitleFr, slide.subtitleEn))}</p>
                    <p className="text-base md:text-lg mb-6 text-gray-200">{localized(toLocalized(slide.descriptionFr, slide.descriptionEn, slide.descriptionMs, slide.descriptionAr))}</p>
                    <Link href={slide.link}>
                      <Button size="lg" className="bg-brand-primary hover:bg-brand-hover text-white px-8">
                        {localized(toLocalized(slide.ctaFr, slide.ctaEn))}
                        <ArrowRight className="ms-2 w-5 h-5" />
                      </Button>
                    </Link>
                  </div>
                </div>
              </div>
            </div>
          ))}
          <div className="absolute bottom-4 start-1/2 -translate-x-1/2 z-30 flex gap-2">
            {slides.map((_, i) => (
              <button
                key={i}
                onClick={() => setCurrentSlide(i)}
                className={`w-3 h-3 rounded-full transition-colors ${i === currentSlide ? 'bg-brand-primary' : 'bg-white/50'}`}
                aria-label={`Slide ${i + 1}`}
                aria-current={i === currentSlide ? 'true' : undefined}
              />
            ))}
          </div>
        </section>
      )}

      {/* Categories Grid */}
      <section className="container mx-auto px-4 py-12">
        <div className="flex items-center justify-between mb-6">
          <h2 className="text-2xl text-brand-dark">{t('home.categories')}</h2>
          <Link href="/categories" className="text-brand-primary hover:text-brand-hover text-sm font-medium flex items-center gap-1">
            {t('home.view_all')} <ArrowRight className="w-4 h-4" />
          </Link>
        </div>
        <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
          {categories.map((cat) => (
            <Link key={cat.id} href={`/category/${cat.slug}`} className="group">
              <Card className="overflow-hidden border-0 shadow-md hover:shadow-lg transition-shadow">
                <div className="relative h-36 md:h-48">
                  <Image src={getCategoryImageUrl(cat)} alt={localized(toLocalized(cat.nameFr, cat.nameEn, cat.nameMs, cat.nameAr))} fill className="object-cover group-hover:scale-105 transition-transform duration-300" />
                  <div className="absolute inset-0 bg-gradient-to-t from-brand-dark/80 to-transparent" />
                  <h3 className="absolute bottom-3 start-3 end-3 text-white text-sm md:text-base font-semibold">
                    {localized(toLocalized(cat.nameFr, cat.nameEn, cat.nameMs, cat.nameAr))}
                  </h3>
                </div>
              </Card>
            </Link>
          ))}
        </div>
      </section>

      {/* Top Products */}
      <section className="bg-gray-50 py-12">
        <div className="container mx-auto px-4">
          <div className="flex items-center justify-between mb-6">
            <h2 className="text-2xl text-brand-dark">{t('home.top_products')}</h2>
            <Link href="/search" className="text-brand-primary hover:text-brand-hover text-sm font-medium flex items-center gap-1">
              {t('home.view_all')} <ArrowRight className="w-4 h-4" />
            </Link>
          </div>
          <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-6">
            {topProducts.map((product) => {
              const vatRate = VAT_RATE_VALUES[product.vatRate] ?? 0.20;
              const priceTTC = product.priceHT * (1 + vatRate);
              const name = localized(toLocalized(product.nameFr, product.nameEn, product.nameMs, product.nameAr));
              return (
                <Card key={product.id} className="overflow-hidden hover:shadow-lg transition-shadow group">
                  <Link href={`/product/${product.slug}`}>
                    <div className="relative h-48">
                      <Image src={getProductImageUrl(product)} alt={name} fill className="object-cover group-hover:scale-105 transition-transform duration-300" />
                      <div className="absolute top-2 start-2 flex gap-1">
                        {product.isNew && <Badge className="bg-brand-primary text-white">{t('product.new')}</Badge>}
                        {product.stockStatus === StockStatus.LowStock && <Badge className="bg-warning text-white">{t('product.low_stock')}</Badge>}
                        {product.stockStatus === StockStatus.OutOfStock && <Badge className="bg-error text-white">{t('product.out_of_stock')}</Badge>}
                      </div>
                    </div>
                  </Link>
                  <CardContent className="p-4">
                    <Link href={`/product/${product.slug}`}>
                      <h3 className="font-semibold text-brand-dark text-sm mb-1 line-clamp-2 hover:text-brand-primary transition-colors">
                        {name}
                      </h3>
                    </Link>
                    <p className="text-xs text-muted-foreground mb-3 line-clamp-2">{localized(toLocalized(product.descriptionFr, product.descriptionEn, product.descriptionMs, product.descriptionAr))}</p>
                    <div className="flex items-center justify-between">
                      <span className="text-lg font-bold text-brand-dark">{formatPrice(priceTTC, toIntlLocale(locale))}</span>
                      <Button
                        size="sm"
                        className="bg-brand-primary hover:bg-brand-hover text-white"
                        disabled={product.stockStatus === StockStatus.OutOfStock}
                        onClick={(e) => { e.preventDefault(); handleAddToCart(product.id, name); }}
                        aria-label={`${t('product.add_to_cart')} - ${name}`}
                      >
                        <ShoppingCart className="w-4 h-4" />
                      </Button>
                    </div>
                  </CardContent>
                </Card>
              );
            })}
          </div>
        </div>
      </section>
    </div>
  );
}
