using API_Althea_systems.Models.Products;

namespace API_Althea_systems.Repositories.IRepositories;

public interface ICategoryRepository
{
    Task<Category?> GetByIdAsync(Guid id);
    Task<Category?> GetBySlugAsync(string slug);
    Task<IEnumerable<Category>> GetAllAsync();
    Task<Category> CreateAsync(Category category);
    Task UpdateAsync(Category category);
    Task DeleteAsync(Category category);
    Task<int> GetProductCountAsync(Guid categoryId);
}
