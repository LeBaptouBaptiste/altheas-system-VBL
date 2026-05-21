'use client';

import { useState, useEffect } from 'react';
import { Pencil, Save, Eye, Loader2 } from 'lucide-react';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Badge } from '@/components/ui/badge';
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/components/ui/tabs';
import { Separator } from '@/components/ui/separator';
import { useI18n } from '@/context/i18n-context';
import { contentService } from '@/lib/api-services';
import { toIntlLocale } from '@/lib/money';
import type { StaticPageDto } from '@/lib/api-types';
import { toLocalized } from '@/lib/api-types';
import { toast } from 'sonner';

export default function AdminStaticPagesPage() {
  const { t, locale, localized } = useI18n();
  const [pages, setPages] = useState<StaticPageDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [editingId, setEditingId] = useState<string | null>(null);
  const [formData, setFormData] = useState({
    titleFr: '', titleEn: '', contentFr: '', contentEn: '',
  });

  useEffect(() => {
    const load = async () => {
      try {
        const data = await contentService.getPages();
        setPages(data);
      } catch (err) {
        console.error('Failed to load pages', err);
      } finally {
        setLoading(false);
      }
    };
    load();
  }, []);

  const startEdit = (page: StaticPageDto) => {
    setEditingId(page.id);
    setFormData({
      titleFr: page.titleFr, titleEn: page.titleEn,
      contentFr: page.contentFr, contentEn: page.contentEn,
    });
  };

  const handleSave = async (pageId: string) => {
    try {
      const updated = await contentService.updatePage(pageId, {
        titleFr: formData.titleFr, titleEn: formData.titleEn,
        contentFr: formData.contentFr, contentEn: formData.contentEn,
      });
      setPages(prev => prev.map(p => p.id === pageId ? updated : p));
      setEditingId(null);
      toast.success(t('admin.page_updated'));
    } catch (err) {
      console.error('Failed to save page', err);
      toast.error(t('admin.save_page_failed'));
    }
  };

  if (loading) {
    return (
      <div className="flex items-center justify-center h-64">
        <Loader2 className="w-8 h-8 animate-spin text-brand-primary" />
      </div>
    );
  }

  return (
    <div className="space-y-6">
      {pages.map(page => (
        <Card key={page.id}>
          <CardHeader className="pb-3">
            <div className="flex items-center justify-between">
              <div className="flex items-center gap-3">
                <CardTitle className="text-base">{localized(toLocalized(page.titleFr, page.titleEn))}</CardTitle>
                <Badge variant="outline" className="text-xs">/{page.slug}</Badge>
              </div>
              <div className="flex items-center gap-2">
                <span className="text-xs text-muted-foreground">
                  {t('admin.updated_short')}: {new Date(page.updatedAt).toLocaleDateString(toIntlLocale(locale))}
                </span>
                {editingId !== page.id ? (
                  <Button size="sm" variant="outline" onClick={() => startEdit(page)}>
                    <Pencil className="w-3.5 h-3.5 me-1" />{t('admin.edit')}
                  </Button>
                ) : (
                  <div className="flex gap-1">
                    <Button size="sm" variant="outline" onClick={() => setEditingId(null)}>{t('admin.cancel')}</Button>
                    <Button size="sm" className="bg-brand-primary hover:bg-brand-hover text-white" onClick={() => handleSave(page.id)}>
                      <Save className="w-3.5 h-3.5 me-1" />{t('admin.save')}
                    </Button>
                  </div>
                )}
              </div>
            </div>
          </CardHeader>
          <CardContent>
            {editingId === page.id ? (
              <Tabs defaultValue="fr">
                <TabsList>
                  <TabsTrigger value="fr">Français</TabsTrigger>
                  <TabsTrigger value="en">English</TabsTrigger>
                </TabsList>
                <TabsContent value="fr" className="space-y-3 mt-3">
                  <div>
                    <Label>{t('admin.title')}</Label>
                    <Input value={formData.titleFr} onChange={e => setFormData(d => ({ ...d, titleFr: e.target.value }))} />
                  </div>
                  <div>
                    <Label>{t('admin.content_markdown')}</Label>
                    <textarea
                      className="w-full border rounded-md p-3 text-sm font-mono h-64 resize-y"
                      value={formData.contentFr}
                      onChange={e => setFormData(d => ({ ...d, contentFr: e.target.value }))}
                    />
                  </div>
                </TabsContent>
                <TabsContent value="en" className="space-y-3 mt-3">
                  <div>
                    <Label>Title</Label>
                    <Input value={formData.titleEn} onChange={e => setFormData(d => ({ ...d, titleEn: e.target.value }))} />
                  </div>
                  <div>
                    <Label>Content (Markdown)</Label>
                    <textarea
                      className="w-full border rounded-md p-3 text-sm font-mono h-64 resize-y"
                      value={formData.contentEn}
                      onChange={e => setFormData(d => ({ ...d, contentEn: e.target.value }))}
                    />
                  </div>
                </TabsContent>
              </Tabs>
            ) : (
              <div className="text-sm text-muted-foreground">
                <p className="line-clamp-3">{localized(toLocalized(page.contentFr, page.contentEn)).split('\n').filter(l => l.trim() && !l.startsWith('#')).slice(0, 2).join(' ')}</p>
                <a href={`/${page.slug}`} target="_blank" rel="noopener" className="inline-flex items-center gap-1 text-brand-primary text-xs mt-2 hover:underline">
                  <Eye className="w-3 h-3" /> {t('admin.view_page')}
                </a>
              </div>
            )}
          </CardContent>
        </Card>
      ))}
    </div>
  );
}
