'use client';

import { useState } from 'react';
import { Pencil, GripVertical, Plus, Trash2, ExternalLink } from 'lucide-react';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Dialog, DialogContent, DialogHeader, DialogTitle, DialogFooter } from '@/components/ui/dialog';
import { useI18n } from '@/context/i18n-context';
import { heroSlides as initialSlides } from '@/mock';
import type { HeroSlide } from '@/mock';
import { toast } from 'sonner';

export default function AdminCarouselPage() {
  const { locale, localized } = useI18n();
  const [slides, setSlides] = useState<HeroSlide[]>(initialSlides);
  const [editSlide, setEditSlide] = useState<HeroSlide | null>(null);
  const [showDialog, setShowDialog] = useState(false);
  const [formData, setFormData] = useState({
    titleFr: '', titleEn: '', subtitleFr: '', subtitleEn: '',
    descFr: '', descEn: '', ctaFr: '', ctaEn: '', link: '', image: '',
  });

  const openNew = () => {
    setEditSlide(null);
    setFormData({ titleFr: '', titleEn: '', subtitleFr: '', subtitleEn: '', descFr: '', descEn: '', ctaFr: '', ctaEn: '', link: '', image: '' });
    setShowDialog(true);
  };

  const openEdit = (slide: HeroSlide) => {
    setEditSlide(slide);
    setFormData({
      titleFr: slide.title.fr, titleEn: slide.title.en,
      subtitleFr: slide.subtitle.fr, subtitleEn: slide.subtitle.en,
      descFr: slide.description.fr, descEn: slide.description.en,
      ctaFr: slide.cta.fr, ctaEn: slide.cta.en, link: slide.link, image: slide.image,
    });
    setShowDialog(true);
  };

  const handleSave = () => {
    if (editSlide) {
      setSlides(prev => prev.map(s => s.id === editSlide.id ? {
        ...s,
        title: { fr: formData.titleFr, en: formData.titleEn },
        subtitle: { fr: formData.subtitleFr, en: formData.subtitleEn },
        description: { fr: formData.descFr, en: formData.descEn },
        cta: { fr: formData.ctaFr, en: formData.ctaEn },
        link: formData.link, image: formData.image,
      } : s));
      toast.success(locale === 'fr' ? 'Slide mise à jour' : 'Slide updated');
    } else {
      const newSlide: HeroSlide = {
        id: `slide-new-${Date.now()}`, image: formData.image || 'slide1',
        title: { fr: formData.titleFr, en: formData.titleEn },
        subtitle: { fr: formData.subtitleFr, en: formData.subtitleEn },
        description: { fr: formData.descFr, en: formData.descEn },
        cta: { fr: formData.ctaFr, en: formData.ctaEn },
        link: formData.link,
      };
      setSlides(prev => [...prev, newSlide]);
      toast.success(locale === 'fr' ? 'Slide créée' : 'Slide created');
    }
    setShowDialog(false);
  };

  const moveUp = (i: number) => {
    if (i === 0) return;
    setSlides(prev => { const n = [...prev]; [n[i - 1], n[i]] = [n[i], n[i - 1]]; return n; });
  };
  const moveDown = (i: number) => {
    if (i === slides.length - 1) return;
    setSlides(prev => { const n = [...prev]; [n[i], n[i + 1]] = [n[i + 1], n[i]]; return n; });
  };

  return (
    <div className="space-y-4">
      <div className="flex items-center justify-between">
        <p className="text-sm text-muted-foreground">{slides.length} slides</p>
        <Button size="sm" className="bg-brand-primary hover:bg-brand-hover text-white" onClick={openNew}>
          <Plus className="w-4 h-4 mr-1" />{locale === 'fr' ? 'Nouvelle slide' : 'New slide'}
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
                  <h3 className="font-semibold text-brand-dark">{localized(slide.title)}</h3>
                  <p className="text-sm text-brand-primary">{localized(slide.subtitle)}</p>
                  <p className="text-xs text-muted-foreground mt-1">{localized(slide.description)}</p>
                  <div className="flex items-center gap-2 mt-2 text-xs text-muted-foreground">
                    <ExternalLink className="w-3 h-3" /> {slide.link}
                    <span className="mx-1">|</span>
                    CTA: {localized(slide.cta)}
                  </div>
                </div>
                <div className="flex items-center gap-1">
                  <Button size="icon" variant="ghost" className="h-8 w-8" onClick={() => openEdit(slide)}>
                    <Pencil className="w-4 h-4" />
                  </Button>
                  <Button size="icon" variant="ghost" className="h-8 w-8 text-destructive" onClick={() => {
                    setSlides(prev => prev.filter(s => s.id !== slide.id));
                    toast.success(locale === 'fr' ? 'Slide supprimée' : 'Slide deleted');
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
