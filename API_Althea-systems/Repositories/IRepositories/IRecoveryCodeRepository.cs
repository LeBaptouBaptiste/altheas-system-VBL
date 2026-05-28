using API_Althea_systems.Models.Users;

namespace API_Althea_systems.Repositories.IRepositories;

public interface IRecoveryCodeRepository
{
    /// <summary>Returns the unused recovery codes for the user (UsedAt IS NULL).</summary>
    Task<IReadOnlyList<UserRecoveryCode>> GetUnusedAsync(Guid userId);

    /// <summary>Counts unused recovery codes (cheap version of <see cref="GetUnusedAsync"/>).</summary>
    Task<int> CountUnusedAsync(Guid userId);

    /// <summary>Inserts a batch of fresh codes (already hashed by the caller).</summary>
    Task AddRangeAsync(IEnumerable<UserRecoveryCode> codes);

    /// <summary>Marks a single code as consumed (sets UsedAt = now).</summary>
    Task MarkUsedAsync(UserRecoveryCode code);

    /// <summary>Deletes ALL recovery codes for the user — used on disable / regenerate.</summary>
    Task DeleteAllForUserAsync(Guid userId);
}
