using Microsoft.EntityFrameworkCore;
using API_Althea_systems.Data;
using API_Althea_systems.Models.Users;
using API_Althea_systems.Repositories.IRepositories;

namespace API_Althea_systems.Repositories;

public class UserRepository : IUserRepository
{
    private readonly AltheaDbContext _context;

    public UserRepository(AltheaDbContext context)
    {
        _context = context;
    }

    public async Task<User?> GetByIdAsync(Guid id)
    {
        return await _context.Users
            .Include(u => u.Addresses)
            .Include(u => u.PaymentMethods)
            .FirstOrDefaultAsync(u => u.Id == id);
    }

    public async Task<User?> GetByEmailAsync(string email)
    {
        return await _context.Users
            .Include(u => u.Addresses)
            .Include(u => u.PaymentMethods)
            .FirstOrDefaultAsync(u => u.Email == email);
    }

    public async Task<bool> EmailExistsAsync(string email)
    {
        return await _context.Users.AnyAsync(u => u.Email == email);
    }

    public async Task<User?> GetByStripeCustomerIdAsync(string stripeCustomerId)
    {
        // Include PaymentMethods because the main caller (Stripe webhook)
        // needs to dedupe attached methods before INSERTing — avoids a
        // second roundtrip just to fetch them.
        return await _context.Users
            .Include(u => u.PaymentMethods)
            .FirstOrDefaultAsync(u => u.StripeCustomerId == stripeCustomerId);
    }

    public async Task<User> CreateAsync(User user)
    {
        _context.Users.Add(user);
        await _context.SaveChangesAsync();
        return user;
    }

    public async Task UpdateAsync(User user)
    {
        user.UpdatedAt = DateTime.UtcNow;
        _context.Users.Update(user);
        await _context.SaveChangesAsync();
    }

    public async Task<IEnumerable<User>> GetAllAsync(int page, int pageSize)
    {
        return await _context.Users
            .Include(u => u.Addresses)
            .Include(u => u.PaymentMethods)
            .OrderByDescending(u => u.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<int> CountAsync()
    {
        return await _context.Users.CountAsync();
    }
}
