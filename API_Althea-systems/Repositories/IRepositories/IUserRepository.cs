using API_Althea_systems.Models.Users;

namespace API_Althea_systems.Repositories.IRepositories;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(Guid id);
    Task<User?> GetByEmailAsync(string email);
    Task<bool> EmailExistsAsync(string email);
    /// <summary>
    /// Looks up the user owning a Stripe Customer (cus_xxx). Used by the
    /// webhook handler to route inbound Stripe events to the right user
    /// without trusting client-supplied metadata.
    /// </summary>
    Task<User?> GetByStripeCustomerIdAsync(string stripeCustomerId);
    Task<User> CreateAsync(User user);
    Task UpdateAsync(User user);
    Task<IEnumerable<User>> GetAllAsync(int page, int pageSize);
    Task<int> CountAsync();
}
