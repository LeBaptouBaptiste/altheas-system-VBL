'use client';

import { useState, useEffect } from 'react';
import { Plus, Pencil, Trash2, GripVertical, Eye, EyeOff, Loader2 } from 'lucide-react';
import { Card, CardContent } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Badge } from '@/components/ui/badge';
import { Dialog, DialogContent, DialogHeader, DialogTitle, DialogFooter } from '@/components/ui/dialog';
import { Label } from '@/components/ui/label';
import { useI18n } from '@/context/i18n-context';
import { categoriesService } from '@/lib/api-services';
import type { CategoryDto } from '@/lib/api-types';
import { toLocalized } from '@/lib/api-types';
import { toast } from 'sonner';

export default function AdminCategoriesPage() {
  const { locale, localized } = useI18n();
  const [catList, setCatList] = useState<CategoryDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [editCat, setEditCat] = useState<CategoryDto | null>(null);
  const [showDialog, setShowDialog] = useState(false);
  const [formData, setFormData] = useState({ nameFr: '', nameEn: '', descFr: '', descEn: '', slug: '', active: true });

  useEffect(() => {
    const load = async () => {
      try {
        const cats = await categoriesService.getAll();
        setCatList([...cats].sort((a, b) => a.displayOrder - b.displayOrder));
      } catch (err) {
        console.error('Failed to load categories', err);
      } finally {
        setLoading(false);
      }
    };
    load();
  }, []);

  const openNew = () => {
    setEditCat(null);
    setFormData({ nameFr: '', nameEn: '', descFr: '', descEn: '', slug: '', active: true });
    setShowDialog(true);
  };

  const openEdit = (cat: CategoryDto) => {
    setEditCat(cat);
    setFormData({
      nameFr: cat.nameFr, nameEn: cat.nameEn,
      descFr: cat.descriptionFr, descEn: cat.descriptionEn,
      slug: cat.slug, active: cat.active,
    });
    setShowDialog(true);
  };

  const handleSave = async () => {
    const slug = formData.slug || formData.nameFr.toLowerCase().replace(/[^a-z0-9]+/g, '-');
    const payload = {
      nameFr: formData.nameFr, nameEn: formData.nameEn,
      descriptionFr: formData.descFr, descriptionEn: formData.descEn,
      slug, active: formData.active,
    };

    try {
      if (editCat) {
        const updated = await categoriesService.update(editCat.id, payload);
        setCatList(prev => prev.map(c => c.id === editCat.id ? updated : c));
        toast.success(locale === 'fr' ? 'Catégorie mise à jour' : 'Category updated');
      } else {
        const created = await categoriesService.create({ ...payload, displayOrder: catList.length + 1 });
        setCatList(prev => [...prev, created]);
        toast.success(locale === 'fr' ? 'Catégorie créée' : 'Category created');
      }
      setShowDialog(false);
    } catch (err) {
      console.error('Failed to save category', err);
      toast.error(locale === 'fr' ? 'Erreur lors de la sauvegarde' : 'Failed to save category');
    }
  };

  const moveUp = (index: number) => {
    if (index === 0) return;
    setCatList(prev => {
      const next = [...prev];
      [next[index - 1], next[index]] = [next[index], next[index - 1]];
      return next.map((c, i) => ({ ...c, displayOrder: i + 1 }));
    });
  };

  const moveDown = (index: number) => {
    if (index === catList.length - 1) return;
    setCatList(prev => {
      const next = [...prev];
      [next[index], next[index + 1]] = [next[index + 1], next[index]];
      return next.map((c, i) => ({ ...c, displayOrder: i + 1 }));
    });
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
        <p className="text-sm text-muted-foreground">{catList.length} {locale === 'fr' ? 'catégories' : 'categories'}</p>
        <Button size="sm" className="bg-brand-primary hover:bg-brand-hover text-white" onClick={openNew}>
          <Plus className="w-4 h-4 mr-1" />{locale === 'fr' ? 'Nouvelle catégorie' : 'New category'}
        </Button>
      </div>

      <Card>
        <CardContent className="p-0">
          <table className="w-full text-sm">
            <thead>
              <tr className="border-b bg-gray-50">
                <th className="p-3 w-10">#</th>
                <th className="p-3 text-left">{locale === 'fr' ? 'Nom' : 'Name'}</th>
                <th className="p-3 text-center">{locale === 'fr' ? 'Produits' : 'Products'}</th>
                <th className="p-3 text-center">Status</th>
                <th className="p-3 text-center">{locale === 'fr' ? 'Ordre' : 'Order'}</th>
                <th className="p-3 text-right">Actions</th>
              </tr>
            </thead>
            <tbody>
              {catList.map((cat, idx) => (
                <tr key={cat.id} className="border-b hover:bg-gray-50/50">
                  <td className="p-3 text-center text-muted-foreground">{idx + 1}</td>
                  <td className="p-3">
                    <span className="font-medium text-brand-dark">{localized(toLocalized(cat.nameFr, cat.nameEn))}</span>
                    <span className="block text-xs text-muted-foreground">{cat.slug}</span>
                  </td>
                  <td className="p-3 text-center">
                    <Badge variant="secondary">{cat.productCount}</Badge>
                  </td>
                  <td className="p-3 text-center">
                    <Badge variant="outline" className={cat.active ? 'text-success border-success' : 'text-muted-foreground'}>
                      {cat.active ? (locale === 'fr' ? 'Active' : 'Active') : (locale === 'fr' ? 'Inactive' : 'Inactive')}
                    </Badge>
                  </td>
                  <td className="p-3">
                    <div className="flex items-center justify-center gap-1">
                      <Button size="icon" variant="ghost" className="h-6 w-6" onClick={() => moveUp(idx)} disabled={idx === 0}>
                        <GripVertical className="w-3 h-3 rotate-90" />
                      </Button>
                      <span className="text-xs text-muted-foreground w-4 text-center">{idx + 1}</span>
                      <Button size="icon" variant="ghost" className="h-6 w-6" onClick={() => moveDown(idx)} disabled={idx === catList.length - 1}>
                        <GripVertical className="w-3 h-3 -rotate-90" />
                      </Button>
                    </div>
                  </td>
                  <td className="p-3 text-right">
                    <div className="flex items-center justify-end gap-1">
                      <Button size="icon" variant="ghost" className="h-7 w-7" onClick={async () => {
                        try {
                          const updated = await categoriesService.update(cat.id, { active: !cat.active });
                          setCatList(prev => prev.map(c => c.id === cat.id ? updated : c));
                        } catch (err) {
                          console.error('Failed to toggle category', err);
                          toast.error(locale === 'fr' ? 'Erreur' : 'Error');
                        }
                      }}>
                        {cat.active ? <EyeOff className="w-3.5 h-3.5" /> : <Eye className="w-3.5 h-3.5" />}
                      </Button>
                      <Button size="icon" variant="ghost" className="h-7 w-7" onClick={() => openEdit(cat)}>
                        <Pencil className="w-3.5 h-3.5" />
                      </Button>
                      <Button size="icon" variant="ghost" className="h-7 w-7 text-destructive" onClick={async () => {
                        try {
                          await categoriesService.delete(cat.id);
                          setCatList(prev => prev.filter(c => c.id !== cat.id));
                          toast.success(locale === 'fr' ? 'Catégorie supprimée' : 'Category deleted');
                        } catch (err) {
                          console.error('Failed to delete category', err);
                          toast.error(locale === 'fr' ? 'Erreur lors de la suppression' : 'Failed to delete category');
                        }
                      }}>
                        <Trash2 className="w-3.5 h-3.5" />
                      </Button>
                    </div>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </CardContent>
      </Card>

      <Dialog open={showDialog} onOpenChange={setShowDialog}>
        <DialogContent className="max-w-md">
          <DialogHeader>
            <DialogTitle>{editCat ? (locale === 'fr' ? 'Modifier la catégorie' : 'Edit Category') : (locale === 'fr' ? 'Nouvelle catégorie' : 'New Category')}</DialogTitle>
          </DialogHeader>
          <div className="space-y-3">
            <div className="grid grid-cols-2 gap-3">
              <div><Label>{locale === 'fr' ? 'Nom (FR)' : 'Name (FR)'}</Label><Input value={formData.nameFr} onChange={e => setFormData(d => ({ ...d, nameFr: e.target.value }))} /></div>
              <div><Label>{locale === 'fr' ? 'Nom (EN)' : 'Name (EN)'}</Label><Input value={formData.nameEn} onChange={e => setFormData(d => ({ ...d, nameEn: e.target.value }))} /></div>
            </div>
            <div><Label>Slug</Label><Input value={formData.slug} onChange={e => setFormData(d => ({ ...d, slug: e.target.value }))} placeholder="auto-generated" /></div>
            <div className="grid grid-cols-2 gap-3">
              <div><Label>Description (FR)</Label><textarea className="w-full border rounded-md p-2 text-sm h-16 resize-none" value={formData.descFr} onChange={e => setFormData(d => ({ ...d, descFr: e.target.value }))} /></div>
              <div><Label>Description (EN)</Label><textarea className="w-full border rounded-md p-2 text-sm h-16 resize-none" value={formData.descEn} onChange={e => setFormData(d => ({ ...d, descEn: e.target.value }))} /></div>
            </div>
            <label className="flex items-center gap-2 text-sm">
              <input type="checkbox" checked={formData.active} onChange={e => setFormData(d => ({ ...d, active: e.target.checked }))} className="rounded" />
              {locale === 'fr' ? 'Catégorie active' : 'Active category'}
            </label>
          </div>
          <DialogFooter>
            <Button variant="outline" onClick={() => setShowDialog(false)}>{locale === 'fr' ? 'Annuler' : 'Cancel'}</Button>
            <Button className="bg-brand-primary hover:bg-brand-hover text-white" onClick={handleSave}>
              {editCat ? (locale === 'fr' ? 'Enregistrer' : 'Save') : (locale === 'fr' ? 'Créer' : 'Create')}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </div>
  );
}
