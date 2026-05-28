using API_Althea_systems.Common.Exceptions;
using API_Althea_systems.Models.Content;
using API_Althea_systems.Repositories.IRepositories;
using API_Althea_systems.Services.IServices;

namespace API_Althea_systems.Services;

public class ContentService : IContentService
{
    private readonly IContentRepository _contentRepository;

    public ContentService(IContentRepository contentRepository)
    {
        _contentRepository = contentRepository;
    }

    // ── Hero Slides ──────────────────────────────────────

    public async Task<HeroSlideDto> GetHeroSlideByIdAsync(Guid id)
    {
        var slide = await _contentRepository.GetHeroSlideByIdAsync(id)
            ?? throw new NotFoundException("HeroSlide", id);
        return MapSlideDto(slide);
    }

    public async Task<IEnumerable<HeroSlideDto>> GetAllHeroSlidesAsync(bool activeOnly = false)
    {
        var slides = await _contentRepository.GetAllHeroSlidesAsync(activeOnly);
        return slides.Select(MapSlideDto);
    }

    public async Task<HeroSlideDto> CreateHeroSlideAsync(HeroSlideCreateRequest request)
    {
        var slide = new HeroSlide
        {
            Id = Guid.NewGuid(), Image = request.Image,
            TitleFr = request.TitleFr, TitleEn = request.TitleEn,
            TitleMs = request.TitleMs, TitleAr = request.TitleAr,
            SubtitleFr = request.SubtitleFr, SubtitleEn = request.SubtitleEn,
            SubtitleMs = request.SubtitleMs, SubtitleAr = request.SubtitleAr,
            DescriptionFr = request.DescriptionFr, DescriptionEn = request.DescriptionEn,
            DescriptionMs = request.DescriptionMs, DescriptionAr = request.DescriptionAr,
            CtaFr = request.CtaFr, CtaEn = request.CtaEn,
            CtaMs = request.CtaMs, CtaAr = request.CtaAr,
            Link = request.Link, DisplayOrder = request.DisplayOrder, Active = request.Active
        };
        await _contentRepository.CreateHeroSlideAsync(slide);
        return MapSlideDto(slide);
    }

    public async Task<HeroSlideDto> UpdateHeroSlideAsync(Guid id, HeroSlideUpdateRequest request)
    {
        var slide = await _contentRepository.GetHeroSlideByIdAsync(id)
            ?? throw new NotFoundException("HeroSlide", id);

        if (request.Image != null) slide.Image = request.Image;
        if (request.TitleFr != null) slide.TitleFr = request.TitleFr;
        if (request.TitleEn != null) slide.TitleEn = request.TitleEn;
        if (request.TitleMs != null) slide.TitleMs = request.TitleMs;
        if (request.TitleAr != null) slide.TitleAr = request.TitleAr;
        if (request.SubtitleFr != null) slide.SubtitleFr = request.SubtitleFr;
        if (request.SubtitleEn != null) slide.SubtitleEn = request.SubtitleEn;
        if (request.SubtitleMs != null) slide.SubtitleMs = request.SubtitleMs;
        if (request.SubtitleAr != null) slide.SubtitleAr = request.SubtitleAr;
        if (request.DescriptionFr != null) slide.DescriptionFr = request.DescriptionFr;
        if (request.DescriptionEn != null) slide.DescriptionEn = request.DescriptionEn;
        if (request.DescriptionMs != null) slide.DescriptionMs = request.DescriptionMs;
        if (request.DescriptionAr != null) slide.DescriptionAr = request.DescriptionAr;
        if (request.CtaFr != null) slide.CtaFr = request.CtaFr;
        if (request.CtaEn != null) slide.CtaEn = request.CtaEn;
        if (request.CtaMs != null) slide.CtaMs = request.CtaMs;
        if (request.CtaAr != null) slide.CtaAr = request.CtaAr;
        if (request.Link != null) slide.Link = request.Link;
        if (request.DisplayOrder.HasValue) slide.DisplayOrder = request.DisplayOrder.Value;
        if (request.Active.HasValue) slide.Active = request.Active.Value;

        await _contentRepository.UpdateHeroSlideAsync(slide);
        return MapSlideDto(slide);
    }

    public async Task DeleteHeroSlideAsync(Guid id)
    {
        var slide = await _contentRepository.GetHeroSlideByIdAsync(id)
            ?? throw new NotFoundException("HeroSlide", id);
        await _contentRepository.DeleteHeroSlideAsync(slide);
    }

    // ── Static Pages ─────────────────────────────────────

    public async Task<StaticPageDto> GetStaticPageByIdAsync(Guid id)
    {
        var page = await _contentRepository.GetStaticPageByIdAsync(id)
            ?? throw new NotFoundException("StaticPage", id);
        return MapPageDto(page);
    }

    public async Task<StaticPageDto> GetStaticPageBySlugAsync(string slug)
    {
        var page = await _contentRepository.GetStaticPageBySlugAsync(slug)
            ?? throw new NotFoundException($"StaticPage with slug '{slug}' was not found.");
        return MapPageDto(page);
    }

    public async Task<IEnumerable<StaticPageDto>> GetAllStaticPagesAsync()
    {
        var pages = await _contentRepository.GetAllStaticPagesAsync();
        return pages.Select(MapPageDto);
    }

    public async Task<StaticPageDto> CreateStaticPageAsync(StaticPageCreateRequest request)
    {
        var page = new StaticPage
        {
            Id = Guid.NewGuid(), Slug = request.Slug,
            TitleFr = request.TitleFr, TitleEn = request.TitleEn,
            TitleMs = request.TitleMs, TitleAr = request.TitleAr,
            ContentFr = request.ContentFr, ContentEn = request.ContentEn,
            ContentMs = request.ContentMs, ContentAr = request.ContentAr,
        };
        await _contentRepository.CreateStaticPageAsync(page);
        return MapPageDto(page);
    }

    public async Task<StaticPageDto> UpdateStaticPageAsync(Guid id, StaticPageUpdateRequest request)
    {
        var page = await _contentRepository.GetStaticPageByIdAsync(id)
            ?? throw new NotFoundException("StaticPage", id);

        if (request.Slug != null) page.Slug = request.Slug;
        if (request.TitleFr != null) page.TitleFr = request.TitleFr;
        if (request.TitleEn != null) page.TitleEn = request.TitleEn;
        if (request.TitleMs != null) page.TitleMs = request.TitleMs;
        if (request.TitleAr != null) page.TitleAr = request.TitleAr;
        if (request.ContentFr != null) page.ContentFr = request.ContentFr;
        if (request.ContentEn != null) page.ContentEn = request.ContentEn;
        if (request.ContentMs != null) page.ContentMs = request.ContentMs;
        if (request.ContentAr != null) page.ContentAr = request.ContentAr;

        await _contentRepository.UpdateStaticPageAsync(page);
        return MapPageDto(page);
    }

    public async Task DeleteStaticPageAsync(Guid id)
    {
        var page = await _contentRepository.GetStaticPageByIdAsync(id)
            ?? throw new NotFoundException("StaticPage", id);
        await _contentRepository.DeleteStaticPageAsync(page);
    }

    private static HeroSlideDto MapSlideDto(HeroSlide h) => new(
        h.Id, h.Image,
        h.TitleFr, h.TitleEn, h.TitleMs, h.TitleAr,
        h.SubtitleFr, h.SubtitleEn, h.SubtitleMs, h.SubtitleAr,
        h.DescriptionFr, h.DescriptionEn, h.DescriptionMs, h.DescriptionAr,
        h.CtaFr, h.CtaEn, h.CtaMs, h.CtaAr,
        h.Link, h.DisplayOrder, h.Active);

    private static StaticPageDto MapPageDto(StaticPage p) => new(
        p.Id, p.Slug,
        p.TitleFr, p.TitleEn, p.TitleMs, p.TitleAr,
        p.ContentFr, p.ContentEn, p.ContentMs, p.ContentAr,
        p.UpdatedAt);
}
