'use client';

import { use } from 'react';
import { notFound } from 'next/navigation';
import { useI18n } from '@/context/i18n-context';
import { staticPages } from '@/mock';

export default function StaticPage({ params }: { params: Promise<{ slug: string }> }) {
  const { slug } = use(params);
  const { localized } = useI18n();

  const page = staticPages.find(p => p.slug === slug);
  if (!page) notFound();

  const content = localized(page.content);

  return (
    <div className="container mx-auto px-4 py-10 max-w-3xl">
      <article className="prose prose-sm sm:prose max-w-none">
        {content.split('\n').map((line, i) => {
          if (line.startsWith('# ')) return <h1 key={i} className="text-2xl md:text-3xl text-brand-dark mb-6">{line.slice(2)}</h1>;
          if (line.startsWith('## ')) return <h2 key={i} className="text-xl text-brand-dark mt-8 mb-3">{line.slice(3)}</h2>;
          if (line.startsWith('- **')) {
            const match = line.match(/^- \*\*(.+?)\*\*\s*:?\s*(.*)$/);
            if (match) return <div key={i} className="flex gap-2 mb-2 ml-4"><span className="font-semibold text-brand-dark">{match[1]}:</span><span className="text-muted-foreground">{match[2]}</span></div>;
          }
          if (line.startsWith('- ')) return <div key={i} className="flex gap-2 mb-1 ml-4"><span className="text-brand-primary">•</span><span className="text-muted-foreground">{line.slice(2)}</span></div>;
          if (line.startsWith('**') && line.endsWith('**')) return <p key={i} className="font-bold text-brand-dark my-2">{line.slice(2, -2)}</p>;
          if (line.startsWith('*') && line.endsWith('*')) return <p key={i} className="text-xs text-muted-foreground italic mt-6">{line.slice(1, -1)}</p>;
          if (line.trim() === '') return <div key={i} className="h-2" />;
          return <p key={i} className="text-muted-foreground mb-2 leading-relaxed">{line}</p>;
        })}
      </article>
    </div>
  );
}
