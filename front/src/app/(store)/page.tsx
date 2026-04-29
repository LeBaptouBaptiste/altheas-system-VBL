'use client';

import { useState, useEffect } from 'react';
import Link from 'next/link';
import Image from 'next/image';
import { ArrowRight, ShoppingCart } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { Card, CardContent } from '@/components/ui/card';
import { Badge } from '@/components/ui/badge';
import { useI18n } from '@/context/i18n-context';
import { useCart } from '@/context/cart-context';
import { products, categories, heroSlides, marketingText, imageUrls, getCategoryImage, getProductImage } from '@/mock';
import { formatPrice, toIntlLocale } from '@/lib/money';
import { VAT_RATES } from '@/lib/constants';
import { toast } from 'sonner';

export default function HomePage() {
  const { t, localized, locale } = useI18n();
  const { addItem } = useCart();
  const [currentSlide, setCurrentSlide] = useState(0);
  // Pauses the carousel autoplay. Required for WCAG 2.2.2 ("Pause, Stop,
  // Hide") on auto-rotating content; also a nicer UX when the user is
  // reading a slide. Triggered on hover and on keyboard focus so both
  // mouse and keyboard users benefit.
  const [paused, setPaused] = useState(false);

  const slides = heroSlides;
  const topProducts = products.filter(p => p.priorityRank > 0 && p.status === 'published').sort((a, b) => a.priorityRank - b.priorityRank).slice(0, 8);
  const activeCategories = categories.filter(c => c.active).sort((a, b) => a.displayOrder - b.displayOrder);

  useEffect(() => {
    if (paused) return;
    const timer = setInterval(() => {
      setCurrentSlide((prev) => (prev + 1) % slides.length);
    }, 5000);
    return () => clearInterval(timer);
  }, [slides.length, paused]);

  const heroImageMap: Record<string, string> = {
    slide1: imageUrls.hero.slide1,
    slide2: imageUrls.hero.slide2,
    slide3: imageUrls.hero.slide3,
  };

  const handleAddToCart = (productId: string, name: string) => {
    addItem(productId);
    const suffix: Record<string, string> = { fr: 'ajouté au panier', en: 'added to cart', ms: 'ditambah ke troli', ar: 'أُضيف إلى عربة التسوق' };
    toast.success(`${name} ${suffix[locale] ?? 'added to cart'}`);
  };

  return (
    <div>
      {/* Hero Carousel */}
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
            <Image src={heroImageMap[slide.image] || imageUrls.hero.slide1} alt={localized(slide.title)} fill className="object-cover" priority={index === 0} />
            <div className="absolute inset-0 z-20 flex items-center">
              <div className="container mx-auto px-4">
                <div className="max-w-2xl text-white">
                  <h1 className="text-3xl md:text-5xl mb-3">{localized(slide.title)}</h1>
                  <p className="text-lg md:text-2xl mb-2 text-brand-primary">{localized(slide.subtitle)}</p>
                  <p className="text-base md:text-lg mb-6 text-gray-200">{localized(slide.description)}</p>
                  <Link href={slide.link}>
                    <Button size="lg" className="bg-brand-primary hover:bg-brand-hover text-white px-8">
                      {localized(slide.cta)}
                      <ArrowRight className="ml-2 w-5 h-5" />
                    </Button>
                  </Link>
                </div>
              </div>
            </div>
          </div>
        ))}
        <div className="absolute bottom-4 left-1/2 -translate-x-1/2 z-30 flex gap-2">
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

      {/* Marketing Text */}
      <section className="container mx-auto px-4 py-10 text-center">
        <p className="text-lg text-muted-foreground max-w-3xl mx-auto" dangerouslySetInnerHTML={{ __html: localized(marketingText).replace(/\*\*(.*?)\*\*/g, '<strong class="text-brand-dark">$1</strong>') }} />
      </section>

      {/* Categories Grid */}
      <section className="container mx-auto px-4 pb-12">
        <div className="flex items-center justify-between mb-6">
          <h2 className="text-2xl text-brand-dark">{t('home.categories')}</h2>
          <Link href="/categories" className="text-brand-primary hover:text-brand-hover text-sm font-medium flex items-center gap-1">
            {t('home.view_all')} <ArrowRight className="w-4 h-4" />
          </Link>
        </div>
        <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
          {activeCategories.map((cat) => (
            <Link key={cat.id} href={`/category/${cat.slug}`} className="group">
              <Card className="overflow-hidden border-0 shadow-md hover:shadow-lg transition-shadow">
                <div className="relative h-36 md:h-48">
                  <Image src={getCategoryImage(cat.id)} alt={localized(cat.name)} fill className="object-cover group-hover:scale-105 transition-transform duration-300" />
                  <div className="absolute inset-0 bg-gradient-to-t from-brand-dark/80 to-transparent" />
                  <h3 className="absolute bottom-3 left-3 right-3 text-white text-sm md:text-base font-semibold">
                    {localized(cat.name)}
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
              const priceTTC = product.priceHT * (1 + VAT_RATES[product.vatRate]);
              return (
                <Card key={product.id} className="overflow-hidden hover:shadow-lg transition-shadow group">
                  <Link href={`/product/${product.slug}`}>
                    <div className="relative h-48">
                      <Image src={getProductImage(product.images[0])} alt={localized(product.name)} fill className="object-cover group-hover:scale-105 transition-transform duration-300" />
                      <div className="absolute top-2 left-2 flex gap-1">
                        {product.isNew && <Badge className="bg-brand-primary text-white">{t('product.new')}</Badge>}
                        {product.stockStatus === 'low_stock' && <Badge className="bg-warning text-white">{t('product.low_stock')}</Badge>}
                        {product.stockStatus === 'out_of_stock' && <Badge className="bg-error text-white">{t('product.out_of_stock')}</Badge>}
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
                      <span className="text-lg font-bold text-brand-dark">{formatPrice(priceTTC, toIntlLocale(locale))}</span>
                      <Button
                        size="sm"
                        className="bg-brand-primary hover:bg-brand-hover text-white"
                        disabled={product.stockStatus === 'out_of_stock'}
                        onClick={(e) => { e.preventDefault(); handleAddToCart(product.id, localized(product.name)); }}
                        aria-label={`${t('product.add_to_cart')} - ${localized(product.name)}`}
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
