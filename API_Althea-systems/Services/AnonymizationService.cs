using API_Althea_systems.Common.Enums;
using API_Althea_systems.Common.Exceptions;
using API_Althea_systems.Models.Users;
using API_Althea_systems.Repositories.IRepositories;
using API_Althea_systems.Services.IServices;

namespace API_Althea_systems.Services;

public class AnonymizationService : IAnonymizationService
{
    private readonly IUserRepository _userRepository;

    public AnonymizationService(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<UserDto> AnonymizeUserAsync(Guid userId)
    {
        var user = await _userRepository.GetByIdAsync(userId)
            ?? throw new NotFoundException("User", userId);

        if (user.Anonymized)
            throw new ConflictException("User is already anonymized.");

        // RGPD-compliant anonymization
        var anonymizedId = user.Id.ToString()[..8];
        user.Name = "Utilisateur Anonymisé";
        user.Email = $"anon-{anonymizedId}@deleted.local";
        user.PasswordHash = string.Empty;
        user.Anonymized = true;
        user.Status = UserStatus.Inactive;
        user.TwoFactorSecret = null;
        user.TwoFactorEnabled = false;

        // Clear all personal data collections
        user.Addresses.Clear();
        user.PaymentMethods.Clear();

        await _userRepository.UpdateAsync(user);

        return new UserDto(
            user.Id, user.Name, user.Email, user.Role, user.Status,
            user.Anonymized, user.EmailConfirmed, user.TwoFactorEnabled,
            user.LastLogin, user.CreatedAt, [], []
        );
    }

    public bool IsAnonymized(User user) => user.Anonymized;
}
