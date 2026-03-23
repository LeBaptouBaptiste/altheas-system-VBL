namespace API_Althea_systems.Models.Content;

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
