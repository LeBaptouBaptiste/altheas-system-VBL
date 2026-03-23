using API_Althea_systems.Models.Products;
using API_Althea_systems.Models.Shared;

namespace API_Althea_systems.Services.IServices;

public interface IProductService
{
    Task<ProductDto> GetByIdAsync(Guid id);
    Task<ProductDto> GetBySlugAsync(string slug);
    Task<PaginatedResponse<ProductDto>> GetAllAsync(int page, int pageSize, Guid? categoryId = null);
    Task<PaginatedResponse<ProductDto>> SearchAsync(string query, int page, int pageSize);
    Task<ProductDto> CreateAsync(ProductCreateRequest request);
    Task<ProductDto> UpdateAsync(Guid id, ProductUpdateRequest request);
    Task DeleteAsync(Guid id);
}
