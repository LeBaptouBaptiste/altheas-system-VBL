using API_Althea_systems.Common.Enums;
using API_Althea_systems.Models.Invoices;
using API_Althea_systems.Models.Users;

namespace API_Althea_systems.Models.Order;

public record OrderDto(
    Guid Id,
    Guid UserId,
    string UserName,
    DateTime Date,
    OrderStatus Status,
    PaymentStatus PaymentStatus,
    PaymentMethod PaymentMethod,
    AddressDto BillingAddress,
    AddressDto ShippingAddress,
    ShippingMethod ShippingMethod,
    decimal ShippingCost,
    decimal TotalHT,
    decimal TotalVAT,
    decimal TotalTTC,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    IEnumerable<OrderItemDto> Items,
    IEnumerable<OrderStatusChangeDto> StatusHistory,
    // ID of the most recent Invoice (Type=Invoice, not CreditNote) attached to
    // this order, or null if no invoice has been issued yet. /account/orders
    // uses it to decide whether to enable the "download" button.
    Guid? LatestInvoiceId,
    // Phase 6: every invoice + credit note attached to the order, oldest
    // first. The front lists them all under the order with type-aware icons
    // and individual download buttons. Empty when no invoice has been
    // issued yet.
    IEnumerable<OrderInvoiceSummaryDto> Invoices,
    // Phase 7 (store credit): amount of store credit (cents EUR) applied
    // at checkout. 0 when the customer didn't redeem credit on this order.
    // Total paid to Stripe was TotalTTC*100 − CreditAppliedCents.
    long CreditAppliedCents = 0
);

/// <summary>
/// Compact view of an invoice attached to an order — just enough for the
/// account-orders UI to list and offer downloads without pulling the full
/// InvoiceDto graph.
/// </summary>
public record OrderInvoiceSummaryDto(
    Guid Id,
    InvoiceType Type,
    DateTime Date,
    decimal AmountTTC,
    InvoiceStatus Status,
    // Set on credit notes — lets the UI show "Avoir sur facture #ABC".
    Guid? RelatedInvoiceId
);

public record OrderItemDto(
    Guid ProductId,
    string ProductNameFr,
    string ProductNameEn,
    int Quantity,
    decimal PriceHT,
    VatRate VatRate
);

public record OrderStatusChangeDto(
    OrderStatus From,
    OrderStatus To,
    DateTime Date,
    Guid UserId
);

public record OrderCreateRequest(
    Guid BillingAddressId,
    Guid ShippingAddressId,
    ShippingMethod ShippingMethod,
    PaymentMethod PaymentMethod,
    IEnumerable<OrderItemCreateRequest> Items,
    // Phase 7: store credit (cents EUR) the customer wants applied to this
    // order at checkout. Server validates user.CreditBalanceCents ≥ this
    // value before accepting. Capped server-side at total − 50 cents
    // (Stripe's minimum charge in EUR) — if the customer wants 100% credit,
    // they need to remove items.
    long CreditAppliedCents = 0
);

public record OrderItemCreateRequest(
    Guid ProductId,
    int Quantity
);

public record OrderStatusUpdateRequest(OrderStatus Status);
