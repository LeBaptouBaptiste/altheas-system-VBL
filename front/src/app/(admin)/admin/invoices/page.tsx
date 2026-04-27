'use client';

import { useState, useMemo } from 'react';
import { Search, Download, FileText, RotateCcw } from 'lucide-react';
import { Card, CardContent } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Badge } from '@/components/ui/badge';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';
import { useI18n } from '@/context/i18n-context';
import { invoices as initialInvoices, orders } from '@/mock';
import type { Invoice } from '@/mock';
import { formatPrice, toIntlLocale } from '@/lib/money';
import { toast } from 'sonner';

const STATUS_COLORS: Record<string, string> = {
  paid: 'bg-success/10 text-success border-success/20',
  pending: 'bg-warning/10 text-warning border-warning/20',
  overdue: 'bg-error/10 text-error border-error/20',
  cancelled: 'bg-gray-100 text-muted-foreground',
};

export default function AdminInvoicesPage() {
  const { locale } = useI18n();
  const fmt = (n: number) => formatPrice(n, toIntlLocale(locale));

  const [invoicesList] = useState<Invoice[]>(initialInvoices);
  const [search, setSearch] = useState('');
  const [typeFilter, setTypeFilter] = useState('all');
  const [statusFilter, setStatusFilter] = useState('all');

  const filtered = useMemo(() => {
    let list = [...invoicesList].sort((a, b) => b.date.localeCompare(a.date));
    if (search) {
      const q = search.toLowerCase();
      list = list.filter(i => i.id.toLowerCase().includes(q) || i.orderId.toLowerCase().includes(q));
    }
    if (typeFilter !== 'all') list = list.filter(i => i.type === typeFilter);
    if (statusFilter !== 'all') list = list.filter(i => i.status === statusFilter);
    return list;
  }, [invoicesList, search, typeFilter, statusFilter]);

  const handleDownload = (inv: Invoice) => {
    const content = `${inv.type === 'credit_note' ? (locale === 'fr' ? 'AVOIR' : 'CREDIT NOTE') : (locale === 'fr' ? 'FACTURE' : 'INVOICE')}\n${inv.id}\nDate: ${inv.date}\nOrder: ${inv.orderId}\nHT: ${fmt(inv.amountHT)}\nTVA: ${fmt(inv.vatAmount)}\nTTC: ${fmt(inv.amountTTC)}`;
    const blob = new Blob([content], { type: 'text/plain' });
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url; a.download = `${inv.id}.txt`; a.click();
    URL.revokeObjectURL(url);
    toast.success(locale === 'fr' ? 'Téléchargement...' : 'Downloading...');
  };

  const totals = filtered.reduce((acc, inv) => {
    if (inv.type === 'credit_note') acc.credits += inv.amountTTC;
    else acc.invoices += inv.amountTTC;
    return acc;
  }, { invoices: 0, credits: 0 });

  return (
    <div className="space-y-4">
      {/* Summary */}
      <div className="grid grid-cols-3 gap-4">
        <Card>
          <CardContent className="p-4">
            <p className="text-xs text-muted-foreground">{locale === 'fr' ? 'Total factures' : 'Total invoices'}</p>
            <p className="text-xl font-bold text-brand-dark">{fmt(totals.invoices)}</p>
          </CardContent>
        </Card>
        <Card>
          <CardContent className="p-4">
            <p className="text-xs text-muted-foreground">{locale === 'fr' ? 'Total avoirs' : 'Total credits'}</p>
            <p className="text-xl font-bold text-error">{fmt(totals.credits)}</p>
          </CardContent>
        </Card>
        <Card>
          <CardContent className="p-4">
            <p className="text-xs text-muted-foreground">Net</p>
            <p className="text-xl font-bold text-success">{fmt(totals.invoices - totals.credits)}</p>
          </CardContent>
        </Card>
      </div>

      {/* Filters */}
      <div className="flex flex-wrap items-center gap-3">
        <div className="relative flex-1 min-w-[200px]">
          <Search className="absolute left-3 top-1/2 -translate-y-1/2 w-4 h-4 text-muted-foreground" />
          <Input value={search} onChange={e => setSearch(e.target.value)} placeholder={locale === 'fr' ? 'Rechercher...' : 'Search...'} className="pl-9" />
        </div>
        <Select value={typeFilter} onValueChange={setTypeFilter}>
          <SelectTrigger className="w-[140px]"><SelectValue /></SelectTrigger>
          <SelectContent>
            <SelectItem value="all">{locale === 'fr' ? 'Tous types' : 'All types'}</SelectItem>
            <SelectItem value="invoice">{locale === 'fr' ? 'Factures' : 'Invoices'}</SelectItem>
            <SelectItem value="credit_note">{locale === 'fr' ? 'Avoirs' : 'Credit notes'}</SelectItem>
          </SelectContent>
        </Select>
        <Select value={statusFilter} onValueChange={setStatusFilter}>
          <SelectTrigger className="w-[130px]"><SelectValue /></SelectTrigger>
          <SelectContent>
            <SelectItem value="all">{locale === 'fr' ? 'Tous statuts' : 'All statuses'}</SelectItem>
            <SelectItem value="paid">{locale === 'fr' ? 'Payée' : 'Paid'}</SelectItem>
            <SelectItem value="pending">{locale === 'fr' ? 'En attente' : 'Pending'}</SelectItem>
            <SelectItem value="overdue">{locale === 'fr' ? 'En retard' : 'Overdue'}</SelectItem>
            <SelectItem value="cancelled">{locale === 'fr' ? 'Annulée' : 'Cancelled'}</SelectItem>
          </SelectContent>
        </Select>
      </div>

      {/* Table */}
      <Card>
        <CardContent className="p-0">
          <div className="overflow-x-auto">
            <table className="w-full text-sm">
              <thead>
                <tr className="border-b bg-gray-50">
                  <th className="p-3 text-left">N°</th>
                  <th className="p-3 text-left">Type</th>
                  <th className="p-3 text-left">Date</th>
                  <th className="p-3 text-left">{locale === 'fr' ? 'Commande' : 'Order'}</th>
                  <th className="p-3 text-right">HT</th>
                  <th className="p-3 text-right">TVA</th>
                  <th className="p-3 text-right">TTC</th>
                  <th className="p-3 text-center">Status</th>
                  <th className="p-3 text-right">Actions</th>
                </tr>
              </thead>
              <tbody>
                {filtered.map(inv => (
                  <tr key={inv.id} className="border-b hover:bg-gray-50/50">
                    <td className="p-3 font-medium text-brand-dark">{inv.id}</td>
                    <td className="p-3">
                      <div className="flex items-center gap-1">
                        {inv.type === 'credit_note' ? <RotateCcw className="w-3.5 h-3.5 text-error" /> : <FileText className="w-3.5 h-3.5 text-brand-primary" />}
                        <span className={inv.type === 'credit_note' ? 'text-error' : ''}>
                          {inv.type === 'credit_note' ? (locale === 'fr' ? 'Avoir' : 'Credit') : (locale === 'fr' ? 'Facture' : 'Invoice')}
                        </span>
                      </div>
                      {inv.relatedInvoiceId && (
                        <span className="text-xs text-muted-foreground block">{locale === 'fr' ? 'Réf' : 'Ref'}: {inv.relatedInvoiceId}</span>
                      )}
                    </td>
                    <td className="p-3 text-muted-foreground">{new Date(inv.date).toLocaleDateString(toIntlLocale(locale))}</td>
                    <td className="p-3 text-muted-foreground">{inv.orderId}</td>
                    <td className="p-3 text-right">{fmt(inv.amountHT)}</td>
                    <td className="p-3 text-right text-muted-foreground">{fmt(inv.vatAmount)}</td>
                    <td className="p-3 text-right font-medium">{fmt(inv.amountTTC)}</td>
                    <td className="p-3 text-center">
                      <Badge variant="outline" className={STATUS_COLORS[inv.status]}>{inv.status}</Badge>
                    </td>
                    <td className="p-3 text-right">
                      <Button size="icon" variant="ghost" className="h-7 w-7" onClick={() => handleDownload(inv)}>
                        <Download className="w-3.5 h-3.5" />
                      </Button>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </CardContent>
      </Card>
    </div>
  );
}
