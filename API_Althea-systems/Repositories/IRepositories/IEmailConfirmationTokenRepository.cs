using API_Althea_systems.Models.Users;

namespace API_Althea_systems.Repositories.IRepositories;

public interface IEmailConfirmationTokenRepository
{
    Task CreateAsync(EmailConfirmationToken token);

    /// <summary>
    /// Looks up an unconsumed, unexpired token by its hash. Returns null if
    /// no row matches, OR if the row is expired / already consumed — callers
    /// don't need to revalidate.
    /// </summary>
    Task<EmailConfirmationToken?> GetActiveByHashAsync(string tokenHash);

    /// <summary>
    /// Looks up any row with this hash, regardless of state. Used so the
    /// caller can distinguish "wrong token" (null) from "already used /
    /// expired" (row found, ConsumedAt set / ExpiresAt past) and surface a
    /// useful error.
    /// </summary>
    Task<EmailConfirmationToken?> GetAnyByHashAsync(string tokenHash);

    Task UpdateAsync(EmailConfirmationToken token);

    /// <summary>
    /// Returns the CreatedAt of the most recent token issued for this user,
    /// or null if none. Used to throttle the resend endpoint
    /// (one mail per 5 min per user).
    /// </summary>
    Task<DateTime?> GetMostRecentCreatedAtAsync(Guid userId);
}
