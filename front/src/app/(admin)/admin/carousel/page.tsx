'use client';

import { useState, useEffect } from 'react';
import { Pencil, GripVertical, Plus, Trash2, ExternalLink, Loader2 } from 'lucide-react';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Dialog, DialogContent, DialogHeader, DialogTitle, DialogFooter } from '@/components/ui/dialog';
import { useI18n } from '@/context/i18n-context';
import { contentService } from '@/lib/api-services';
import type { HeroSlideDto } from '@/lib/api-types';
import { toLocalized } from '@/lib/api-types';
import { toast } from 'sonner';

export default function AdminCarouselPage() {
  const { locale, localized } = useI18n();
  const [slides, setSlides] = useState<HeroSlideDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [editSlide, setEditSlide] = useState<HeroSlideDto | null>(null);
  const [showDialog, setShowDialog] = useState(false);
  const [formData, setFormData] = useState({
    titleFr: '', titleEn: '', subtitleFr: '', subtitleEn: '',
    descFr: '', descEn: '', ctaFr: '', ctaEn: '', link: '', image: '',
  });

  useEffect(() => {
    const load = async () => {
      try {
        const data = await contentService.getSlides(false);
        setSlides(data.sort((a, b) => a.displayOrder - b.displayOrder));
      } catch (err) {
        console.error('Failed to load slides', err);
      } finally {
        setLoading(false);
      }
    };
    load();
  }, []);

  const openNew = () => {
    setEditSlide(null);
    setFormData({ titleFr: '', titleEn: '', subtitleFr: '', subtitleEn: '', descFr: '', descEn: '', ctaFr: '', ctaEn: '', link: '', image: '' });
    setShowDialog(true);
  };

  const openEdit = (slide: HeroSlideDto) => {
    setEditSlide(slide);
    setFormData({
      titleFr: slide.titleFr, titleEn: slide.titleEn,
      subtitleFr: slide.subtitleFr, subtitleEn: slide.subtitleEn,
      descFr: slide.descriptionFr, descEn: slide.descriptionEn,
      ctaFr: slide.ctaFr, ctaEn: slide.ctaEn, link: slide.link, image: slide.image,
    });
    setShowDialog(true);
  };

  const handleSave = async () => {
    const payload = {
      titleFr: formData.titleFr, titleEn: formData.titleEn,
      subtitleFr: formData.subtitleFr, subtitleEn: formData.subtitleEn,
      descriptionFr: formData.descFr, descriptionEn: formData.descEn,
      ctaFr: formData.ctaFr, ctaEn: formData.ctaEn,
      link: formData.link, image: formData.image || 'slide1',
      active: true,
    };

    try {
      if (editSlide) {
        const updated = await contentService.updateSlide(editSlide.id, payload);
        setSlides(prev => prev.map(s => s.id === editSlide.id ? updated : s));
        toast.success(locale === 'fr' ? 'Slide mise à jour' : 'Slide updated');
      } else {
        const created = await contentService.createSlide({ ...payload, displayOrder: slides.length + 1 });
        setSlides(prev => [...prev, created]);
        toast.success(locale === 'fr' ? 'Slide créée' : 'Slide created');
      }
      setShowDialog(false);
    } catch (err) {
      console.error('Failed to save slide', err);
      toast.error(locale === 'fr' ? 'Erreur lors de la sauvegarde' : 'Failed to save slide');
    }
  };

  const moveUp = (i: number) => {
    if (i === 0) return;
    setSlides(prev => { const n = [...prev]; [n[i - 1], n[i]] = [n[i], n[i - 1]]; return n; });
  };
  const moveDown = (i: number) => {
    if (i === slides.length - 1) return;
    setSlides(prev => { const n = [...prev]; [n[i], n[i + 1]] = [n[i + 1], n[i]]; return n; });
  };

  if (loading) {
    return (
      <div className="flex items-center justify-center h-64">
        <Loader2 className="w-8 h-8 animate-spin text-brand-primary" />
      </div>
    );
  }

  return (
    <div className="space-y-4">
      <div className="flex items-center justify-between">
        <p className="text-sm text-muted-foreground">{slides.length} slides</p>
        <Button size="sm" className="bg-brand-primary hover:bg-brand-hover text-white" onClick={openNew}>
          <Plus className="w-4 h-4 me-1" />{locale === 'fr' ? 'Nouvelle slide' : 'New slide'}
        </Button>
      </div>

      <div className="space-y-3">
        {slides.map((slide, idx) => (
          <Card key={slide.id}>
            <CardContent className="p-4">
              <div className="flex items-start gap-4">
                <div className="flex flex-col items-center gap-1">
                  <Button size="icon" variant="ghost" className="h-6 w-6" onClick={() => moveUp(idx)} disabled={idx === 0}>
                    <GripVertical className="w-3 h-3 rotate-90" />
                  </Button>
                  <span className="text-xs text-muted-foreground font-medium">{idx + 1}</span>
                  <Button size="icon" variant="ghost" className="h-6 w-6" onClick={() => moveDown(idx)} disabled={idx === slides.length - 1}>
                    <GripVertical className="w-3 h-3 -rotate-90" />
                  </Button>
                </div>
                <div className="flex-1 min-w-0">
                  <h3 className="font-semibold text-brand-dark">{localized(toLocalized(slide.titleFr, slide.titleEn))}</h3>
                  <p className="text-sm text-brand-primary">{localized(toLocalized(slide.subtitleFr, slide.subtitleEn))}</p>
                  <p className="text-xs text-muted-foreground mt-1">{localized(toLocalized(slide.descriptionFr, slide.descriptionEn))}</p>
                  <div className="flex items-center gap-2 mt-2 text-xs text-muted-foreground">
                    <ExternalLink className="w-3 h-3" /> {slide.link}
                    <span className="mx-1">|</span>
                    CTA: {localized(toLocalized(slide.ctaFr, slide.ctaEn))}
                  </div>
                </div>
                <div className="flex items-center gap-1">
                  <Button size="icon" variant="ghost" className="h-8 w-8" onClick={() => openEdit(slide)}>
                    <Pencil className="w-4 h-4" />
                  </Button>
                  <Button size="icon" variant="ghost" className="h-8 w-8 text-destructive" onClick={async () => {
                    try {
                      await contentService.deleteSlide(slide.id);
                      setSlides(prev => prev.filter(s => s.id !== slide.id));
                      toast.success(locale === 'fr' ? 'Slide supprimée' : 'Slide deleted');
                    } catch (err) {
                      console.error('Failed to delete slide', err);
                      toast.error(locale === 'fr' ? 'Erreur' : 'Error');
                    }
                  }}>
                    <Trash2 className="w-4 h-4" />
                  </Button>
                </div>
              </div>
            </CardContent>
          </Card>
        ))}
      </div>

      <Dialog open={showDialog} onOpenChange={setShowDialog}>
        <DialogContent className="max-w-lg max-h-[90vh] overflow-y-auto">
          <DialogHeader>
            <DialogTitle>{editSlide ? (locale === 'fr' ? 'Modifier la slide' : 'Edit Slide') : (locale === 'fr' ? 'Nouvelle slide' : 'New Slide')}</DialogTitle>
          </DialogHeader>
          <div className="space-y-3">
            <div className="grid grid-cols-2 gap-3">
              <div><Label>{locale === 'fr' ? 'Titre (FR)' : 'Title (FR)'}</Label><Input value={formData.titleFr} onChange={e => setFormData(d => ({ ...d, titleFr: e.target.value }))} /></div>
              <div><Label>{locale === 'fr' ? 'Titre (EN)' : 'Title (EN)'}</Label><Input value={formData.titleEn} onChange={e => setFormData(d => ({ ...d, titleEn: e.target.value }))} /></div>
            </div>
            <div className="grid grid-cols-2 gap-3">
              <div><Label>{locale === 'fr' ? 'Sous-titre (FR)' : 'Subtitle (FR)'}</Label><Input value={formData.subtitleFr} onChange={e => setFormData(d => ({ ...d, subtitleFr: e.target.value }))} /></div>
              <div><Label>{locale === 'fr' ? 'Sous-titre (EN)' : 'Subtitle (EN)'}</Label><Input value={formData.subtitleEn} onChange={e => setFormData(d => ({ ...d, subtitleEn: e.target.value }))} /></div>
            </div>
            <div className="grid grid-cols-2 gap-3">
              <div><Label>Description (FR)</Label><textarea className="w-full border rounded-md p-2 text-sm h-16 resize-none" value={formData.descFr} onChange={e => setFormData(d => ({ ...d, descFr: e.target.value }))} /></div>
              <div><Label>Description (EN)</Label><textarea className="w-full border rounded-md p-2 text-sm h-16 resize-none" value={formData.descEn} onChange={e => setFormData(d => ({ ...d, descEn: e.target.value }))} /></div>
            </div>
            <div className="grid grid-cols-2 gap-3">
              <div><Label>CTA (FR)</Label><Input value={formData.ctaFr} onChange={e => setFormData(d => ({ ...d, ctaFr: e.target.value }))} /></div>
              <div><Label>CTA (EN)</Label><Input value={formData.ctaEn} onChange={e => setFormData(d => ({ ...d, ctaEn: e.target.value }))} /></div>
            </div>
            <div><Label>{locale === 'fr' ? 'Lien' : 'Link'}</Label><Input value={formData.link} onChange={e => setFormData(d => ({ ...d, link: e.target.value }))} placeholder="/product/..." /></div>
            <div><Label>{locale === 'fr' ? 'Image (clé)' : 'Image (key)'}</Label><Input value={formData.image} onChange={e => setFormData(d => ({ ...d, image: e.target.value }))} placeholder="slide1, slide2, slide3" /></div>
          </div>
          <DialogFooter>
            <Button variant="outline" onClick={() => setShowDialog(false)}>{locale === 'fr' ? 'Annuler' : 'Cancel'}</Button>
            <Button className="bg-brand-primary hover:bg-brand-hover text-white" onClick={handleSave}>
              {editSlide ? (locale === 'fr' ? 'Enregistrer' : 'Save') : (locale === 'fr' ? 'Créer' : 'Create')}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </div>
  );
}
