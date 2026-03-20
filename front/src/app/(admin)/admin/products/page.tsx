'use client';

import { useState, useMemo } from 'react';
import { useSearchParams } from 'next/navigation';
import { Plus, Pencil, Trash2, Download, Search, ChevronUp, ChevronDown } from 'lucide-react';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Badge } from '@/components/ui/badge';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';
import { Dialog, DialogContent, DialogHeader, DialogTitle, DialogFooter } from '@/components/ui/dialog';
import { Label } from '@/components/ui/label';
import { Separator } from '@/components/ui/separator';
import { useI18n } from '@/context/i18n-context';
import { products as initialProducts, categories } from '@/mock';
import type { Product } from '@/mock';
import { formatPrice } from '@/lib/money';
import { VAT_RATES } from '@/lib/constants';
import { toast } from 'sonner';

type SortField = 'name' | 'priceHT' | 'stockQty' | 'status' | 'updatedAt';

export default function AdminProductsPage() {
  const { t, locale, localized } = useI18n();
  const searchParams = useSearchParams();
  const fmt = (n: number) => formatPrice(n, locale === 'fr' ? 'fr-FR' : 'en-US');

  const [productsList, setProductsList] = useState<Product[]>(initialProducts);
  const [search, setSearch] = useState('');
  const [categoryFilter, setCategoryFilter] = useState('all');
  const [statusFilter, setStatusFilter] = useState('all');
  const [sortField, setSortField] = useState<SortField>('updatedAt');
  const [sortDir, setSortDir] = useState<'asc' | 'desc'>('desc');
  const [selected, setSelected] = useState<Set<string>>(new Set());
  const [editProduct, setEditProduct] = useState<Product | null>(null);
  const [showDialog, setShowDialog] = useState(searchParams.get('action') === 'new');
  const [page, setPage] = useState(1);
  const pageSize = 10;

  // Form state
  const [formData, setFormData] = useState({
    nameFr: '', nameEn: '', descFr: '', descEn: '', priceHT: '', vatRate: 'STANDARD' as keyof typeof VAT_RATES,
    stockQty: '', status: 'published' as 'published' | 'draft', categoryId: '',
    priorityRank: '0', isNew: false,
  });

  const filtered = useMemo(() => {
    let list = [...productsList];
    if (search) {
      const q = search.toLowerCase();
      list = list.filter(p => localized(p.name).toLowerCase().includes(q) || p.slug.includes(q));
    }
    if (categoryFilter !== 'all') list = list.filter(p => p.categories.includes(categoryFilter));
    if (statusFilter !== 'all') list = list.filter(p => p.status === statusFilter);

    list.sort((a, b) => {
      let cmp = 0;
      switch (sortField) {
        case 'name': cmp = localized(a.name).localeCompare(localized(b.name)); break;
        case 'priceHT': cmp = a.priceHT - b.priceHT; break;
        case 'stockQty': cmp = a.stockQty - b.stockQty; break;
        case 'status': cmp = a.status.localeCompare(b.status); break;
        case 'updatedAt': cmp = a.updatedAt.localeCompare(b.updatedAt); break;
      }
      return sortDir === 'asc' ? cmp : -cmp;
    });
    return list;
  }, [productsList, search, categoryFilter, statusFilter, sortField, sortDir, locale, localized]);

  const paged = filtered.slice((page - 1) * pageSize, page * pageSize);
  const totalPages = Math.ceil(filtered.length / pageSize);

  const toggleSort = (field: SortField) => {
    if (sortField === field) setSortDir(d => d === 'asc' ? 'desc' : 'asc');
    else { setSortField(field); setSortDir('asc'); }
  };

  const SortIcon = ({ field }: { field: SortField }) => {
    if (sortField !== field) return null;
    return sortDir === 'asc' ? <ChevronUp className="w-3 h-3" /> : <ChevronDown className="w-3 h-3" />;
  };

  const toggleSelect = (id: string) => {
    setSelected(prev => {
      const next = new Set(prev);
      if (next.has(id)) next.delete(id); else next.add(id);
      return next;
    });
  };

  const toggleSelectAll = () => {
    if (selected.size === paged.length) setSelected(new Set());
    else setSelected(new Set(paged.map(p => p.id)));
  };

  const handleBulkAction = (action: string) => {
    if (selected.size === 0) return;
    setProductsList(prev => prev.map(p => {
      if (!selected.has(p.id)) return p;
      switch (action) {
        case 'publish': return { ...p, status: 'published' as const };
        case 'unpublish': return { ...p, status: 'draft' as const };
        case 'delete': return p; // handled below
        default: return p;
      }
    }));
    if (action === 'delete') {
      setProductsList(prev => prev.filter(p => !selected.has(p.id)));
      toast.success(`${selected.size} ${locale === 'fr' ? 'produit(s) supprimé(s)' : 'product(s) deleted'}`);
    } else {
      toast.success(`${selected.size} ${locale === 'fr' ? 'produit(s) mis à jour' : 'product(s) updated'}`);
    }
    setSelected(new Set());
  };

  const openEditDialog = (product: Product) => {
    setEditProduct(product);
    setFormData({
      nameFr: product.name.fr, nameEn: product.name.en,
      descFr: product.description.fr, descEn: product.description.en,
      priceHT: String(product.priceHT), vatRate: product.vatRate,
      stockQty: String(product.stockQty), status: product.status,
      categoryId: product.categories[0] || '', priorityRank: String(product.priorityRank),
      isNew: product.isNew,
    });
    setShowDialog(true);
  };

  const openNewDialog = () => {
    setEditProduct(null);
    setFormData({
      nameFr: '', nameEn: '', descFr: '', descEn: '', priceHT: '', vatRate: 'STANDARD',
      stockQty: '', status: 'draft', categoryId: categories[0]?.id || '', priorityRank: '0', isNew: true,
    });
    setShowDialog(true);
  };

  const handleSave = () => {
    const now = new Date().toISOString().split('T')[0];
    const slug = formData.nameFr.toLowerCase().replace(/[^a-z0-9]+/g, '-').replace(/^-|-$/g, '');
    const qty = parseInt(formData.stockQty) || 0;
    const stockStatus = qty === 0 ? 'out_of_stock' as const : qty <= 5 ? 'low_stock' as const : 'in_stock' as const;

    if (editProduct) {
      setProductsList(prev => prev.map(p => p.id === editProduct.id ? {
        ...p,
        name: { fr: formData.nameFr, en: formData.nameEn },
        description: { fr: formData.descFr, en: formData.descEn },
        priceHT: parseFloat(formData.priceHT) || 0,
        vatRate: formData.vatRate,
        stockQty: qty, stockStatus,
        status: formData.status,
        categories: formData.categoryId ? [formData.categoryId] : p.categories,
        priorityRank: parseInt(formData.priorityRank) || 0,
        isNew: formData.isNew,
        updatedAt: now,
      } : p));
      toast.success(locale === 'fr' ? 'Produit mis à jour' : 'Product updated');
    } else {
      const newProduct: Product = {
        id: `prod-new-${Date.now()}`,
        slug,
        name: { fr: formData.nameFr, en: formData.nameEn },
        description: { fr: formData.descFr, en: formData.descEn },
        longDescription: { fr: formData.descFr, en: formData.descEn },
        priceHT: parseFloat(formData.priceHT) || 0,
        vatRate: formData.vatRate,
        stockQty: qty, stockStatus,
        isNew: formData.isNew,
        priorityRank: parseInt(formData.priorityRank) || 0,
        categories: formData.categoryId ? [formData.categoryId] : [],
        images: ['default'],
        specs: [],
        status: formData.status,
        createdAt: now,
        updatedAt: now,
      };
      setProductsList(prev => [newProduct, ...prev]);
      toast.success(locale === 'fr' ? 'Produit créé' : 'Product created');
    }
    setShowDialog(false);
  };

  const exportCSV = () => {
    const header = 'ID,Name,Price HT,Stock,Status,Category\n';
    const rows = filtered.map(p =>
      `${p.id},"${localized(p.name)}",${p.priceHT},${p.stockQty},${p.status},${p.categories.join(';')}`
    ).join('\n');
    const blob = new Blob([header + rows], { type: 'text/csv' });
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a'); a.href = url; a.download = 'products.csv'; a.click();
    URL.revokeObjectURL(url);
  };

  return (
    <div className="space-y-4">
      {/* Toolbar */}
      <div className="flex flex-wrap items-center gap-3">
        <div className="relative flex-1 min-w-[200px]">
          <Search className="absolute left-3 top-1/2 -translate-y-1/2 w-4 h-4 text-muted-foreground" />
          <Input value={search} onChange={e => setSearch(e.target.value)} placeholder={locale === 'fr' ? 'Rechercher un produit...' : 'Search products...'} className="pl-9" />
        </div>
        <Select value={categoryFilter} onValueChange={setCategoryFilter}>
          <SelectTrigger className="w-[180px]"><SelectValue /></SelectTrigger>
          <SelectContent>
            <SelectItem value="all">{locale === 'fr' ? 'Toutes catégories' : 'All categories'}</SelectItem>
            {categories.map(c => <SelectItem key={c.id} value={c.id}>{localized(c.name)}</SelectItem>)}
          </SelectContent>
        </Select>
        <Select value={statusFilter} onValueChange={setStatusFilter}>
          <SelectTrigger className="w-[130px]"><SelectValue /></SelectTrigger>
          <SelectContent>
            <SelectItem value="all">{locale === 'fr' ? 'Tous statuts' : 'All statuses'}</SelectItem>
            <SelectItem value="published">{locale === 'fr' ? 'Publié' : 'Published'}</SelectItem>
            <SelectItem value="draft">{locale === 'fr' ? 'Brouillon' : 'Draft'}</SelectItem>
          </SelectContent>
        </Select>
        <Button size="sm" variant="outline" onClick={exportCSV}><Download className="w-4 h-4 mr-1" />{t('admin.export_csv')}</Button>
        <Button size="sm" className="bg-brand-primary hover:bg-brand-hover text-white" onClick={openNewDialog}>
          <Plus className="w-4 h-4 mr-1" />{t('admin.add_product')}
        </Button>
      </div>

      {/* Bulk actions */}
      {selected.size > 0 && (
        <div className="flex items-center gap-2 bg-brand-light p-3 rounded-lg">
          <span className="text-sm font-medium">{selected.size} {locale === 'fr' ? 'sélectionné(s)' : 'selected'}</span>
          <Separator orientation="vertical" className="h-5" />
          <Button size="sm" variant="outline" onClick={() => handleBulkAction('publish')}>{t('admin.publish')}</Button>
          <Button size="sm" variant="outline" onClick={() => handleBulkAction('unpublish')}>{t('admin.unpublish')}</Button>
          <Button size="sm" variant="destructive" onClick={() => handleBulkAction('delete')}>{t('admin.delete')}</Button>
        </div>
      )}

      {/* Table */}
      <Card>
        <CardContent className="p-0">
          <div className="overflow-x-auto">
            <table className="w-full text-sm">
              <thead>
                <tr className="border-b bg-gray-50">
                  <th className="p-3 w-10">
                    <input type="checkbox" checked={paged.length > 0 && selected.size === paged.length} onChange={toggleSelectAll} className="rounded" />
                  </th>
                  <th className="p-3 text-left cursor-pointer select-none" onClick={() => toggleSort('name')}>
                    <span className="flex items-center gap-1">{locale === 'fr' ? 'Nom' : 'Name'} <SortIcon field="name" /></span>
                  </th>
                  <th className="p-3 text-right cursor-pointer select-none" onClick={() => toggleSort('priceHT')}>
                    <span className="flex items-center justify-end gap-1">{locale === 'fr' ? 'Prix HT' : 'Price excl.'} <SortIcon field="priceHT" /></span>
                  </th>
                  <th className="p-3 text-center cursor-pointer select-none" onClick={() => toggleSort('stockQty')}>
                    <span className="flex items-center justify-center gap-1">Stock <SortIcon field="stockQty" /></span>
                  </th>
                  <th className="p-3 text-center cursor-pointer select-none" onClick={() => toggleSort('status')}>
                    <span className="flex items-center justify-center gap-1">Status <SortIcon field="status" /></span>
                  </th>
                  <th className="p-3 text-center">{locale === 'fr' ? 'Catégorie' : 'Category'}</th>
                  <th className="p-3 text-right">Actions</th>
                </tr>
              </thead>
              <tbody>
                {paged.map(p => (
                  <tr key={p.id} className="border-b hover:bg-gray-50/50">
                    <td className="p-3"><input type="checkbox" checked={selected.has(p.id)} onChange={() => toggleSelect(p.id)} className="rounded" /></td>
                    <td className="p-3">
                      <div className="flex items-center gap-2">
                        <span className="font-medium text-brand-dark">{localized(p.name)}</span>
                        {p.isNew && <Badge className="bg-brand-primary text-white text-[10px]">NEW</Badge>}
                      </div>
                      <span className="text-xs text-muted-foreground">{p.slug}</span>
                    </td>
                    <td className="p-3 text-right font-medium">{fmt(p.priceHT)}</td>
                    <td className="p-3 text-center">
                      <Badge variant={p.stockStatus === 'in_stock' ? 'default' : p.stockStatus === 'low_stock' ? 'secondary' : 'destructive'}
                        className={p.stockStatus === 'in_stock' ? 'bg-success/10 text-success border-success/20' : p.stockStatus === 'low_stock' ? 'bg-warning/10 text-warning border-warning/20' : ''}>
                        {p.stockQty}
                      </Badge>
                    </td>
                    <td className="p-3 text-center">
                      <Badge variant="outline" className={p.status === 'published' ? 'text-success border-success' : 'text-muted-foreground'}>
                        {p.status === 'published' ? (locale === 'fr' ? 'Publié' : 'Published') : (locale === 'fr' ? 'Brouillon' : 'Draft')}
                      </Badge>
                    </td>
                    <td className="p-3 text-center text-xs text-muted-foreground">
                      {p.categories.map(cid => categories.find(c => c.id === cid)).filter(Boolean).map(c => localized(c!.name)).join(', ')}
                    </td>
                    <td className="p-3 text-right">
                      <div className="flex items-center justify-end gap-1">
                        <Button size="icon" variant="ghost" className="h-7 w-7" onClick={() => openEditDialog(p)}>
                          <Pencil className="w-3.5 h-3.5" />
                        </Button>
                        <Button size="icon" variant="ghost" className="h-7 w-7 text-destructive hover:text-destructive" onClick={() => {
                          setProductsList(prev => prev.filter(x => x.id !== p.id));
                          toast.success(locale === 'fr' ? 'Produit supprimé' : 'Product deleted');
                        }}>
                          <Trash2 className="w-3.5 h-3.5" />
                        </Button>
                      </div>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>

          {/* Pagination */}
          <div className="flex items-center justify-between p-3 border-t">
            <span className="text-xs text-muted-foreground">{filtered.length} {locale === 'fr' ? 'produits' : 'products'}</span>
            <div className="flex gap-1">
              {Array.from({ length: totalPages }, (_, i) => (
                <Button key={i} size="sm" variant={page === i + 1 ? 'default' : 'outline'} className={page === i + 1 ? 'bg-brand-primary text-white' : ''} onClick={() => setPage(i + 1)}>
                  {i + 1}
                </Button>
              ))}
            </div>
          </div>
        </CardContent>
      </Card>

      {/* Edit/Create Dialog */}
      <Dialog open={showDialog} onOpenChange={setShowDialog}>
        <DialogContent className="max-w-lg max-h-[90vh] overflow-y-auto">
          <DialogHeader>
            <DialogTitle>{editProduct ? (locale === 'fr' ? 'Modifier le produit' : 'Edit Product') : (locale === 'fr' ? 'Nouveau produit' : 'New Product')}</DialogTitle>
          </DialogHeader>
          <div className="space-y-4">
            <div className="grid grid-cols-2 gap-3">
              <div>
                <Label>{locale === 'fr' ? 'Nom (FR)' : 'Name (FR)'}</Label>
                <Input value={formData.nameFr} onChange={e => setFormData(d => ({ ...d, nameFr: e.target.value }))} />
              </div>
              <div>
                <Label>{locale === 'fr' ? 'Nom (EN)' : 'Name (EN)'}</Label>
                <Input value={formData.nameEn} onChange={e => setFormData(d => ({ ...d, nameEn: e.target.value }))} />
              </div>
            </div>
            <div className="grid grid-cols-2 gap-3">
              <div>
                <Label>Description (FR)</Label>
                <textarea className="w-full border rounded-md p-2 text-sm h-20 resize-none" value={formData.descFr} onChange={e => setFormData(d => ({ ...d, descFr: e.target.value }))} />
              </div>
              <div>
                <Label>Description (EN)</Label>
                <textarea className="w-full border rounded-md p-2 text-sm h-20 resize-none" value={formData.descEn} onChange={e => setFormData(d => ({ ...d, descEn: e.target.value }))} />
              </div>
            </div>
            <div className="grid grid-cols-3 gap-3">
              <div>
                <Label>{locale === 'fr' ? 'Prix HT (€)' : 'Price excl. (€)'}</Label>
                <Input type="number" step="0.01" value={formData.priceHT} onChange={e => setFormData(d => ({ ...d, priceHT: e.target.value }))} />
              </div>
              <div>
                <Label>TVA</Label>
                <Select value={formData.vatRate} onValueChange={v => setFormData(d => ({ ...d, vatRate: v as keyof typeof VAT_RATES }))}>
                  <SelectTrigger><SelectValue /></SelectTrigger>
                  <SelectContent>
                    <SelectItem value="STANDARD">20%</SelectItem>
                    <SelectItem value="INTERMEDIATE">10%</SelectItem>
                    <SelectItem value="REDUCED">5,5%</SelectItem>
                    <SelectItem value="ZERO">0%</SelectItem>
                  </SelectContent>
                </Select>
              </div>
              <div>
                <Label>Stock</Label>
                <Input type="number" value={formData.stockQty} onChange={e => setFormData(d => ({ ...d, stockQty: e.target.value }))} />
              </div>
            </div>
            <div className="grid grid-cols-2 gap-3">
              <div>
                <Label>{locale === 'fr' ? 'Catégorie' : 'Category'}</Label>
                <Select value={formData.categoryId} onValueChange={v => setFormData(d => ({ ...d, categoryId: v }))}>
                  <SelectTrigger><SelectValue /></SelectTrigger>
                  <SelectContent>
                    {categories.map(c => <SelectItem key={c.id} value={c.id}>{localized(c.name)}</SelectItem>)}
                  </SelectContent>
                </Select>
              </div>
              <div>
                <Label>Status</Label>
                <Select value={formData.status} onValueChange={v => setFormData(d => ({ ...d, status: v as 'published' | 'draft' }))}>
                  <SelectTrigger><SelectValue /></SelectTrigger>
                  <SelectContent>
                    <SelectItem value="published">{locale === 'fr' ? 'Publié' : 'Published'}</SelectItem>
                    <SelectItem value="draft">{locale === 'fr' ? 'Brouillon' : 'Draft'}</SelectItem>
                  </SelectContent>
                </Select>
              </div>
            </div>
            <div className="grid grid-cols-2 gap-3">
              <div>
                <Label>{locale === 'fr' ? 'Rang priorité' : 'Priority rank'}</Label>
                <Input type="number" value={formData.priorityRank} onChange={e => setFormData(d => ({ ...d, priorityRank: e.target.value }))} />
              </div>
              <div className="flex items-end pb-2">
                <label className="flex items-center gap-2 text-sm">
                  <input type="checkbox" checked={formData.isNew} onChange={e => setFormData(d => ({ ...d, isNew: e.target.checked }))} className="rounded" />
                  {locale === 'fr' ? 'Marquer comme nouveau' : 'Mark as new'}
                </label>
              </div>
            </div>
          </div>
          <DialogFooter>
            <Button variant="outline" onClick={() => setShowDialog(false)}>{locale === 'fr' ? 'Annuler' : 'Cancel'}</Button>
            <Button className="bg-brand-primary hover:bg-brand-hover text-white" onClick={handleSave}>
              {editProduct ? (locale === 'fr' ? 'Enregistrer' : 'Save') : (locale === 'fr' ? 'Créer' : 'Create')}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </div>
  );
}
