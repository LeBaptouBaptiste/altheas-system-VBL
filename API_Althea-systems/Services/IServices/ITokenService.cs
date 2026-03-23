using API_Althea_systems.Models.Users;

namespace API_Althea_systems.Services.IServices;

public interface ITokenService
{
    string GenerateAccessToken(User user);
    string GenerateRefreshToken();
}
