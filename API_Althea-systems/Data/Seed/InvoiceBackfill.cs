using API_Althea_systems.Common.Enums;
using API_Althea_systems.Models.Invoices;
using API_Althea_systems.Services.IServices;
using Microsoft.EntityFrameworkCore;

namespace API_Althea_systems.Data.Seed;

/// <summary>
/// One-shot startup task that issues invoices for orders whose payment has
/// already cleared but never got one (legacy orders from before auto-issuance
/// landed, or webhook failures where EnsureForOrderAsync's catch fired).
/// Idempotent — runs every boot but only acts on orders missing an invoice.
/// </summary>
public static class InvoiceBackfill
{
    public static async Task RunAsync(AltheaDbContext db, IInvoiceService invoices, ILogger logger)
    {
        // Pull only the IDs to keep this cheap on large tables. The filter
        // "no Type=Invoice exists" mirrors EnsureForOrderAsync's idempotency
        // check, so credit-note-only orders still get a real invoice issued.
        var orderIds = await db.Orders
            .Where(o => o.PaymentStatus == PaymentStatus.Validated
                     && !o.Invoices.Any(i => i.Type == InvoiceType.Invoice))
            .Select(o => o.Id)
            .ToListAsync();

        if (orderIds.Count == 0)
        {
            logger.LogInformation("Invoice backfill: nothing to do.");
            return;
        }

        logger.LogInformation("Invoice backfill: issuing invoices for {Count} order(s).", orderIds.Count);

        var issued = 0;
        foreach (var orderId in orderIds)
        {
            try
            {
                await invoices.EnsureForOrderAsync(orderId);
                issued++;
            }
            catch (Exception ex)
            {
                // One bad order shouldn't block the rest of the boot — log and
                // move on; ops can inspect from /admin/invoices afterwards.
                logger.LogError(ex,
                    "Invoice backfill: failed to issue invoice for order {OrderId}.",
                    orderId);
            }
        }

        logger.LogInformation("Invoice backfill: issued {Issued}/{Total}.", issued, orderIds.Count);
    }
}
