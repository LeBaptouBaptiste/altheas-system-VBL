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
        // INTENTIONALLY no `_context.Users.Update(user)` here.
        //
        // All callers obtain `user` via GetByIdAsync / GetByEmailAsync /
        // GetByStripeCustomerIdAsync — meaning the entity is ALREADY tracked
        // by this DbContext. Calling Update() at this point cascades the
        // Modified state to every navigation child, including newly Added
        // ones (e.g. user.Addresses.Add(new Address { Id = NewGuid() })).
        // EF then tries to UPDATE WHERE Id = <new-guid-not-yet-in-DB>, the
        // statement affects 0 rows, and we get a DbUpdateConcurrencyException.
        //
        // The change tracker already does the right thing: SaveChanges
        // detects modified properties on `user`, INSERTs newly-added
        // navigation entities, DELETEs removed ones.
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
