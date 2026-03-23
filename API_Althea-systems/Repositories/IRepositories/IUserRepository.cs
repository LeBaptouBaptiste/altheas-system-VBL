using API_Althea_systems.Models.Users;

namespace API_Althea_systems.Repositories.IRepositories;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(Guid id);
    Task<User?> GetByEmailAsync(string email);
    Task<bool> EmailExistsAsync(string email);
    Task<User> CreateAsync(User user);
    Task UpdateAsync(User user);
    Task<IEnumerable<User>> GetAllAsync(int page, int pageSize);
    Task<int> CountAsync();
}
