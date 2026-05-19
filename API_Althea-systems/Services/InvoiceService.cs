using API_Althea_systems.Common.Exceptions;
using API_Althea_systems.Models.Invoices;
using API_Althea_systems.Models.Shared;
using API_Althea_systems.Repositories.IRepositories;
using API_Althea_systems.Services.IServices;

namespace API_Althea_systems.Services;

public class InvoiceService : IInvoiceService
{
    private readonly IInvoiceRepository _invoiceRepository;
    private readonly IOrderRepository _orderRepository;

    public InvoiceService(IInvoiceRepository invoiceRepository, IOrderRepository orderRepository)
    {
        _invoiceRepository = invoiceRepository;
        _orderRepository = orderRepository;
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
