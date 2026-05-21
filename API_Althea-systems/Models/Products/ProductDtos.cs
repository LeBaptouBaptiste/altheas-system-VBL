using API_Althea_systems.Common.Enums;

namespace API_Althea_systems.Models.Products;

// Translation fields (NameMs/Ar, DescriptionMs/Ar, LongDescriptionMs/Ar and
// the equivalents on ProductSpec) are nullable: the front falls back to the
// French canonical value when a locale isn't filled in. This keeps the API
// surface tolerant to partially-translated catalogs.

public record ProductDto(
    Guid Id,
    string Slug,
    string NameFr,
    string NameEn,
    string? NameMs,
    string? NameAr,
    string DescriptionFr,
    string DescriptionEn,
    string? DescriptionMs,
    string? DescriptionAr,
    string LongDescriptionFr,
    string LongDescriptionEn,
    string? LongDescriptionMs,
    string? LongDescriptionAr,
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
    string? NameMs,
    string? NameAr,
    string DescriptionFr,
    string DescriptionEn,
    string? DescriptionMs,
    string? DescriptionAr,
    string LongDescriptionFr,
    string LongDescriptionEn,
    string? LongDescriptionMs,
    string? LongDescriptionAr,
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
    string? NameMs,
    string? NameAr,
    string? DescriptionFr,
    string? DescriptionEn,
    string? DescriptionMs,
    string? DescriptionAr,
    string? LongDescriptionFr,
    string? LongDescriptionEn,
    string? LongDescriptionMs,
    string? LongDescriptionAr,
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

public record ProductSpecDto(
    string Label,
    string Value,
    string? LabelEn = null,
    string? LabelMs = null,
    string? LabelAr = null,
    string? ValueEn = null,
    string? ValueMs = null,
    string? ValueAr = null);

public record ProductSpecRequest(
    string Label,
    string Value,
    string? LabelEn = null,
    string? LabelMs = null,
    string? LabelAr = null,
    string? ValueEn = null,
    string? ValueMs = null,
    string? ValueAr = null);
