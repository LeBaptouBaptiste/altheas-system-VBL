using Microsoft.EntityFrameworkCore;
using API_Althea_systems.Data;
using API_Althea_systems.Models.Products;
using API_Althea_systems.Repositories.IRepositories;

namespace API_Althea_systems.Repositories;

public class ProductRepository : IProductRepository
{
    private readonly AltheaDbContext _context;

    public ProductRepository(AltheaDbContext context)
    {
        _context = context;
    }

    public async Task<Product?> GetByIdAsync(Guid id)
    {
        return await _context.Products
            .Include(p => p.ProductCategories).ThenInclude(pc => pc.Category)
            .Include(p => p.Specs)
            .FirstOrDefaultAsync(p => p.Id == id);
    }

    public async Task<Product?> GetBySlugAsync(string slug)
    {
        return await _context.Products
            .Include(p => p.ProductCategories).ThenInclude(pc => pc.Category)
            .Include(p => p.Specs)
            .FirstOrDefaultAsync(p => p.Slug == slug);
    }

    public async Task<IEnumerable<Product>> GetAllAsync(int page, int pageSize, Guid? categoryId = null)
    {
        var query = _context.Products
            .Include(p => p.ProductCategories).ThenInclude(pc => pc.Category)
            .Include(p => p.Specs)
            .AsQueryable();

        if (categoryId.HasValue)
            query = query.Where(p => p.ProductCategories.Any(pc => pc.CategoryId == categoryId.Value));

        return await query
            .OrderByDescending(p => p.PriorityRank > 0)
            .ThenBy(p => p.PriorityRank)
            .ThenByDescending(p => p.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<IEnumerable<Product>> SearchAsync(string query, int page, int pageSize)
    {
        var lowerQuery = query.ToLowerInvariant();
        return await _context.Products
            .Include(p => p.ProductCategories).ThenInclude(pc => pc.Category)
            .Include(p => p.Specs)
            .Where(p => p.NameFr.ToLower().Contains(lowerQuery)
                     || p.NameEn.ToLower().Contains(lowerQuery)
                     || p.DescriptionFr.ToLower().Contains(lowerQuery)
                     || p.DescriptionEn.ToLower().Contains(lowerQuery))
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<int> CountAsync(Guid? categoryId = null)
    {
        var query = _context.Products.AsQueryable();
        if (categoryId.HasValue)
            query = query.Where(p => p.ProductCategories.Any(pc => pc.CategoryId == categoryId.Value));
        return await query.CountAsync();
    }

    public async Task<int> SearchCountAsync(string query)
    {
        var lowerQuery = query.ToLowerInvariant();
        return await _context.Products
            .CountAsync(p => p.NameFr.ToLower().Contains(lowerQuery)
                          || p.NameEn.ToLower().Contains(lowerQuery)
                          || p.DescriptionFr.ToLower().Contains(lowerQuery)
                          || p.DescriptionEn.ToLower().Contains(lowerQuery));
    }

    public async Task<Product> CreateAsync(Product product)
    {
        _context.Products.Add(product);
        await _context.SaveChangesAsync();
        return product;
    }

    public async Task UpdateAsync(Product product)
    {
        product.UpdatedAt = DateTime.UtcNow;
        _context.Products.Update(product);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(Product product)
    {
        _context.Products.Remove(product);
        await _context.SaveChangesAsync();
    }
}
