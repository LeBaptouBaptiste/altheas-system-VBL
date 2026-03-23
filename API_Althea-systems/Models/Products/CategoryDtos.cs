namespace API_Althea_systems.Models.Products;

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
