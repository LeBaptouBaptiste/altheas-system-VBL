namespace API_Althea_systems.Models.Invoices;

public class Invoice
{
    public Guid Id { get; set; }
    public Guid OrderId { get; set; }
    public DateTime Date { get; set; }
    public decimal AmountHT { get; set; }
    public decimal VatAmount { get; set; }
    public decimal AmountTTC { get; set; }
    public InvoiceStatus Status { get; set; } = InvoiceStatus.Pending;
    public InvoiceType Type { get; set; } = InvoiceType.Invoice;
    public Guid? RelatedInvoiceId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public Order.Order Order { get; set; } = null!;
    public Invoice? RelatedInvoice { get; set; }
}

public enum InvoiceStatus
{
    Paid,
    Pending,
    Overdue,
    Cancelled
}

public enum InvoiceType
{
    Invoice,
    CreditNote
}
