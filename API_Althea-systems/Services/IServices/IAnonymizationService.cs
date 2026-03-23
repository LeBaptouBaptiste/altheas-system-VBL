using API_Althea_systems.Models.Users;

namespace API_Althea_systems.Services.IServices;

public interface IAnonymizationService
{
    Task<UserDto> AnonymizeUserAsync(Guid userId);
    bool IsAnonymized(User user);
}
