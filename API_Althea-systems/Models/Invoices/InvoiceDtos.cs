namespace API_Althea_systems.Models.Invoices;

public record InvoiceDto(
    Guid Id,
    Guid OrderId,
    DateTime Date,
    decimal AmountHT,
    decimal VatAmount,
    decimal AmountTTC,
    InvoiceStatus Status,
    InvoiceType Type,
    Guid? RelatedInvoiceId
);

public record InvoiceCreateRequest(
    Guid OrderId,
    InvoiceType Type,
    Guid? RelatedInvoiceId
);
