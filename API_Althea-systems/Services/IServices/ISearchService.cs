using API_Althea_systems.Models.Products;
using API_Althea_systems.Models.Shared;

namespace API_Althea_systems.Services.IServices;

public interface ISearchService
{
    Task<PaginatedResponse<ProductDto>> SearchAsync(string query, int page, int pageSize, int tolerance = 2);
}
