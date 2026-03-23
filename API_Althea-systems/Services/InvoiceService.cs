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
