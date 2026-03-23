using API_Althea_systems.Models.Content;

namespace API_Althea_systems.Services.IServices;

public interface IContentService
{
    // Hero Slides
    Task<HeroSlideDto> GetHeroSlideByIdAsync(Guid id);
    Task<IEnumerable<HeroSlideDto>> GetAllHeroSlidesAsync(bool activeOnly = false);
    Task<HeroSlideDto> CreateHeroSlideAsync(HeroSlideCreateRequest request);
    Task<HeroSlideDto> UpdateHeroSlideAsync(Guid id, HeroSlideUpdateRequest request);
    Task DeleteHeroSlideAsync(Guid id);

    // Static Pages
    Task<StaticPageDto> GetStaticPageByIdAsync(Guid id);
    Task<StaticPageDto> GetStaticPageBySlugAsync(string slug);
    Task<IEnumerable<StaticPageDto>> GetAllStaticPagesAsync();
    Task<StaticPageDto> CreateStaticPageAsync(StaticPageCreateRequest request);
    Task<StaticPageDto> UpdateStaticPageAsync(Guid id, StaticPageUpdateRequest request);
    Task DeleteStaticPageAsync(Guid id);
}
