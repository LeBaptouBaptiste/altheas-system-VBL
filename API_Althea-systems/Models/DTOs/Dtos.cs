using API_Althea_systems.Common.Enums;
using API_Althea_systems.Models.Invoices;
using API_Althea_systems.Models.Messaging;

namespace API_Althea_systems.Models.DTOs;

// ══════════════════════════════════════════════════════════════
// AUTH
// ══════════════════════════════════════════════════════════════

public record RegisterRequest(
    string Name,
    string Email,
    string Password,
    string ConfirmPassword
);

public record LoginRequest(
    string Email,
    string Password
);

public record ForgotPasswordRequest(string Email);

public record ResetPasswordRequest(
    string Token,
    string NewPassword,
    string ConfirmPassword
);

public record ConfirmEmailRequest(string Token);

public record Verify2FaRequest(string Code);

public record AuthResponse(
    string AccessToken,
    string RefreshToken,
    UserDto User
);

public record RefreshTokenRequest(string RefreshToken);

// ══════════════════════════════════════════════════════════════
// USER
// ══════════════════════════════════════════════════════════════

public record UserDto(
    Guid Id,
    string Name,
    string Email,
    UserRole Role,
    UserStatus Status,
    bool Anonymized,
    bool EmailConfirmed,
    bool TwoFactorEnabled,
    DateTime? LastLogin,
    DateTime CreatedAt,
    IEnumerable<AddressDto> Addresses,
    IEnumerable<PaymentMethodDto> PaymentMethods
);

public record UserUpdateRequest(
    string? Name,
    string? Email,
    UserStatus? Status
);

public record AddressDto(
    Guid Id,
    string Label,
    string FirstName,
    string LastName,
    string? Company,
    string Street,
    string? Street2,
    string City,
    string PostalCode,
    string Country,
    string? Phone
);

public record AddressCreateRequest(
    string Label,
    string FirstName,
    string LastName,
    string? Company,
    string Street,
    string? Street2,
    string City,
    string PostalCode,
    string Country,
    string? Phone
);

public record PaymentMethodDto(
    Guid Id,
    string Type,
    string Label
);

public record PaymentMethodCreateRequest(
    string Type,
    string Label
);

// ══════════════════════════════════════════════════════════════
// PRODUCT
// ══════════════════════════════════════════════════════════════

public record ProductDto(
    Guid Id,
    string Slug,
    string NameFr,
    string NameEn,
    string DescriptionFr,
    string DescriptionEn,
    string LongDescriptionFr,
    string LongDescriptionEn,
    decimal PriceHT,
    VatRate VatRate,
    int StockQty,
    StockStatus StockStatus,
    bool IsNew,
    int PriorityRank,
    string[] Images,
    ProductStatus Status,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    IEnumerable<CategorySummaryDto> Categories,
    IEnumerable<ProductSpecDto> Specs
);

public record ProductCreateRequest(
    string Slug,
    string NameFr,
    string NameEn,
    string DescriptionFr,
    string DescriptionEn,
    string LongDescriptionFr,
    string LongDescriptionEn,
    decimal PriceHT,
    VatRate VatRate,
    int StockQty,
    StockStatus StockStatus,
    bool IsNew,
    int PriorityRank,
    Guid[] CategoryIds,
    string[] Images,
    ProductStatus Status,
    IEnumerable<ProductSpecRequest> Specs
);

public record ProductUpdateRequest(
    string? Slug,
    string? NameFr,
    string? NameEn,
    string? DescriptionFr,
    string? DescriptionEn,
    string? LongDescriptionFr,
    string? LongDescriptionEn,
    decimal? PriceHT,
    VatRate? VatRate,
    int? StockQty,
    StockStatus? StockStatus,
    bool? IsNew,
    int? PriorityRank,
    Guid[]? CategoryIds,
    string[]? Images,
    ProductStatus? Status,
    IEnumerable<ProductSpecRequest>? Specs
);

public record ProductSpecDto(string Label, string Value);
public record ProductSpecRequest(string Label, string Value);

// ══════════════════════════════════════════════════════════════
// CATEGORY
// ══════════════════════════════════════════════════════════════

public record CategoryDto(
    Guid Id,
    string Slug,
    string NameFr,
    string NameEn,
    string DescriptionFr,
    string DescriptionEn,
    string Image,
    Guid? ParentId,
    int DisplayOrder,
    bool Active,
    int ProductCount
);

public record CategorySummaryDto(Guid Id, string Slug, string NameFr, string NameEn);

public record CategoryCreateRequest(
    string Slug,
    string NameFr,
    string NameEn,
    string DescriptionFr,
    string DescriptionEn,
    string Image,
    Guid? ParentId,
    int DisplayOrder,
    bool Active
);

public record CategoryUpdateRequest(
    string? Slug,
    string? NameFr,
    string? NameEn,
    string? DescriptionFr,
    string? DescriptionEn,
    string? Image,
    Guid? ParentId,
    int? DisplayOrder,
    bool? Active
);

// ══════════════════════════════════════════════════════════════
// ORDER
// ══════════════════════════════════════════════════════════════

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
    IEnumerable<OrderStatusChangeDto> StatusHistory
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

// ══════════════════════════════════════════════════════════════
// INVOICE
// ══════════════════════════════════════════════════════════════

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

// ══════════════════════════════════════════════════════════════
// MESSAGING
// ══════════════════════════════════════════════════════════════

public record ContactMessageDto(
    Guid Id,
    string Email,
    string Subject,
    string Message,
    MessageStatus Status,
    DateTime CreatedAt
);

public record ContactMessageCreateRequest(
    string Email,
    string Subject,
    string Message
);

public record ChatConversationDto(
    Guid Id,
    Guid? UserId,
    string? Email,
    bool Escalated,
    Guid? TicketId,
    DateTime CreatedAt,
    IEnumerable<ChatMessageDto> Messages
);

public record ChatMessageDto(
    Guid Id,
    ChatRole Role,
    string Content,
    DateTime Timestamp
);

public record ChatMessageCreateRequest(string Content);

public record SupportTicketDto(
    Guid Id,
    Guid? ConversationId,
    Guid? ContactMessageId,
    string Email,
    string Subject,
    TicketStatus Status,
    DateTime CreatedAt,
    DateTime UpdatedAt
);

public record TicketUpdateRequest(TicketStatus Status);

// ══════════════════════════════════════════════════════════════
// CONTENT
// ══════════════════════════════════════════════════════════════

public record HeroSlideDto(
    Guid Id,
    string Image,
    string TitleFr,
    string TitleEn,
    string SubtitleFr,
    string SubtitleEn,
    string DescriptionFr,
    string DescriptionEn,
    string CtaFr,
    string CtaEn,
    string Link,
    int DisplayOrder,
    bool Active
);

public record HeroSlideCreateRequest(
    string Image,
    string TitleFr,
    string TitleEn,
    string SubtitleFr,
    string SubtitleEn,
    string DescriptionFr,
    string DescriptionEn,
    string CtaFr,
    string CtaEn,
    string Link,
    int DisplayOrder,
    bool Active
);

public record HeroSlideUpdateRequest(
    string? Image,
    string? TitleFr,
    string? TitleEn,
    string? SubtitleFr,
    string? SubtitleEn,
    string? DescriptionFr,
    string? DescriptionEn,
    string? CtaFr,
    string? CtaEn,
    string? Link,
    int? DisplayOrder,
    bool? Active
);

public record StaticPageDto(
    Guid Id,
    string Slug,
    string TitleFr,
    string TitleEn,
    string ContentFr,
    string ContentEn,
    DateTime UpdatedAt
);

public record StaticPageCreateRequest(
    string Slug,
    string TitleFr,
    string TitleEn,
    string ContentFr,
    string ContentEn
);

public record StaticPageUpdateRequest(
    string? Slug,
    string? TitleFr,
    string? TitleEn,
    string? ContentFr,
    string? ContentEn
);

// ══════════════════════════════════════════════════════════════
// ANALYTICS
// ══════════════════════════════════════════════════════════════

public record SalesAnalyticsDto(
    DateOnly Date,
    decimal Revenue,
    int OrderCount,
    Dictionary<string, decimal> CategoryBreakdown
);

public record DashboardKpiDto(
    decimal TotalRevenue,
    int TotalOrders,
    int TotalCustomers,
    int TotalProducts,
    decimal AverageOrderValue,
    IEnumerable<SalesAnalyticsDto> DailySales
);

// ══════════════════════════════════════════════════════════════
// SHARED
// ══════════════════════════════════════════════════════════════

public record PaginatedResponse<T>(
    IEnumerable<T> Data,
    int Page,
    int PageSize,
    int TotalCount,
    int TotalPages
);

public record SearchRequest(
    string? Query,
    int Page = 1,
    int PageSize = 12
);

public record ShippingMethodDto(
    ShippingMethod Method,
    string LabelFr,
    string LabelEn,
    decimal Cost,
    string EstimatedDeliveryFr,
    string EstimatedDeliveryEn
);

public record HealthResponse(
    string Status,
    string Version,
    DateTime Timestamp
);
