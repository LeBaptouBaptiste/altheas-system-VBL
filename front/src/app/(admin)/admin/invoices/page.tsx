'use client';

import { useState, useEffect, useMemo } from 'react';
import { Search, Download, FileText, RotateCcw, Loader2, FilePlus } from 'lucide-react';
import { Card, CardContent } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Badge } from '@/components/ui/badge';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';
import { Dialog, DialogClose, DialogContent, DialogHeader, DialogTitle } from '@/components/ui/dialog';
import { useI18n } from '@/context/i18n-context';
import { invoicesService, downloadInvoicePdf, downloadCreditNotePdf } from '@/lib/api-services';
import { getErrorMessage } from '@/lib/api-errors';
import type { InvoiceDto } from '@/lib/api-types';
import { formatPrice, toIntlLocale } from '@/lib/money';
import { InvoiceStatus, InvoiceType } from '@/lib/enums';
import { enumLabel } from '@/lib/enums';
import { toast } from 'sonner';

const STATUS_COLORS: Record<number, string> = {
  [InvoiceStatus.Paid]: 'bg-success/10 text-success border-success/20',
  [InvoiceStatus.Pending]: 'bg-warning/10 text-warning border-warning/20',
  [InvoiceStatus.Overdue]: 'bg-error/10 text-error border-error/20',
  [InvoiceStatus.Cancelled]: 'bg-gray-100 text-muted-foreground',
};

export default function AdminInvoicesPage() {
  const { locale } = useI18n();
  const fmt = (n: number) => formatPrice(n, toIntlLocale(locale));

  const [invoicesList, setInvoicesList] = useState<InvoiceDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [search, setSearch] = useState('');
  const [typeFilter, setTypeFilter] = useState('all');
  const [statusFilter, setStatusFilter] = useState('all');

  useEffect(() => {
    const load = async () => {
      try {
        const res = await invoicesService.getAll(1, 200);
        setInvoicesList(res.data);
      } catch (err) {
        console.error('Failed to load invoices', err);
      } finally {
        setLoading(false);
      }
    };
    load();
  }, []);

  const filtered = useMemo(() => {
    let list = [...invoicesList].sort((a, b) => b.date.localeCompare(a.date));
    if (search) {
      const q = search.toLowerCase();
      list = list.filter(i => i.id.toLowerCase().includes(q) || i.orderId.toLowerCase().includes(q));
    }
    if (typeFilter !== 'all') list = list.filter(i => i.type === Number(typeFilter));
    if (statusFilter !== 'all') list = list.filter(i => i.status === Number(statusFilter));
    return list;
  }, [invoicesList, search, typeFilter, statusFilter]);

  // Per-row download state so a slow PDF render doesn't freeze the whole
  // table — only the clicked row shows a spinner.
  const [downloadingId, setDownloadingId] = useState<string | null>(null);

  const handleDownload = async (inv: InvoiceDto) => {
    if (downloadingId) return;  // ignore double-clicks
    setDownloadingId(inv.id);
    try {
      // Use the credit-note helper for credit notes so the saved filename
      // is "avoir-XXX.pdf" instead of "facture-XXX.pdf".
      if (inv.type === InvoiceType.CreditNote) {
        await downloadCreditNotePdf(inv.id);
        toast.success(locale === 'fr' ? 'Avoir téléchargé' : 'Credit note downloaded');
      } else {
        await downloadInvoicePdf(inv.id);
        toast.success(locale === 'fr' ? 'Facture téléchargée' : 'Invoice downloaded');
      }
    } catch (err) {
      console.error('PDF download failed', err);
      toast.error(locale === 'fr' ? 'Échec du téléchargement' : 'Download failed');
    } finally {
      setDownloadingId(null);
    }
  };

  // ── Credit-note modal (phase 6) ──────────────────
  const [creditNoteTarget, setCreditNoteTarget] = useState<InvoiceDto | null>(null);
  const [creditAmount, setCreditAmount] = useState('');
  const [creditReason, setCreditReason] = useState('');
  const [creditSubmitting, setCreditSubmitting] = useState(false);

  const openCreditNoteModal = (inv: InvoiceDto) => {
    setCreditNoteTarget(inv);
    // Pre-fill with the remaining creditable amount as a sensible default.
    // The server is authoritative — we just give the admin a head start.
    const alreadyCredited = invoicesList
      .filter(i => i.type === InvoiceType.CreditNote && i.relatedInvoiceId === inv.id)
      .reduce((sum, cn) => sum + cn.amountHT, 0);
    const remainingHT = Math.max(0, inv.amountHT - alreadyCredited);
    setCreditAmount(remainingHT.toFixed(2));
    setCreditReason('');
  };

  const submitCreditNote = async () => {
    if (!creditNoteTarget) return;
    const amount = Number(creditAmount.replace(',', '.'));
    if (!Number.isFinite(amount) || amount <= 0) {
      toast.error(locale === 'fr' ? 'Montant invalide' : 'Invalid amount');
      return;
    }
    setCreditSubmitting(true);
    try {
      // Admin layout sets the ambient step-up token, so this endpoint
      // (RequireStepUp(Admin)) is auto-authenticated.
      const created = await invoicesService.issueCreditNote(
        creditNoteTarget.id,
        amount,
        creditReason.trim() || null,
      );
      // Refresh list to reflect totals and the new row.
      const res = await invoicesService.getAll(1, 200);
      setInvoicesList(res.data);
      // Offer the PDF immediately — common UX for accounting flows.
      await downloadCreditNotePdf(created.id);
      toast.success(locale === 'fr' ? 'Avoir émis' : 'Credit note issued');
      setCreditNoteTarget(null);
    } catch (err) {
      toast.error(getErrorMessage(err, (k) => k));
    } finally {
      setCreditSubmitting(false);
    }
  };

  const totals = filtered.reduce((acc, inv) => {
    if (inv.type === InvoiceType.CreditNote) acc.credits += inv.amountTTC;
    else acc.invoices += inv.amountTTC;
    return acc;
  }, { invoices: 0, credits: 0 });

  if (loading) {
    return (
      <div className="flex items-center justify-center h-64">
        <Loader2 className="w-8 h-8 animate-spin text-brand-primary" />
      </div>
    );
  }

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
          <Search className="absolute start-3 top-1/2 -translate-y-1/2 w-4 h-4 text-muted-foreground" />
          <Input value={search} onChange={e => setSearch(e.target.value)} placeholder={locale === 'fr' ? 'Rechercher...' : 'Search...'} className="ps-9" />
        </div>
        <Select value={typeFilter} onValueChange={setTypeFilter}>
          <SelectTrigger className="w-[140px]"><SelectValue /></SelectTrigger>
          <SelectContent>
            <SelectItem value="all">{locale === 'fr' ? 'Tous types' : 'All types'}</SelectItem>
            <SelectItem value={String(InvoiceType.Invoice)}>{locale === 'fr' ? 'Factures' : 'Invoices'}</SelectItem>
            <SelectItem value={String(InvoiceType.CreditNote)}>{locale === 'fr' ? 'Avoirs' : 'Credit notes'}</SelectItem>
          </SelectContent>
        </Select>
        <Select value={statusFilter} onValueChange={setStatusFilter}>
          <SelectTrigger className="w-[130px]"><SelectValue /></SelectTrigger>
          <SelectContent>
            <SelectItem value="all">{locale === 'fr' ? 'Tous statuts' : 'All statuses'}</SelectItem>
            <SelectItem value={String(InvoiceStatus.Paid)}>{enumLabel('InvoiceStatus', InvoiceStatus.Paid, locale)}</SelectItem>
            <SelectItem value={String(InvoiceStatus.Pending)}>{enumLabel('InvoiceStatus', InvoiceStatus.Pending, locale)}</SelectItem>
            <SelectItem value={String(InvoiceStatus.Overdue)}>{enumLabel('InvoiceStatus', InvoiceStatus.Overdue, locale)}</SelectItem>
            <SelectItem value={String(InvoiceStatus.Cancelled)}>{enumLabel('InvoiceStatus', InvoiceStatus.Cancelled, locale)}</SelectItem>
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
                  <th className="p-3 text-start">N°</th>
                  <th className="p-3 text-start">Type</th>
                  <th className="p-3 text-start">Date</th>
                  <th className="p-3 text-start">{locale === 'fr' ? 'Commande' : 'Order'}</th>
                  <th className="p-3 text-end">HT</th>
                  <th className="p-3 text-end">TVA</th>
                  <th className="p-3 text-end">TTC</th>
                  <th className="p-3 text-center">Status</th>
                  <th className="p-3 text-end">Actions</th>
                </tr>
              </thead>
              <tbody>
                {filtered.map(inv => (
                  <tr key={inv.id} className="border-b hover:bg-gray-50/50">
                    <td className="p-3 font-medium text-brand-dark">{inv.id}</td>
                    <td className="p-3">
                      <div className="flex items-center gap-1">
                        {inv.type === InvoiceType.CreditNote ? <RotateCcw className="w-3.5 h-3.5 text-error" /> : <FileText className="w-3.5 h-3.5 text-brand-primary" />}
                        <span className={inv.type === InvoiceType.CreditNote ? 'text-error' : ''}>
                          {enumLabel('InvoiceType', inv.type, locale)}
                        </span>
                      </div>
                      {inv.relatedInvoiceId && (
                        <span className="text-xs text-muted-foreground block">{locale === 'fr' ? 'Réf' : 'Ref'}: {inv.relatedInvoiceId}</span>
                      )}
                    </td>
                    <td className="p-3 text-muted-foreground">{new Date(inv.date).toLocaleDateString(toIntlLocale(locale))}</td>
                    <td className="p-3 text-muted-foreground">{inv.orderId}</td>
                    <td className="p-3 text-end">{fmt(inv.amountHT)}</td>
                    <td className="p-3 text-end text-muted-foreground">{fmt(inv.vatAmount)}</td>
                    <td className="p-3 text-end font-medium">{fmt(inv.amountTTC)}</td>
                    <td className="p-3 text-center">
                      <Badge variant="outline" className={STATUS_COLORS[inv.status] || ''}>{enumLabel('InvoiceStatus', inv.status, locale)}</Badge>
                    </td>
                    <td className="p-3 text-end">
                      <div className="inline-flex gap-1">
                        {/* Credit-note CTA: only for PAID invoices (and not
                            on credit notes themselves — no CN-on-CN). */}
                        {inv.type === InvoiceType.Invoice
                          && inv.status === InvoiceStatus.Paid && (
                          <Button
                            size="icon"
                            variant="ghost"
                            className="h-7 w-7 text-error hover:bg-error/10"
                            onClick={() => openCreditNoteModal(inv)}
                            title={locale === 'fr' ? 'Émettre un avoir' : 'Issue credit note'}
                          >
                            <FilePlus className="w-3.5 h-3.5" />
                          </Button>
                        )}
                        <Button
                          size="icon"
                          variant="ghost"
                          className="h-7 w-7"
                          onClick={() => handleDownload(inv)}
                          disabled={downloadingId === inv.id}
                          title={inv.type === InvoiceType.CreditNote
                            ? (locale === 'fr' ? "Télécharger l'avoir (PDF)" : 'Download credit note (PDF)')
                            : (locale === 'fr' ? 'Télécharger la facture (PDF)' : 'Download invoice (PDF)')}
                        >
                          {downloadingId === inv.id
                            ? <Loader2 className="w-3.5 h-3.5 animate-spin" />
                            : <Download className="w-3.5 h-3.5" />}
                        </Button>
                      </div>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </CardContent>
      </Card>

      {/* Credit-note modal (phase 6) */}
      <Dialog open={!!creditNoteTarget} onOpenChange={(open) => !open && setCreditNoteTarget(null)}>
        <DialogContent className="max-w-md">
          <DialogHeader>
            <DialogTitle>
              {locale === 'fr' ? 'Émettre un avoir' : 'Issue credit note'}
            </DialogTitle>
          </DialogHeader>
          {creditNoteTarget && (() => {
            const alreadyCredited = invoicesList
              .filter(i => i.type === InvoiceType.CreditNote && i.relatedInvoiceId === creditNoteTarget.id)
              .reduce((sum, cn) => sum + cn.amountHT, 0);
            const remainingHT = Math.max(0, creditNoteTarget.amountHT - alreadyCredited);
            return (
              <div className="space-y-4 text-sm">
                <div className="bg-gray-50 rounded-md border p-3 space-y-1">
                  <p className="text-xs text-muted-foreground">
                    {locale === 'fr' ? 'Facture source' : 'Source invoice'}
                  </p>
                  <p className="font-medium">#{creditNoteTarget.id.slice(0, 8).toUpperCase()}</p>
                  <p className="text-xs text-muted-foreground">
                    {locale === 'fr' ? 'Montant HT facture' : 'Invoice amount HT'}: <strong>{fmt(creditNoteTarget.amountHT)}</strong>
                    {alreadyCredited > 0 && (
                      <> · {locale === 'fr' ? 'Déjà crédité' : 'Already credited'}: <strong>{fmt(alreadyCredited)}</strong></>
                    )}
                    {' '}· {locale === 'fr' ? 'Restant' : 'Remaining'}: <strong className="text-error">{fmt(remainingHT)}</strong>
                  </p>
                </div>

                <div>
                  <Label htmlFor="creditAmount">
                    {locale === 'fr' ? 'Montant HT à créditer (€)' : 'Amount to credit HT (€)'}
                  </Label>
                  <Input
                    id="creditAmount"
                    type="number"
                    step="0.01"
                    min="0.01"
                    max={remainingHT}
                    value={creditAmount}
                    onChange={(e) => setCreditAmount(e.target.value)}
                    autoFocus
                  />
                  <p className="text-xs text-muted-foreground mt-1">
                    {locale === 'fr'
                      ? 'La TVA est calculée automatiquement au taux effectif de la facture source.'
                      : 'VAT is auto-prorated at the source invoice effective rate.'}
                  </p>
                </div>

                <div>
                  <Label htmlFor="creditReason">
                    {locale === 'fr' ? 'Motif (optionnel)' : 'Reason (optional)'}
                  </Label>
                  <Input
                    id="creditReason"
                    type="text"
                    maxLength={500}
                    placeholder={locale === 'fr'
                      ? 'Ex : produit défectueux, retour client...'
                      : 'e.g. defective product, customer return...'}
                    value={creditReason}
                    onChange={(e) => setCreditReason(e.target.value)}
                  />
                </div>

                <div className="flex gap-2 pt-2">
                  <DialogClose asChild>
                    <Button variant="outline" className="flex-1" disabled={creditSubmitting}>
                      {locale === 'fr' ? 'Annuler' : 'Cancel'}
                    </Button>
                  </DialogClose>
                  <Button
                    onClick={submitCreditNote}
                    disabled={creditSubmitting || remainingHT <= 0}
                    className="flex-1 bg-error hover:bg-error/90 text-white"
                  >
                    {creditSubmitting
                      ? <Loader2 className="w-4 h-4 animate-spin" />
                      : (locale === 'fr' ? 'Émettre l’avoir' : 'Issue credit note')}
                  </Button>
                </div>
              </div>
            );
          })()}
        </DialogContent>
      </Dialog>
    </div>
  );
}
