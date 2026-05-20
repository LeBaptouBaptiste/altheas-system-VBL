using API_Althea_systems.Models.Users;

namespace API_Althea_systems.Repositories.IRepositories;

public interface IPasswordResetTokenRepository
{
    Task CreateAsync(PasswordResetToken token);

    /// <summary>
    /// Looks up any row with this hash, regardless of state. The caller
    /// (AuthService.ResetPasswordAsync) inspects ConsumedAt / ExpiresAt to
    /// pick the right error message — same pattern as
    /// <see cref="IEmailConfirmationTokenRepository"/>.
    /// </summary>
    Task<PasswordResetToken?> GetAnyByHashAsync(string tokenHash);

    Task UpdateAsync(PasswordResetToken token);

    /// <summary>
    /// Returns the CreatedAt of the most recent token issued for this user,
    /// or null. Used to throttle /forgot-password (one mail per 5 min).
    /// </summary>
    Task<DateTime?> GetMostRecentCreatedAtAsync(Guid userId);
}
