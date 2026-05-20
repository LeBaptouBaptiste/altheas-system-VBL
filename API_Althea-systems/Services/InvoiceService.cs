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
    private readonly IOrderConfirmationSender _orderConfirmationSender;
    private readonly ILogger<InvoiceService> _logger;

    public InvoiceService(
        IInvoiceRepository invoiceRepository,
        IOrderRepository orderRepository,
        IOrderConfirmationSender orderConfirmationSender,
        ILogger<InvoiceService> logger)
    {
        _invoiceRepository = invoiceRepository;
        _orderRepository = orderRepository;
        _orderConfirmationSender = orderConfirmationSender;
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

    private static decimal GetVatMultiplier(Common.Enums.VatRate rate) => rate switch
    {
        Common.Enums.VatRate.Standard => 0.20m,
        Common.Enums.VatRate.Intermediate => 0.10m,
        Common.Enums.VatRate.Reduced => 0.055m,
        _ => 0m
    };

    private static InvoiceDto MapToDto(Invoice i) => new(
        i.Id, i.OrderId, i.Date, i.AmountHT, i.VatAmount, i.AmountTTC, i.Status, i.Type, i.RelatedInvoiceId);
}
