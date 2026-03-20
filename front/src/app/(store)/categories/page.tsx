'use client';

import Link from 'next/link';
import Image from 'next/image';
import { Card } from '@/components/ui/card';
import { useI18n } from '@/context/i18n-context';
import { categories, getCategoryImage } from '@/mock';

export default function CategoriesPage() {
  const { t, localized } = useI18n();
  const activeCategories = categories.filter(c => c.active).sort((a, b) => a.displayOrder - b.displayOrder);

  return (
    <div className="container mx-auto px-4 py-8">
      <h1 className="text-3xl text-brand-dark mb-8">{t('nav.categories')}</h1>
      <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4 gap-6">
        {activeCategories.map((cat) => (
          <Link key={cat.id} href={`/category/${cat.slug}`} className="group">
            <Card className="overflow-hidden border-0 shadow-md hover:shadow-xl transition-shadow">
              <div className="relative h-48 md:h-56">
                <Image src={getCategoryImage(cat.id)} alt={localized(cat.name)} fill className="object-cover group-hover:scale-105 transition-transform duration-300" />
                <div className="absolute inset-0 bg-gradient-to-t from-brand-dark/80 via-brand-dark/20 to-transparent" />
                <div className="absolute bottom-4 left-4 right-4 text-white">
                  <h2 className="text-lg font-semibold mb-1">{localized(cat.name)}</h2>
                  <p className="text-sm text-gray-200 line-clamp-2">{localized(cat.description)}</p>
                </div>
              </div>
            </Card>
          </Link>
        ))}
      </div>
    </div>
  );
}
