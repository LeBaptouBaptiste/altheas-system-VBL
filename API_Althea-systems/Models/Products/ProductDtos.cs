using API_Althea_systems.Common.Enums;

namespace API_Althea_systems.Models.Products;

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
