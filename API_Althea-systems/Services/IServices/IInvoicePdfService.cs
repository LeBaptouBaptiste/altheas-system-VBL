using API_Althea_systems.Models.Invoices;
using API_Althea_systems.Models.Order;
using API_Althea_systems.Models.Users;

namespace API_Althea_systems.Services.IServices;

/// <summary>
/// Generates printable PDF invoices from an Invoice + its Order context.
/// Implementation uses QuestPDF (managed library, bundles Lato + Inter fonts).
///
/// Returns raw bytes — the caller decides how to deliver them
/// (HTTP stream, blob storage upload, email attachment, …).
/// </summary>
public interface IInvoicePdfService
{
    /// <summary>
    /// Renders the invoice PDF. Throws ArgumentException if any required
    /// related entity (order, billing address, items) is missing — the
    /// caller is expected to pre-load the aggregate.
    /// </summary>
    byte[] Render(Invoice invoice, Order order, User user, Address billingAddress);
}
