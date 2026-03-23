using API_Althea_systems.Models.Products;

namespace API_Althea_systems.Repositories.IRepositories;

public interface IProductRepository
{
    Task<Product?> GetByIdAsync(Guid id);
    Task<Product?> GetBySlugAsync(string slug);
    Task<IEnumerable<Product>> GetAllAsync(int page, int pageSize, Guid? categoryId = null);
    Task<IEnumerable<Product>> SearchAsync(string query, int page, int pageSize);
    Task<int> CountAsync(Guid? categoryId = null);
    Task<int> SearchCountAsync(string query);
    Task<Product> CreateAsync(Product product);
    Task UpdateAsync(Product product);
    Task DeleteAsync(Product product);
}
