using API_Althea_systems.Models.Users;

namespace API_Althea_systems.Services.IServices;

public interface IAuthService
{
    Task<AuthResponse> RegisterAsync(RegisterRequest request);
    Task<AuthResponse> LoginAsync(LoginRequest request);
    Task<UserDto> GetCurrentUserAsync(Guid userId);
    Task ConfirmEmailAsync(ConfirmEmailRequest request);
    Task ForgotPasswordAsync(ForgotPasswordRequest request);
    Task ResetPasswordAsync(ResetPasswordRequest request);
    Task Verify2FaAsync(Guid userId, Verify2FaRequest request);
}
