using API_Althea_systems.Common.Exceptions;
using API_Althea_systems.Models.Products;
using API_Althea_systems.Repositories.IRepositories;
using API_Althea_systems.Services.IServices;

namespace API_Althea_systems.Services;

public class CategoryService : ICategoryService
{
    private readonly ICategoryRepository _categoryRepository;

    public CategoryService(ICategoryRepository categoryRepository)
    {
        _categoryRepository = categoryRepository;
    }

    public async Task<CategoryDto> GetByIdAsync(Guid id)
    {
        var category = await _categoryRepository.GetByIdAsync(id)
            ?? throw new NotFoundException("Category", id);
        var count = await _categoryRepository.GetProductCountAsync(id);
        return MapToDto(category, count);
    }

    public async Task<CategoryDto> GetBySlugAsync(string slug)
    {
        var category = await _categoryRepository.GetBySlugAsync(slug)
            ?? throw new NotFoundException($"Category with slug '{slug}' was not found.");
        var count = await _categoryRepository.GetProductCountAsync(category.Id);
        return MapToDto(category, count);
    }

    public async Task<IEnumerable<CategoryDto>> GetAllAsync()
    {
        var categories = await _categoryRepository.GetAllAsync();
        var result = new List<CategoryDto>();
        foreach (var c in categories)
        {
            var count = await _categoryRepository.GetProductCountAsync(c.Id);
            result.Add(MapToDto(c, count));
        }
        return result;
    }

    public async Task<CategoryDto> CreateAsync(CategoryCreateRequest request)
    {
        var category = new Category
        {
            Id = Guid.NewGuid(),
            Slug = request.Slug,
            NameFr = request.NameFr,
            NameEn = request.NameEn,
            NameMs = request.NameMs,
            NameAr = request.NameAr,
            DescriptionFr = request.DescriptionFr,
            DescriptionEn = request.DescriptionEn,
            DescriptionMs = request.DescriptionMs,
            DescriptionAr = request.DescriptionAr,
            Image = request.Image,
            ParentId = request.ParentId,
            DisplayOrder = request.DisplayOrder,
            Active = request.Active
        };

        await _categoryRepository.CreateAsync(category);
        return await GetByIdAsync(category.Id);
    }

    public async Task<CategoryDto> UpdateAsync(Guid id, CategoryUpdateRequest request)
    {
        var category = await _categoryRepository.GetByIdAsync(id)
            ?? throw new NotFoundException("Category", id);

        if (request.Slug != null) category.Slug = request.Slug;
        if (request.NameFr != null) category.NameFr = request.NameFr;
        if (request.NameEn != null) category.NameEn = request.NameEn;
        if (request.NameMs != null) category.NameMs = request.NameMs;
        if (request.NameAr != null) category.NameAr = request.NameAr;
        if (request.DescriptionFr != null) category.DescriptionFr = request.DescriptionFr;
        if (request.DescriptionEn != null) category.DescriptionEn = request.DescriptionEn;
        if (request.DescriptionMs != null) category.DescriptionMs = request.DescriptionMs;
        if (request.DescriptionAr != null) category.DescriptionAr = request.DescriptionAr;
        if (request.Image != null) category.Image = request.Image;
        if (request.ParentId != null) category.ParentId = request.ParentId;
        if (request.DisplayOrder.HasValue) category.DisplayOrder = request.DisplayOrder.Value;
        if (request.Active.HasValue) category.Active = request.Active.Value;

        await _categoryRepository.UpdateAsync(category);
        return await GetByIdAsync(category.Id);
    }

    public async Task DeleteAsync(Guid id)
    {
        var category = await _categoryRepository.GetByIdAsync(id)
            ?? throw new NotFoundException("Category", id);
        await _categoryRepository.DeleteAsync(category);
    }

    private static CategoryDto MapToDto(Category c, int productCount) => new(
        c.Id, c.Slug,
        c.NameFr, c.NameEn, c.NameMs, c.NameAr,
        c.DescriptionFr, c.DescriptionEn, c.DescriptionMs, c.DescriptionAr,
        c.Image, c.ParentId, c.DisplayOrder, c.Active, productCount);
}
