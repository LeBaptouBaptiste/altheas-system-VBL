using API_Althea_systems.Common.Exceptions;
using API_Althea_systems.Models.Users;
using API_Althea_systems.Repositories.IRepositories;
using API_Althea_systems.Services.IServices;

namespace API_Althea_systems.Services;

public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly ITokenService _tokenService;

    public AuthService(IUserRepository userRepository, ITokenService tokenService)
    {
        _userRepository = userRepository;
        _tokenService = tokenService;
    }

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
    {
        if (await _userRepository.EmailExistsAsync(request.Email))
            throw new ConflictException("User", "email", request.Email);

        var user = new User
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            Email = request.Email.ToLowerInvariant(),
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            EmailConfirmed = false
        };

        await _userRepository.CreateAsync(user);

        var accessToken = _tokenService.GenerateAccessToken(user);
        var refreshToken = _tokenService.GenerateRefreshToken();

        return new AuthResponse(accessToken, refreshToken, MapToDto(user));
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request)
    {
        var user = await _userRepository.GetByEmailAsync(request.Email.ToLowerInvariant())
            ?? throw new UnauthorizedException("Invalid email or password.");

        if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            throw new UnauthorizedException("Invalid email or password.");

        if (user.Status == Common.Enums.UserStatus.Inactive)
            throw new ForbiddenException("This account has been deactivated.");

        user.LastLogin = DateTime.UtcNow;
        await _userRepository.UpdateAsync(user);

        var accessToken = _tokenService.GenerateAccessToken(user);
        var refreshToken = _tokenService.GenerateRefreshToken();

        return new AuthResponse(accessToken, refreshToken, MapToDto(user));
    }

    public async Task<UserDto> GetCurrentUserAsync(Guid userId)
    {
        var user = await _userRepository.GetByIdAsync(userId)
            ?? throw new NotFoundException("User", userId);

        return MapToDto(user);
    }

    public async Task ConfirmEmailAsync(ConfirmEmailRequest request)
    {
        // In production, validate the token from an email service
        // For now, we confirm based on a simple token lookup
        var user = await _userRepository.GetByEmailAsync(request.Token)
            ?? throw new NotFoundException("User", request.Token);

        user.EmailConfirmed = true;
        await _userRepository.UpdateAsync(user);
    }

    public async Task ForgotPasswordAsync(ForgotPasswordRequest request)
    {
        var user = await _userRepository.GetByEmailAsync(request.Email.ToLowerInvariant());
        // Always return success to prevent email enumeration
        if (user == null) return;

        // In production: generate a reset token and send via email service
        // For now, this is a no-op stub
    }

    public async Task ResetPasswordAsync(ResetPasswordRequest request)
    {
        // In production: validate the reset token
        // For now, we use the token as email for simplicity
        var user = await _userRepository.GetByEmailAsync(request.Token)
            ?? throw new UnauthorizedException("Invalid or expired reset token.");

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
        await _userRepository.UpdateAsync(user);
    }

    public async Task Verify2FaAsync(Guid userId, Verify2FaRequest request)
    {
        var user = await _userRepository.GetByIdAsync(userId)
            ?? throw new NotFoundException("User", userId);

        // Simple mock 2FA: code "123456" always passes
        if (request.Code != "123456")
            throw new UnauthorizedException("Invalid 2FA code.");

        user.TwoFactorEnabled = true;
        await _userRepository.UpdateAsync(user);
    }

    private static UserDto MapToDto(User user) => new(
        user.Id,
        user.Name,
        user.Email,
        user.Role,
        user.Status,
        user.Anonymized,
        user.EmailConfirmed,
        user.TwoFactorEnabled,
        user.LastLogin,
        user.CreatedAt,
        user.Addresses.Select(a => new AddressDto(
            a.Id, a.Label, a.FirstName, a.LastName, a.Company,
            a.Street, a.Street2, a.City, a.PostalCode, a.Country, a.Phone
        )),
        user.PaymentMethods.Select(p => new PaymentMethodDto(p.Id, p.Type, p.Label))
    );
}
