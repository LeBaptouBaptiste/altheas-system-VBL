'use client';

import { useState, useEffect, useMemo } from 'react';
import { useSearchParams } from 'next/navigation';
import { Plus, Pencil, Trash2, Download, Search, ChevronUp, ChevronDown, Loader2 } from 'lucide-react';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Badge } from '@/components/ui/badge';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';
import { Dialog, DialogContent, DialogHeader, DialogTitle, DialogFooter } from '@/components/ui/dialog';
import { Label } from '@/components/ui/label';
import { Separator } from '@/components/ui/separator';
import { useI18n } from '@/context/i18n-context';
import { productsService, categoriesService } from '@/lib/api-services';
import type { ProductDto, CategoryDto } from '@/lib/api-types';
import { toLocalized } from '@/lib/api-types';
import { formatPrice, toIntlLocale } from '@/lib/money';
import { ProductStatus, StockStatus, VatRate } from '@/lib/enums';
import { enumLabel } from '@/lib/enums';
import { toast } from 'sonner';

const VAT_RATE_VALUES: Record<number, number> = {
  [VatRate.Standard]: 0.20,
  [VatRate.Intermediate]: 0.10,
  [VatRate.Reduced]: 0.055,
  [VatRate.Zero]: 0,
};

type SortField = 'name' | 'priceHT' | 'stockQty' | 'status' | 'updatedAt';

export default function AdminProductsPage() {
  const { t, locale, localized } = useI18n();
  const searchParams = useSearchParams();
  const fmt = (n: number) => formatPrice(n, toIntlLocale(locale));

  const [productsList, setProductsList] = useState<ProductDto[]>([]);
  const [categories, setCategories] = useState<CategoryDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [search, setSearch] = useState('');
  const [categoryFilter, setCategoryFilter] = useState('all');
  const [statusFilter, setStatusFilter] = useState('all');
  const [sortField, setSortField] = useState<SortField>('updatedAt');
  const [sortDir, setSortDir] = useState<'asc' | 'desc'>('desc');
  const [selected, setSelected] = useState<Set<string>>(new Set());
  const [editProduct, setEditProduct] = useState<ProductDto | null>(null);
  const [showDialog, setShowDialog] = useState(searchParams.get('action') === 'new');
  const [page, setPage] = useState(1);
  const pageSize = 10;

  // Form state
  const [formData, setFormData] = useState({
    nameFr: '', nameEn: '', descFr: '', descEn: '', priceHT: '', vatRate: String(VatRate.Standard),
    stockQty: '', status: String(ProductStatus.Draft),
    categoryId: '',
    priorityRank: '0', isNew: false,
  });

  useEffect(() => {
    const load = async () => {
      try {
        const [prodsRes, cats] = await Promise.all([
          productsService.getAll(1, 200),
          categoriesService.getAll(),
        ]);
        setProductsList(prodsRes.data);
        setCategories(cats);
      } catch (err) {
        console.error('Failed to load products', err);
      } finally {
        setLoading(false);
      }
    };
    load();
  }, []);

  const filtered = useMemo(() => {
    let list = [...productsList];
    if (search) {
      const q = search.toLowerCase();
      list = list.filter(p => localized(toLocalized(p.nameFr, p.nameEn, p.nameMs, p.nameAr)).toLowerCase().includes(q) || p.slug.includes(q));
    }
    if (categoryFilter !== 'all') list = list.filter(p => p.categories.some(c => c.id === categoryFilter));
    if (statusFilter !== 'all') list = list.filter(p => p.status === Number(statusFilter));

    list.sort((a, b) => {
      let cmp = 0;
      switch (sortField) {
        case 'name': cmp = localized(toLocalized(a.nameFr, a.nameEn, a.nameMs, a.nameAr)).localeCompare(localized(toLocalized(b.nameFr, b.nameEn, b.nameMs, b.nameAr))); break;
        case 'priceHT': cmp = a.priceHT - b.priceHT; break;
        case 'stockQty': cmp = a.stockQty - b.stockQty; break;
        case 'status': cmp = a.status - b.status; break;
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

  const handleBulkAction = async (action: string) => {
    if (selected.size === 0) return;
    try {
      if (action === 'delete') {
        await Promise.all(Array.from(selected).map(id => productsService.delete(id)));
        setProductsList(prev => prev.filter(p => !selected.has(p.id)));
        toast.success(`${selected.size} ${t('admin.products_deleted_n')}`);
      } else {
        const newStatus = action === 'publish' ? ProductStatus.Active : ProductStatus.Draft;
        await Promise.all(Array.from(selected).map(id => productsService.update(id, { status: newStatus })));
        setProductsList(prev => prev.map(p => {
          if (!selected.has(p.id)) return p;
          return { ...p, status: newStatus as ProductDto['status'] };
        }));
        toast.success(`${selected.size} ${t('admin.products_updated_n')}`);
      }
    } catch (err) {
      console.error('Bulk action failed', err);
      toast.error(t('admin.action_failed'));
    }
    setSelected(new Set());
  };

  const openEditDialog = (product: ProductDto) => {
    setEditProduct(product);
    setFormData({
      nameFr: product.nameFr, nameEn: product.nameEn,
      descFr: product.descriptionFr, descEn: product.descriptionEn,
      priceHT: String(product.priceHT), vatRate: String(product.vatRate),
      stockQty: String(product.stockQty), status: product.status === ProductStatus.Active ? String(ProductStatus.Active) : String(ProductStatus.Draft),
      categoryId: product.categories[0]?.id || '', priorityRank: String(product.priorityRank),
      isNew: product.isNew,
    });
    setShowDialog(true);
  };

  const openNewDialog = () => {
    setEditProduct(null);
    setFormData({
      nameFr: '', nameEn: '', descFr: '', descEn: '', priceHT: '', vatRate: String(VatRate.Standard),
      stockQty: '', status: String(ProductStatus.Draft), categoryId: categories[0]?.id || '', priorityRank: '0', isNew: true,
    });
    setShowDialog(true);
  };

  const handleSave = async () => {
    const slug = formData.nameFr.toLowerCase().replace(/[^a-z0-9]+/g, '-').replace(/^-|-$/g, '');
    const qty = parseInt(formData.stockQty) || 0;
    const stockStatus = qty === 0 ? StockStatus.OutOfStock : qty <= 5 ? StockStatus.LowStock : StockStatus.InStock;

    const payload = {
      nameFr: formData.nameFr, nameEn: formData.nameEn,
      descriptionFr: formData.descFr, descriptionEn: formData.descEn,
      longDescriptionFr: formData.descFr, longDescriptionEn: formData.descEn,
      priceHT: parseFloat(formData.priceHT) || 0,
      vatRate: Number(formData.vatRate),
      stockQty: qty, stockStatus,
      status: Number(formData.status) === ProductStatus.Active ? ProductStatus.Active : ProductStatus.Draft,
      categoryIds: formData.categoryId ? [formData.categoryId] : [],
      priorityRank: parseInt(formData.priorityRank) || 0,
      isNew: formData.isNew,
      slug,
    };

    try {
      if (editProduct) {
        const updated = await productsService.update(editProduct.id, payload);
        setProductsList(prev => prev.map(p => p.id === editProduct.id ? updated : p));
        toast.success(t('admin.product_updated'));
      } else {
        const created = await productsService.create(payload);
        setProductsList(prev => [created, ...prev]);
        toast.success(t('admin.product_created'));
      }
      setShowDialog(false);
    } catch (err) {
      console.error('Failed to save product', err);
      toast.error(t('admin.save_product_failed'));
    }
  };

  const exportCSV = () => {
    const header = 'ID,Name,Price HT,Stock,Status,Category\n';
    const rows = filtered.map(p =>
      `${p.id},"${localized(toLocalized(p.nameFr, p.nameEn, p.nameMs, p.nameAr))}",${p.priceHT},${p.stockQty},${enumLabel('ProductStatus', p.status, locale)},${p.categories.map(c => c.id).join(';')}`
    ).join('\n');
    const blob = new Blob([header + rows], { type: 'text/csv' });
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a'); a.href = url; a.download = 'products.csv'; a.click();
    URL.revokeObjectURL(url);
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
      {/* Toolbar */}
      <div className="flex flex-wrap items-center gap-3">
        <div className="relative flex-1 min-w-[200px]">
          <Search className="absolute start-3 top-1/2 -translate-y-1/2 w-4 h-4 text-muted-foreground" />
          <Input value={search} onChange={e => setSearch(e.target.value)} placeholder={t('admin.search_products')} className="ps-9" />
        </div>
        <Select value={categoryFilter} onValueChange={setCategoryFilter}>
          <SelectTrigger className="w-[180px]"><SelectValue /></SelectTrigger>
          <SelectContent>
            <SelectItem value="all">{t('admin.all_categories')}</SelectItem>
            {categories.map(c => <SelectItem key={c.id} value={c.id}>{localized(toLocalized(c.nameFr, c.nameEn, c.nameMs, c.nameAr))}</SelectItem>)}
          </SelectContent>
        </Select>
        <Select value={statusFilter} onValueChange={setStatusFilter}>
          <SelectTrigger className="w-[130px]"><SelectValue /></SelectTrigger>
          <SelectContent>
            <SelectItem value="all">{t('admin.all_statuses')}</SelectItem>
            <SelectItem value={String(ProductStatus.Active)}>{t('admin.published')}</SelectItem>
            <SelectItem value={String(ProductStatus.Draft)}>{t('admin.draft')}</SelectItem>
          </SelectContent>
        </Select>
        <Button size="sm" variant="outline" onClick={exportCSV}><Download className="w-4 h-4 me-1" />{t('admin.export_csv')}</Button>
        <Button size="sm" className="bg-brand-primary hover:bg-brand-hover text-white" onClick={openNewDialog}>
          <Plus className="w-4 h-4 me-1" />{t('admin.add_product')}
        </Button>
      </div>

      {/* Bulk actions */}
      {selected.size > 0 && (
        <div className="flex items-center gap-2 bg-brand-light p-3 rounded-lg">
          <span className="text-sm font-medium">{selected.size} {t('admin.selected_n')}</span>
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
                  <th className="p-3 text-start cursor-pointer select-none" onClick={() => toggleSort('name')}>
                    <span className="flex items-center gap-1">{t('admin.name')} <SortIcon field="name" /></span>
                  </th>
                  <th className="p-3 text-end cursor-pointer select-none" onClick={() => toggleSort('priceHT')}>
                    <span className="flex items-center justify-end gap-1">{t('admin.price_excl')} <SortIcon field="priceHT" /></span>
                  </th>
                  <th className="p-3 text-center cursor-pointer select-none" onClick={() => toggleSort('stockQty')}>
                    <span className="flex items-center justify-center gap-1">Stock <SortIcon field="stockQty" /></span>
                  </th>
                  <th className="p-3 text-center cursor-pointer select-none" onClick={() => toggleSort('status')}>
                    <span className="flex items-center justify-center gap-1">Status <SortIcon field="status" /></span>
                  </th>
                  <th className="p-3 text-center">{t('admin.category_label')}</th>
                  <th className="p-3 text-end">Actions</th>
                </tr>
              </thead>
              <tbody>
                {paged.map(p => (
                  <tr key={p.id} className="border-b hover:bg-gray-50/50">
                    <td className="p-3"><input type="checkbox" checked={selected.has(p.id)} onChange={() => toggleSelect(p.id)} className="rounded" /></td>
                    <td className="p-3">
                      <div className="flex items-center gap-2">
                        <span className="font-medium text-brand-dark">{localized(toLocalized(p.nameFr, p.nameEn, p.nameMs, p.nameAr))}</span>
                        {p.isNew && <Badge className="bg-brand-primary text-white text-[10px]">NEW</Badge>}
                      </div>
                      <span className="text-xs text-muted-foreground">{p.slug}</span>
                    </td>
                    <td className="p-3 text-end font-medium">{fmt(p.priceHT)}</td>
                    <td className="p-3 text-center">
                      <Badge variant={p.stockStatus === StockStatus.InStock ? 'default' : p.stockStatus === StockStatus.LowStock ? 'secondary' : 'destructive'}
                        className={p.stockStatus === StockStatus.InStock ? 'bg-success/10 text-success border-success/20' : p.stockStatus === StockStatus.LowStock ? 'bg-warning/10 text-warning border-warning/20' : ''}>
                        {p.stockQty}
                      </Badge>
                    </td>
                    <td className="p-3 text-center">
                      <Badge variant="outline" className={p.status === ProductStatus.Active ? 'text-success border-success' : 'text-muted-foreground'}>
                        {enumLabel('ProductStatus', p.status, locale)}
                      </Badge>
                    </td>
                    <td className="p-3 text-center text-xs text-muted-foreground">
                      {p.categories.map(c => localized(toLocalized(c.nameFr, c.nameEn, c.nameMs, c.nameAr))).join(', ')}
                    </td>
                    <td className="p-3 text-end">
                      <div className="flex items-center justify-end gap-1">
                        <Button size="icon" variant="ghost" className="h-7 w-7" onClick={() => openEditDialog(p)}>
                          <Pencil className="w-3.5 h-3.5" />
                        </Button>
                        <Button size="icon" variant="ghost" className="h-7 w-7 text-destructive hover:text-destructive" onClick={async () => {
                          try {
                            await productsService.delete(p.id);
                            setProductsList(prev => prev.filter(x => x.id !== p.id));
                            toast.success(t('admin.product_deleted'));
                          } catch (err) {
                            console.error('Failed to delete product', err);
                            toast.error(t('admin.delete_product_failed'));
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
          </div>

          {/* Pagination */}
          <div className="flex items-center justify-between p-3 border-t">
            <span className="text-xs text-muted-foreground">{filtered.length} {t('admin.products_word')}</span>
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
            <DialogTitle>{editProduct ? t('admin.edit_product') : t('admin.new_product')}</DialogTitle>
          </DialogHeader>
          <div className="space-y-4">
            <div className="grid grid-cols-2 gap-3">
              <div>
                <Label>{t('admin.name_fr')}</Label>
                <Input value={formData.nameFr} onChange={e => setFormData(d => ({ ...d, nameFr: e.target.value }))} />
              </div>
              <div>
                <Label>{t('admin.name_en')}</Label>
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
                <Label>{t('admin.price_ht_label')}</Label>
                <Input type="number" step="0.01" value={formData.priceHT} onChange={e => setFormData(d => ({ ...d, priceHT: e.target.value }))} />
              </div>
              <div>
                <Label>TVA</Label>
                <Select value={formData.vatRate} onValueChange={v => setFormData(d => ({ ...d, vatRate: v }))}>
                  <SelectTrigger><SelectValue /></SelectTrigger>
                  <SelectContent>
                    <SelectItem value={String(VatRate.Standard)}>20%</SelectItem>
                    <SelectItem value={String(VatRate.Intermediate)}>10%</SelectItem>
                    <SelectItem value={String(VatRate.Reduced)}>5,5%</SelectItem>
                    <SelectItem value={String(VatRate.Zero)}>0%</SelectItem>
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
                <Label>{t('admin.category_label')}</Label>
                <Select value={formData.categoryId} onValueChange={v => setFormData(d => ({ ...d, categoryId: v }))}>
                  <SelectTrigger><SelectValue /></SelectTrigger>
                  <SelectContent>
                    {categories.map(c => <SelectItem key={c.id} value={c.id}>{localized(toLocalized(c.nameFr, c.nameEn, c.nameMs, c.nameAr))}</SelectItem>)}
                  </SelectContent>
                </Select>
              </div>
              <div>
                <Label>Status</Label>
                <Select value={formData.status} onValueChange={v => setFormData(d => ({ ...d, status: v }))}>
                  <SelectTrigger><SelectValue /></SelectTrigger>
                  <SelectContent>
                    <SelectItem value={String(ProductStatus.Active)}>{t('admin.published')}</SelectItem>
                    <SelectItem value={String(ProductStatus.Draft)}>{t('admin.draft')}</SelectItem>
                  </SelectContent>
                </Select>
              </div>
            </div>
            <div className="grid grid-cols-2 gap-3">
              <div>
                <Label>{t('admin.priority_rank')}</Label>
                <Input type="number" value={formData.priorityRank} onChange={e => setFormData(d => ({ ...d, priorityRank: e.target.value }))} />
              </div>
              <div className="flex items-end pb-2">
                <label className="flex items-center gap-2 text-sm">
                  <input type="checkbox" checked={formData.isNew} onChange={e => setFormData(d => ({ ...d, isNew: e.target.checked }))} className="rounded" />
                  {t('admin.mark_as_new')}
                </label>
              </div>
            </div>
          </div>
          <DialogFooter>
            <Button variant="outline" onClick={() => setShowDialog(false)}>{t('admin.cancel')}</Button>
            <Button className="bg-brand-primary hover:bg-brand-hover text-white" onClick={handleSave}>
              {editProduct ? t('admin.save') : t('admin.create')}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </div>
  );
}
