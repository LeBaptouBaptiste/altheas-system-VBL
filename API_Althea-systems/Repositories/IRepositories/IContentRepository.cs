using API_Althea_systems.Models.Content;

namespace API_Althea_systems.Repositories.IRepositories;

public interface IContentRepository
{
    // Hero Slides
    Task<HeroSlide?> GetHeroSlideByIdAsync(Guid id);
    Task<IEnumerable<HeroSlide>> GetAllHeroSlidesAsync(bool activeOnly = false);
    Task<HeroSlide> CreateHeroSlideAsync(HeroSlide slide);
    Task UpdateHeroSlideAsync(HeroSlide slide);
    Task DeleteHeroSlideAsync(HeroSlide slide);

    // Static Pages
    Task<StaticPage?> GetStaticPageByIdAsync(Guid id);
    Task<StaticPage?> GetStaticPageBySlugAsync(string slug);
    Task<IEnumerable<StaticPage>> GetAllStaticPagesAsync();
    Task<StaticPage> CreateStaticPageAsync(StaticPage page);
    Task UpdateStaticPageAsync(StaticPage page);
    Task DeleteStaticPageAsync(StaticPage page);
}
