using API_Althea_systems.Models.Products;

namespace API_Althea_systems.Services.IServices;

public interface ICategoryService
{
    Task<CategoryDto> GetByIdAsync(Guid id);
    Task<CategoryDto> GetBySlugAsync(string slug);
    Task<IEnumerable<CategoryDto>> GetAllAsync();
    Task<CategoryDto> CreateAsync(CategoryCreateRequest request);
    Task<CategoryDto> UpdateAsync(Guid id, CategoryUpdateRequest request);
    Task DeleteAsync(Guid id);
}
