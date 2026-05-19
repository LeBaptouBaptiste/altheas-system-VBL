using API_Althea_systems.Common.Enums;
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
    Guid? LatestInvoiceId
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
    IEnumerable<OrderItemCreateRequest> Items
);

public record OrderItemCreateRequest(
    Guid ProductId,
    int Quantity
);

public record OrderStatusUpdateRequest(OrderStatus Status);
