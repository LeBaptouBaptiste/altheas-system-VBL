using Microsoft.EntityFrameworkCore;
using API_Althea_systems.Data;
using API_Althea_systems.Models.Products;
using API_Althea_systems.Models.Shared;
using API_Althea_systems.Services.IServices;

namespace API_Althea_systems.Services;

public class SearchService : ISearchService
{
    private readonly AltheaDbContext _context;

    public SearchService(AltheaDbContext context)
    {
        _context = context;
    }

    public async Task<PaginatedResponse<ProductDto>> SearchAsync(string query, int page, int pageSize, int tolerance = 2)
    {
        if (string.IsNullOrWhiteSpace(query))
            return new PaginatedResponse<ProductDto>([], page, pageSize, 0, 0);

        var lowerQuery = query.ToLowerInvariant().Trim();

        // Load all active products (for Levenshtein — can't be done in SQL)
        var allProducts = await _context.Products
            .Include(p => p.ProductCategories).ThenInclude(pc => pc.Category)
            .Include(p => p.Specs)
            .Where(p => p.Status == Common.Enums.ProductStatus.Active)
            .ToListAsync();

        // Score each product
        var scored = allProducts
            .Select(p => new
            {
                Product = p,
                Priority = GetMatchPriority(p, lowerQuery, tolerance)
            })
            .Where(x => x.Priority > 0)
            .OrderByDescending(x => x.Priority)
            .ThenByDescending(x => x.Product.PriorityRank > 0)
            .ThenBy(x => x.Product.PriorityRank)
            .ToList();

        var total = scored.Count;
        var paged = scored
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => MapToDto(x.Product));

        return new PaginatedResponse<ProductDto>(
            paged, page, pageSize, total, (int)Math.Ceiling(total / (double)pageSize));
    }

    private static int GetMatchPriority(Product product, string query, int tolerance)
    {
        var nameFr = product.NameFr.ToLowerInvariant();
        var nameEn = product.NameEn.ToLowerInvariant();
        var descFr = product.DescriptionFr.ToLowerInvariant();
        var descEn = product.DescriptionEn.ToLowerInvariant();

        // Priority 4: exact match in name
        if (nameFr.Contains(query) || nameEn.Contains(query))
            return 4;

        // Priority 3: exact match in description
        if (descFr.Contains(query) || descEn.Contains(query))
            return 3;

        // Priority 2: word-level Levenshtein match in name
        var nameWords = $"{nameFr} {nameEn}".Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (nameWords.Any(word => LevenshteinDistance(word, query) <= tolerance))
            return 2;

        // Priority 1: word-level Levenshtein match in description
        var descWords = $"{descFr} {descEn}".Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (descWords.Any(word => LevenshteinDistance(word, query) <= tolerance))
            return 1;

        return 0;
    }

    /// <summary>
    /// Calculates the Levenshtein distance between two strings.
    /// </summary>
    private static int LevenshteinDistance(string a, string b)
    {
        if (string.IsNullOrEmpty(a)) return b?.Length ?? 0;
        if (string.IsNullOrEmpty(b)) return a.Length;

        var lenA = a.Length;
        var lenB = b.Length;
        var matrix = new int[lenA + 1, lenB + 1];

        for (var i = 0; i <= lenA; i++) matrix[i, 0] = i;
        for (var j = 0; j <= lenB; j++) matrix[0, j] = j;

        for (var i = 1; i <= lenA; i++)
        {
            for (var j = 1; j <= lenB; j++)
            {
                var cost = a[i - 1] == b[j - 1] ? 0 : 1;
                matrix[i, j] = Math.Min(
                    Math.Min(matrix[i - 1, j] + 1, matrix[i, j - 1] + 1),
                    matrix[i - 1, j - 1] + cost
                );
            }
        }

        return matrix[lenA, lenB];
    }

    private static ProductDto MapToDto(Product p) => new(
        p.Id, p.Slug,
        p.NameFr, p.NameEn, p.NameMs, p.NameAr,
        p.DescriptionFr, p.DescriptionEn, p.DescriptionMs, p.DescriptionAr,
        p.LongDescriptionFr, p.LongDescriptionEn, p.LongDescriptionMs, p.LongDescriptionAr,
        p.PriceHT, p.VatRate,
        p.StockQty, p.StockStatus, p.IsNew, p.PriorityRank, p.Images, p.Status,
        p.CreatedAt, p.UpdatedAt,
        p.ProductCategories.Select(pc => new CategorySummaryDto(pc.Category.Id, pc.Category.Slug, pc.Category.NameFr, pc.Category.NameEn)),
        p.Specs.Select(s => new ProductSpecDto(
            s.Label, s.Value,
            s.LabelEn, s.LabelMs, s.LabelAr,
            s.ValueEn, s.ValueMs, s.ValueAr))
    );
}
