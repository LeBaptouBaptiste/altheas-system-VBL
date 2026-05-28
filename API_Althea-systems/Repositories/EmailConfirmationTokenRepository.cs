using API_Althea_systems.Data;
using API_Althea_systems.Models.Users;
using API_Althea_systems.Repositories.IRepositories;
using Microsoft.EntityFrameworkCore;

namespace API_Althea_systems.Repositories;

public class EmailConfirmationTokenRepository : IEmailConfirmationTokenRepository
{
    private readonly AltheaDbContext _context;

    public EmailConfirmationTokenRepository(AltheaDbContext context)
    {
        _context = context;
    }

    public async Task CreateAsync(EmailConfirmationToken token)
    {
        _context.EmailConfirmationTokens.Add(token);
        await _context.SaveChangesAsync();
    }

    public async Task<EmailConfirmationToken?> GetActiveByHashAsync(string tokenHash)
    {
        var now = DateTime.UtcNow;
        return await _context.EmailConfirmationTokens
            .Include(t => t.User)
            .Where(t => t.TokenHash == tokenHash
                     && t.ConsumedAt == null
                     && t.ExpiresAt > now)
            .FirstOrDefaultAsync();
    }

    public async Task<EmailConfirmationToken?> GetAnyByHashAsync(string tokenHash)
    {
        // No filter on state — caller (AuthService.ConfirmEmailAsync) inspects
        // ConsumedAt / ExpiresAt to pick the right error message.
        return await _context.EmailConfirmationTokens
            .Include(t => t.User)
            .FirstOrDefaultAsync(t => t.TokenHash == tokenHash);
    }

    public async Task UpdateAsync(EmailConfirmationToken token)
    {
        _context.EmailConfirmationTokens.Update(token);
        await _context.SaveChangesAsync();
    }

    public async Task<DateTime?> GetMostRecentCreatedAtAsync(Guid userId)
    {
        return await _context.EmailConfirmationTokens
            .Where(t => t.UserId == userId)
            .OrderByDescending(t => t.CreatedAt)
            .Select(t => (DateTime?)t.CreatedAt)
            .FirstOrDefaultAsync();
    }
}
