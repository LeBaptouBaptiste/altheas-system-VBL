using Microsoft.EntityFrameworkCore;
using API_Althea_systems.Data;
using API_Althea_systems.Models.Content;
using API_Althea_systems.Repositories.IRepositories;

namespace API_Althea_systems.Repositories;

public class ContentRepository : IContentRepository
{
    private readonly AltheaDbContext _context;

    public ContentRepository(AltheaDbContext context)
    {
        _context = context;
    }

    // ── Hero Slides ──────────────────────────────────────

    public async Task<HeroSlide?> GetHeroSlideByIdAsync(Guid id)
        => await _context.HeroSlides.FindAsync(id);

    public async Task<IEnumerable<HeroSlide>> GetAllHeroSlidesAsync(bool activeOnly = false)
    {
        var query = _context.HeroSlides.AsQueryable();
        if (activeOnly) query = query.Where(h => h.Active);
        return await query.OrderBy(h => h.DisplayOrder).ToListAsync();
    }

    public async Task<HeroSlide> CreateHeroSlideAsync(HeroSlide slide)
    {
        _context.HeroSlides.Add(slide);
        await _context.SaveChangesAsync();
        return slide;
    }

    public async Task UpdateHeroSlideAsync(HeroSlide slide)
    {
        slide.UpdatedAt = DateTime.UtcNow;
        _context.HeroSlides.Update(slide);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteHeroSlideAsync(HeroSlide slide)
    {
        _context.HeroSlides.Remove(slide);
        await _context.SaveChangesAsync();
    }

    // ── Static Pages ─────────────────────────────────────

    public async Task<StaticPage?> GetStaticPageByIdAsync(Guid id)
        => await _context.StaticPages.FindAsync(id);

    public async Task<StaticPage?> GetStaticPageBySlugAsync(string slug)
        => await _context.StaticPages.FirstOrDefaultAsync(p => p.Slug == slug);

    public async Task<IEnumerable<StaticPage>> GetAllStaticPagesAsync()
        => await _context.StaticPages.OrderBy(p => p.Slug).ToListAsync();

    public async Task<StaticPage> CreateStaticPageAsync(StaticPage page)
    {
        _context.StaticPages.Add(page);
        await _context.SaveChangesAsync();
        return page;
    }

    public async Task UpdateStaticPageAsync(StaticPage page)
    {
        page.UpdatedAt = DateTime.UtcNow;
        _context.StaticPages.Update(page);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteStaticPageAsync(StaticPage page)
    {
        _context.StaticPages.Remove(page);
        await _context.SaveChangesAsync();
    }
}
