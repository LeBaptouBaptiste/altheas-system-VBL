using API_Althea_systems.Common.Enums;
using API_Althea_systems.Common.Exceptions;
using API_Althea_systems.Models.Invoices;
using API_Althea_systems.Models.Shared;
using API_Althea_systems.Repositories.IRepositories;
using API_Althea_systems.Services.Email;
using API_Althea_systems.Services.IServices;

namespace API_Althea_systems.Services;

public class InvoiceService : IInvoiceService
{
    private readonly IInvoiceRepository _invoiceRepository;
    private readonly IOrderRepository _orderRepository;
    private readonly IUserRepository _userRepository;
    private readonly IOrderConfirmationSender _orderConfirmationSender;
    private readonly ICreditNoteSender _creditNoteSender;
    private readonly IStripeRefundService _refunds;
    private readonly ILogger<InvoiceService> _logger;

    public InvoiceService(
        IInvoiceRepository invoiceRepository,
        IOrderRepository orderRepository,
        IUserRepository userRepository,
        IOrderConfirmationSender orderConfirmationSender,
        ICreditNoteSender creditNoteSender,
        IStripeRefundService refunds,
        ILogger<InvoiceService> logger)
    {
        _invoiceRepository = invoiceRepository;
        _orderRepository = orderRepository;
        _userRepository = userRepository;
        _orderConfirmationSender = orderConfirmationSender;
        _creditNoteSender = creditNoteSender;
        _refunds = refunds;
        _logger = logger;
    }

    public async Task<InvoiceDto> GetByIdAsync(Guid id)
    {
        var invoice = await _invoiceRepository.GetByIdAsync(id)
            ?? throw new NotFoundException("Invoice", id);
        return MapToDto(invoice);
    }

    public async Task<PaginatedResponse<InvoiceDto>> GetAllAsync(int page, int pageSize)
    {
        var invoices = await _invoiceRepository.GetAllAsync(page, pageSize);
        var total = await _invoiceRepository.CountAsync();
        return new PaginatedResponse<InvoiceDto>(
            invoices.Select(MapToDto), page, pageSize, total, (int)Math.Ceiling(total / (double)pageSize));
    }

    public async Task<InvoiceDto> CreateAsync(InvoiceCreateRequest request)
    {
        var order = await _orderRepository.GetByIdAsync(request.OrderId)
            ?? throw new NotFoundException("Order", request.OrderId);

        var totalHT = order.Items.Sum(i => i.PriceHT * i.Quantity);
        var vatAmount = order.Items.Sum(i => i.PriceHT * i.Quantity * GetVatMultiplier(i.VatRate));

        var invoice = new Invoice
        {
            Id = Guid.NewGuid(),
            OrderId = request.OrderId,
            Date = DateTime.UtcNow,
            AmountHT = totalHT,
            VatAmount = vatAmount,
            AmountTTC = totalHT + vatAmount,
            Type = request.Type,
            RelatedInvoiceId = request.RelatedInvoiceId,
            Status = InvoiceStatus.Pending
        };

        await _invoiceRepository.CreateAsync(invoice);
        return MapToDto(invoice);
    }

    public async Task<InvoiceDto> EnsureForOrderAsync(Guid orderId)
    {
        var order = await _orderRepository.GetByIdAsync(orderId)
            ?? throw new NotFoundException("Order", orderId);

        // Idempotent: an existing Type=Invoice (credit notes don't count) means
        // we already issued one for this order — return it instead of creating
        // a duplicate. Stripe redelivers webhooks and the startup backfill runs
        // every boot, so this method MUST be safe to call repeatedly.
        var existing = order.Invoices
            .Where(i => i.Type == InvoiceType.Invoice)
            .OrderByDescending(i => i.Date)
            .FirstOrDefault();
        if (existing != null)
            return MapToDto(existing);

        var totalHT = order.Items.Sum(i => i.PriceHT * i.Quantity);
        var vatAmount = order.Items.Sum(i => i.PriceHT * i.Quantity * GetVatMultiplier(i.VatRate));

        var invoice = new Invoice
        {
            Id = Guid.NewGuid(),
            OrderId = order.Id,
            Date = DateTime.UtcNow,
            AmountHT = totalHT,
            VatAmount = vatAmount,
            // Match OrderDto.TotalTTC = HT + VAT + shipping. The customer paid
            // shipping too, so the invoice must reflect it.
            AmountTTC = totalHT + vatAmount + order.ShippingCost,
            // Auto-issued from a successful payment → already paid. Manual
            // POST /invoices keeps its own Pending default for admin-driven
            // workflows (deposit invoices, etc.).
            Status = InvoiceStatus.Paid,
            Type = InvoiceType.Invoice,
        };

        await _invoiceRepository.CreateAsync(invoice);
        return MapToDto(invoice);
    }

    public async Task EnsureEmailedAsync(Guid orderId, CancellationToken ct = default)
    {
        var order = await _orderRepository.GetByIdAsync(orderId);
        if (order is null)
        {
            _logger.LogWarning("EnsureEmailedAsync: order {OrderId} not found — skipping.", orderId);
            return;
        }

        // Pick the most recent Type=Invoice (credit notes excluded) — same
        // selector as OrderService.MapToDto's LatestInvoiceId.
        var invoice = order.Invoices
            .Where(i => i.Type == InvoiceType.Invoice)
            .OrderByDescending(i => i.Date)
            .FirstOrDefault();

        if (invoice is null)
        {
            // Caller should have run EnsureForOrderAsync first. Don't blow
            // up — just log and let the next webhook retry / backfill catch it.
            _logger.LogWarning(
                "EnsureEmailedAsync: order {OrderId} has no Type=Invoice yet — skipping. " +
                "Call EnsureForOrderAsync first.", orderId);
            return;
        }

        if (invoice.EmailedAt is not null)
        {
            // Already sent. Stripe redeliveries / boot backfill all land here.
            _logger.LogDebug(
                "EnsureEmailedAsync: invoice {InvoiceId} already emailed at {EmailedAt} — skipping.",
                invoice.Id, invoice.EmailedAt);
            return;
        }

        if (order.User is null)
        {
            // GetByIdAsync Includes User, so this would be a DB integrity
            // issue (orphaned order). Don't crash the webhook over it.
            _logger.LogError(
                "EnsureEmailedAsync: order {OrderId} has no User loaded — DB issue, skipping mail.",
                orderId);
            return;
        }

        try
        {
            await _orderConfirmationSender.SendAsync(invoice, order, order.User, ct);
        }
        catch (Exception ex)
        {
            // Don't mark EmailedAt on failure — we want the next retry
            // (webhook redelivery / next boot's backfill if we add it) to
            // try again. Log loud.
            _logger.LogError(ex,
                "EnsureEmailedAsync: failed to send confirmation for invoice {InvoiceId} (order {OrderId}). " +
                "EmailedAt left null so a retry can attempt again.",
                invoice.Id, orderId);
            throw;
        }

        invoice.EmailedAt = DateTime.UtcNow;
        await _invoiceRepository.UpdateAsync(invoice);

        _logger.LogInformation(
            "Order-confirmation email sent for invoice {InvoiceId} (order {OrderId}) to {Email}.",
            invoice.Id, orderId, order.User.Email);
    }

    public async Task<InvoiceDto> IssueCreditNoteAsync(
        Guid originalInvoiceId,
        IssueCreditNoteRequest request,
        CancellationToken ct = default)
    {
        // ── Validate original invoice ─────────────────────
        var original = await _invoiceRepository.GetByIdAsync(originalInvoiceId)
            ?? throw new NotFoundException("Invoice", originalInvoiceId);

        if (original.Type != InvoiceType.Invoice)
        {
            // A credit-note-against-a-credit-note doesn't model anything useful;
            // accounting would refuse to reconcile it.
            throw new BadRequestException(
                "Credit notes can only be issued against an Invoice (not against another credit note).",
                reason: "not_an_invoice");
        }

        if (original.Status != InvoiceStatus.Paid)
        {
            // Refunding an unpaid invoice doesn't make sense — just cancel it.
            throw new BadRequestException(
                "Credit notes can only be issued against a Paid invoice.",
                reason: "not_paid");
        }

        // ── Validate request amount ───────────────────────
        if (request.AmountHT <= 0)
        {
            throw new BadRequestException(
                "Credit note amount must be greater than zero.",
                reason: "invalid_amount");
        }

        // Sum prior credit notes against this invoice to enforce
        // cumulative cap. Partial / multiple credits are supported as long
        // as the total stays ≤ original.AmountHT.
        var priorCreditNotes = await _invoiceRepository.GetCreditNotesForInvoiceAsync(originalInvoiceId);
        var alreadyCreditedHT = priorCreditNotes.Sum(cn => cn.AmountHT);
        var remainingHT = original.AmountHT - alreadyCreditedHT;

        if (request.AmountHT > remainingHT)
        {
            throw new BadRequestException(
                $"Credit note amount ({request.AmountHT:N2} €) exceeds the remaining creditable amount " +
                $"({remainingHT:N2} € — {alreadyCreditedHT:N2} € already credited against this invoice).",
                reason: "exceeds_remaining");
        }

        // ── Build credit note ─────────────────────────────
        // Prorata VAT at the original invoice's effective rate so the
        // HT/TTC ratio matches what was originally charged. Banker's
        // rounding via Math.Round(MidpointRounding.ToEven) — same as the
        // OrderConfirmation totals.
        var effectiveVatRate = original.AmountHT > 0
            ? original.VatAmount / original.AmountHT
            : 0m;
        var vatAmount = Math.Round(request.AmountHT * effectiveVatRate, 2, MidpointRounding.ToEven);

        var amountTTC = request.AmountHT + vatAmount;
        var creditNote = new Invoice
        {
            // Leave Id at default — see Address / OrderStatusChange: EF
            // marks default-PK additions as Added → INSERT.
            OrderId = original.OrderId,
            Date = DateTime.UtcNow,
            AmountHT = request.AmountHT,
            VatAmount = vatAmount,
            AmountTTC = amountTTC,
            Type = InvoiceType.CreditNote,
            RelatedInvoiceId = original.Id,
            // A credit note is "paid back" the moment it's issued from the
            // customer's POV; the refund execution is a separate workflow
            // (manual SEPA / Stripe refund). Marking Paid keeps the
            // accounting view consistent.
            Status = InvoiceStatus.Paid,
            Mode = request.Mode,
        };

        // ── Phase 7: money actually moves ─────────────────
        // Refund mode: hit Stripe SYNCHRONOUSLY before persisting the credit
        // note. If Stripe rejects (insufficient funds on the PI, already
        // refunded, network error), we surface the error and the credit note
        // is NOT created — admin must retry. This keeps a "credit note in DB
        // without a refund attached" from being possible in Refund mode.
        if (request.Mode == CreditNoteMode.Refund)
        {
            var order = await _orderRepository.GetByIdAsync(original.OrderId);
            if (order is null || string.IsNullOrWhiteSpace(order.StripePaymentIntentId))
            {
                throw new BadRequestException(
                    "Cannot refund: original order has no Stripe PaymentIntent. " +
                    "Use Mode=StoreCredit for orders paid outside Stripe.",
                    reason: "no_payment_intent");
            }

            var amountCents = (long)Math.Round(amountTTC * 100m);
            try
            {
                var (refundId, refundStatus) = await _refunds.RefundAsync(
                    order.StripePaymentIntentId,
                    amountCents,
                    reason: "requested_by_customer",
                    ct);
                creditNote.StripeRefundId = refundId;
                creditNote.RefundStatus = refundStatus;
            }
            catch (Stripe.StripeException ex)
            {
                _logger.LogError(ex,
                    "Stripe rejected refund on PI {IntentId} for {Amount} cents.",
                    order.StripePaymentIntentId, amountCents);
                throw new BadRequestException(
                    $"Stripe refund failed: {ex.Message}",
                    reason: "stripe_refund_failed");
            }
        }
        else if (request.Mode == CreditNoteMode.StoreCredit)
        {
            // Credit the customer's wallet. Two saves (user then credit note)
            // instead of one transaction — accept the small race window where
            // a crash between them could leave the wallet bumped without a
            // credit note row (which the admin would notice on the next
            // reconciliation pass). The reverse — credit note created without
            // wallet bump — is what we MUST avoid; doing the user save first
            // ensures the wallet is right even if the invoice insert fails.
            var order = await _orderRepository.GetByIdAsync(original.OrderId)
                ?? throw new InvalidOperationException(
                    $"Cannot credit store balance: order {original.OrderId} not found.");
            var user = order.User ?? await _userRepository.GetByIdAsync(order.UserId)
                ?? throw new InvalidOperationException(
                    $"Cannot credit store balance: user {order.UserId} not found.");
            user.CreditBalanceCents += (long)Math.Round(amountTTC * 100m);
            await _userRepository.UpdateAsync(user);
        }

        await _invoiceRepository.CreateAsync(creditNote);

        _logger.LogInformation(
            "Credit note {CreditNoteId} issued against invoice {OriginalInvoiceId} " +
            "for {Amount:N2} € HT (mode={Mode}, reason: {Reason}).",
            creditNote.Id, original.Id, creditNote.AmountHT, creditNote.Mode,
            string.IsNullOrWhiteSpace(request.Reason) ? "(none)" : request.Reason);

        // ── Email customer (best-effort) ──────────────────
        try
        {
            var order = await _orderRepository.GetByIdAsync(original.OrderId);
            if (order?.User is not null)
            {
                await _creditNoteSender.SendAsync(creditNote, original, order, order.User, request.Reason, ct);
            }
            else
            {
                _logger.LogWarning(
                    "Cannot email credit note {CreditNoteId}: order {OrderId} or User missing.",
                    creditNote.Id, original.OrderId);
            }
        }
        catch (Exception ex)
        {
            // Credit note is committed — a flaky SMTP must not be an excuse
            // to refuse a refund. Admin can re-mail manually if needed.
            _logger.LogError(ex,
                "Credit note {CreditNoteId} created but email failed to send.",
                creditNote.Id);
        }

        return MapToDto(creditNote);
    }

    private static decimal GetVatMultiplier(Common.Enums.VatRate rate) => rate switch
    {
        Common.Enums.VatRate.Standard => 0.20m,
        Common.Enums.VatRate.Intermediate => 0.10m,
        Common.Enums.VatRate.Reduced => 0.055m,
        _ => 0m
    };

    private static InvoiceDto MapToDto(Invoice i) => new(
        i.Id, i.OrderId, i.Date, i.AmountHT, i.VatAmount, i.AmountTTC,
        i.Status, i.Type, i.RelatedInvoiceId,
        i.Mode, i.StripeRefundId, i.RefundStatus);
}
