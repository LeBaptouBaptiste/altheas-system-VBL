using Microsoft.EntityFrameworkCore;
using API_Althea_systems.Data;
using API_Althea_systems.Models.Users;
using API_Althea_systems.Repositories.IRepositories;

namespace API_Althea_systems.Repositories;

public class RecoveryCodeRepository : IRecoveryCodeRepository
{
    private readonly AltheaDbContext _context;

    public RecoveryCodeRepository(AltheaDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<UserRecoveryCode>> GetUnusedAsync(Guid userId)
    {
        return await _context.UserRecoveryCodes
            .Where(c => c.UserId == userId && c.UsedAt == null)
            .ToListAsync();
    }

    public Task<int> CountUnusedAsync(Guid userId)
    {
        return _context.UserRecoveryCodes
            .CountAsync(c => c.UserId == userId && c.UsedAt == null);
    }

    public async Task AddRangeAsync(IEnumerable<UserRecoveryCode> codes)
    {
        _context.UserRecoveryCodes.AddRange(codes);
        await _context.SaveChangesAsync();
    }

    public async Task MarkUsedAsync(UserRecoveryCode code)
    {
        code.UsedAt = DateTime.UtcNow;
        _context.UserRecoveryCodes.Update(code);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAllForUserAsync(Guid userId)
    {
        await _context.UserRecoveryCodes
            .Where(c => c.UserId == userId)
            .ExecuteDeleteAsync();
    }
}
