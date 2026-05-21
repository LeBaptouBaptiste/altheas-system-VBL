namespace API_Althea_systems.Models.Products;

// Translation fields (NameMs/Ar, DescriptionMs/Ar) are nullable: the front
// falls back to the French canonical value when a locale isn't filled in.

public record CategoryDto(
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
    string Image,
    Guid? ParentId,
    int DisplayOrder,
    bool Active,
    int ProductCount
);

// Slim DTO embedded in ProductDto.Categories. Doesn't carry description.
public record CategorySummaryDto(
    Guid Id,
    string Slug,
    string NameFr,
    string NameEn,
    string? NameMs = null,
    string? NameAr = null);

public record CategoryCreateRequest(
    string Slug,
    string NameFr,
    string NameEn,
    string? NameMs,
    string? NameAr,
    string DescriptionFr,
    string DescriptionEn,
    string? DescriptionMs,
    string? DescriptionAr,
    string Image,
    Guid? ParentId,
    int DisplayOrder,
    bool Active
);

public record CategoryUpdateRequest(
    string? Slug,
    string? NameFr,
    string? NameEn,
    string? NameMs,
    string? NameAr,
    string? DescriptionFr,
    string? DescriptionEn,
    string? DescriptionMs,
    string? DescriptionAr,
    string? Image,
    Guid? ParentId,
    int? DisplayOrder,
    bool? Active
);
