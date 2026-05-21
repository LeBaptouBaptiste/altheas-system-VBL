'use client';

import { use, useState, useEffect } from 'react';
import { notFound } from 'next/navigation';
import { Loader2 } from 'lucide-react';
import { useI18n } from '@/context/i18n-context';
import { contentService } from '@/lib/api-services';
import type { StaticPageDto } from '@/lib/api-types';
import { toLocalized } from '@/lib/api-types';

export default function StaticPage({ params }: { params: Promise<{ slug: string }> }) {
  const { slug } = use(params);
  const { localized } = useI18n();
  const [page, setPage] = useState<StaticPageDto | null>(null);
  const [loading, setLoading] = useState(true);
  const [notFoundState, setNotFoundState] = useState(false);

  useEffect(() => {
    setLoading(true);
    contentService.getPageBySlug(slug)
      .then(setPage)
      .catch(() => setNotFoundState(true))
      .finally(() => setLoading(false));
  }, [slug]);

  if (loading) return <div className="flex items-center justify-center py-32"><Loader2 className="w-8 h-8 animate-spin text-brand-primary" /></div>;
  if (notFoundState || !page) return notFound();

  const content = localized(toLocalized(page.contentFr, page.contentEn, page.contentMs, page.contentAr));

  return (
    <div className="container mx-auto px-4 py-10 max-w-3xl">
      <article className="prose prose-sm sm:prose max-w-none">
        {content.split('\n').map((line, i) => {
          if (line.startsWith('# ')) return <h1 key={i} className="text-2xl md:text-3xl text-brand-dark mb-6">{line.slice(2)}</h1>;
          if (line.startsWith('## ')) return <h2 key={i} className="text-xl text-brand-dark mt-8 mb-3">{line.slice(3)}</h2>;
          if (line.startsWith('- **')) {
            const match = line.match(/^- \*\*(.+?)\*\*\s*:?\s*(.*)$/);
            if (match) return <div key={i} className="flex gap-2 mb-2 ms-4"><span className="font-semibold text-brand-dark">{match[1]}:</span><span className="text-muted-foreground">{match[2]}</span></div>;
          }
          if (line.startsWith('- ')) return <div key={i} className="flex gap-2 mb-1 ms-4"><span className="text-brand-primary">•</span><span className="text-muted-foreground">{line.slice(2)}</span></div>;
          if (line.startsWith('**') && line.endsWith('**')) return <p key={i} className="font-bold text-brand-dark my-2">{line.slice(2, -2)}</p>;
          if (line.startsWith('*') && line.endsWith('*')) return <p key={i} className="text-xs text-muted-foreground italic mt-6">{line.slice(1, -1)}</p>;
          if (line.trim() === '') return <div key={i} className="h-2" />;
          return <p key={i} className="text-muted-foreground mb-2 leading-relaxed">{line}</p>;
        })}
      </article>
    </div>
  );
}
