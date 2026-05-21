'use client';

import { useState, useEffect } from 'react';
import Link from 'next/link';
import Image from 'next/image';
import { Loader2 } from 'lucide-react';
import { Card } from '@/components/ui/card';
import { useI18n } from '@/context/i18n-context';
import { categoriesService } from '@/lib/api-services';
import type { CategoryDto } from '@/lib/api-types';
import { toLocalized, getCategoryImageUrl } from '@/lib/api-types';

export default function CategoriesPage() {
  const { t, localized } = useI18n();
  const [categories, setCategories] = useState<CategoryDto[]>([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    categoriesService.getAll()
      .then(data => setCategories(data.filter(c => c.active).sort((a, b) => a.displayOrder - b.displayOrder)))
      .finally(() => setLoading(false));
  }, []);

  if (loading) {
    return <div className="flex items-center justify-center py-32"><Loader2 className="w-8 h-8 animate-spin text-brand-primary" /></div>;
  }

  return (
    <div className="container mx-auto px-4 py-8">
      <h1 className="text-3xl text-brand-dark mb-8">{t('nav.categories')}</h1>
      <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4 gap-6">
        {categories.map((cat) => (
          <Link key={cat.id} href={`/category/${cat.slug}`} className="group">
            <Card className="overflow-hidden border-0 shadow-md hover:shadow-xl transition-shadow">
              <div className="relative h-48 md:h-56">
                <Image src={getCategoryImageUrl(cat)} alt={localized(toLocalized(cat.nameFr, cat.nameEn, cat.nameMs, cat.nameAr))} fill className="object-cover group-hover:scale-105 transition-transform duration-300" />
                <div className="absolute inset-0 bg-gradient-to-t from-brand-dark/80 via-brand-dark/20 to-transparent" />
                <div className="absolute bottom-4 start-4 end-4 text-white">
                  <h2 className="text-lg font-semibold mb-1">{localized(toLocalized(cat.nameFr, cat.nameEn, cat.nameMs, cat.nameAr))}</h2>
                  <p className="text-sm text-gray-200 line-clamp-2">{localized(toLocalized(cat.descriptionFr, cat.descriptionEn, cat.descriptionMs, cat.descriptionAr))}</p>
                </div>
              </div>
            </Card>
          </Link>
        ))}
      </div>
    </div>
  );
}
