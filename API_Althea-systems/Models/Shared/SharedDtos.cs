using API_Althea_systems.Common.Enums;

namespace API_Althea_systems.Models.Shared;

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
