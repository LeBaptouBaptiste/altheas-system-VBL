namespace API_Althea_systems.Models.Content;

// MS / AR translation fields are nullable on all content DTOs: the front
// localises with a FR fallback when a locale isn't filled in.

public record HeroSlideDto(
    Guid Id,
    string Image,
    string TitleFr,
    string TitleEn,
    string? TitleMs,
    string? TitleAr,
    string SubtitleFr,
    string SubtitleEn,
    string? SubtitleMs,
    string? SubtitleAr,
    string DescriptionFr,
    string DescriptionEn,
    string? DescriptionMs,
    string? DescriptionAr,
    string CtaFr,
    string CtaEn,
    string? CtaMs,
    string? CtaAr,
    string Link,
    int DisplayOrder,
    bool Active
);

public record HeroSlideCreateRequest(
    string Image,
    string TitleFr,
    string TitleEn,
    string? TitleMs,
    string? TitleAr,
    string SubtitleFr,
    string SubtitleEn,
    string? SubtitleMs,
    string? SubtitleAr,
    string DescriptionFr,
    string DescriptionEn,
    string? DescriptionMs,
    string? DescriptionAr,
    string CtaFr,
    string CtaEn,
    string? CtaMs,
    string? CtaAr,
    string Link,
    int DisplayOrder,
    bool Active
);

public record HeroSlideUpdateRequest(
    string? Image,
    string? TitleFr,
    string? TitleEn,
    string? TitleMs,
    string? TitleAr,
    string? SubtitleFr,
    string? SubtitleEn,
    string? SubtitleMs,
    string? SubtitleAr,
    string? DescriptionFr,
    string? DescriptionEn,
    string? DescriptionMs,
    string? DescriptionAr,
    string? CtaFr,
    string? CtaEn,
    string? CtaMs,
    string? CtaAr,
    string? Link,
    int? DisplayOrder,
    bool? Active
);

public record StaticPageDto(
    Guid Id,
    string Slug,
    string TitleFr,
    string TitleEn,
    string? TitleMs,
    string? TitleAr,
    string ContentFr,
    string ContentEn,
    string? ContentMs,
    string? ContentAr,
    DateTime UpdatedAt
);

public record StaticPageCreateRequest(
    string Slug,
    string TitleFr,
    string TitleEn,
    string? TitleMs,
    string? TitleAr,
    string ContentFr,
    string ContentEn,
    string? ContentMs,
    string? ContentAr
);

public record StaticPageUpdateRequest(
    string? Slug,
    string? TitleFr,
    string? TitleEn,
    string? TitleMs,
    string? TitleAr,
    string? ContentFr,
    string? ContentEn,
    string? ContentMs,
    string? ContentAr
);
