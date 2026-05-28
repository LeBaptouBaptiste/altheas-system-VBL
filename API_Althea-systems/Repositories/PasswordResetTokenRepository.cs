using API_Althea_systems.Data;
using API_Althea_systems.Models.Users;
using API_Althea_systems.Repositories.IRepositories;
using Microsoft.EntityFrameworkCore;

namespace API_Althea_systems.Repositories;

public class PasswordResetTokenRepository : IPasswordResetTokenRepository
{
    private readonly AltheaDbContext _context;

    public PasswordResetTokenRepository(AltheaDbContext context)
    {
        _context = context;
    }

    public async Task CreateAsync(PasswordResetToken token)
    {
        _context.PasswordResetTokens.Add(token);
        await _context.SaveChangesAsync();
    }

    public async Task<PasswordResetToken?> GetAnyByHashAsync(string tokenHash)
    {
        return await _context.PasswordResetTokens
            .Include(t => t.User)
            .FirstOrDefaultAsync(t => t.TokenHash == tokenHash);
    }

    public async Task UpdateAsync(PasswordResetToken token)
    {
        _context.PasswordResetTokens.Update(token);
        await _context.SaveChangesAsync();
    }

    public async Task<DateTime?> GetMostRecentCreatedAtAsync(Guid userId)
    {
        return await _context.PasswordResetTokens
            .Where(t => t.UserId == userId)
            .OrderByDescending(t => t.CreatedAt)
            .Select(t => (DateTime?)t.CreatedAt)
            .FirstOrDefaultAsync();
    }
}
