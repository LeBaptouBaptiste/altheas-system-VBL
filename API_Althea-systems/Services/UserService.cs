using API_Althea_systems.Common.Exceptions;
using API_Althea_systems.Models.Users;
using API_Althea_systems.Models.Shared;
using API_Althea_systems.Repositories.IRepositories;
using API_Althea_systems.Services.IServices;

namespace API_Althea_systems.Services;

public class UserService : IUserService
{
    private readonly IUserRepository _userRepository;

    public UserService(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<UserDto> GetByIdAsync(Guid id)
    {
        var user = await _userRepository.GetByIdAsync(id)
            ?? throw new NotFoundException("User", id);
        return MapToDto(user);
    }

    public async Task<PaginatedResponse<UserDto>> GetAllAsync(int page, int pageSize)
    {
        var users = await _userRepository.GetAllAsync(page, pageSize);
        var total = await _userRepository.CountAsync();
        return new PaginatedResponse<UserDto>(
            users.Select(MapToDto), page, pageSize, total, (int)Math.Ceiling(total / (double)pageSize));
    }

    public async Task<UserDto> UpdateAsync(Guid id, UserUpdateRequest request)
    {
        var user = await _userRepository.GetByIdAsync(id)
            ?? throw new NotFoundException("User", id);

        if (request.Name != null) user.Name = request.Name;
        if (request.Email != null) user.Email = request.Email;
        if (request.Status.HasValue) user.Status = request.Status.Value;

        await _userRepository.UpdateAsync(user);
        return MapToDto(user);
    }

    public async Task DeleteAsync(Guid id)
    {
        var user = await _userRepository.GetByIdAsync(id)
            ?? throw new NotFoundException("User", id);
        user.Status = Common.Enums.UserStatus.Inactive;
        await _userRepository.UpdateAsync(user);
    }

    public async Task<UserDto> AnonymizeAsync(Guid id)
    {
        var user = await _userRepository.GetByIdAsync(id)
            ?? throw new NotFoundException("User", id);

        user.Name = "Anonymisé";
        user.Email = $"anon-{user.Id.ToString()[..8]}@deleted.local";
        user.PasswordHash = "";
        user.Anonymized = true;
        user.Status = Common.Enums.UserStatus.Inactive;
        user.Addresses.Clear();
        user.PaymentMethods.Clear();

        await _userRepository.UpdateAsync(user);
        return MapToDto(user);
    }

    // ── Addresses ────────────────────────────────────────

    public async Task<AddressDto> AddAddressAsync(Guid userId, AddressCreateRequest request)
    {
        var user = await _userRepository.GetByIdAsync(userId)
            ?? throw new NotFoundException("User", userId);

        var address = new Address
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Label = request.Label,
            FirstName = request.FirstName,
            LastName = request.LastName,
            Company = request.Company,
            Street = request.Street,
            Street2 = request.Street2,
            City = request.City,
            PostalCode = request.PostalCode,
            Country = request.Country,
            Phone = request.Phone
        };

        user.Addresses.Add(address);
        await _userRepository.UpdateAsync(user);

        return new AddressDto(address.Id, address.Label, address.FirstName, address.LastName,
            address.Company, address.Street, address.Street2, address.City, address.PostalCode,
            address.Country, address.Phone);
    }

    public async Task<AddressDto> UpdateAddressAsync(Guid userId, Guid addressId, AddressCreateRequest request)
    {
        var user = await _userRepository.GetByIdAsync(userId)
            ?? throw new NotFoundException("User", userId);

        var address = user.Addresses.FirstOrDefault(a => a.Id == addressId)
            ?? throw new NotFoundException("Address", addressId);

        address.Label = request.Label;
        address.FirstName = request.FirstName;
        address.LastName = request.LastName;
        address.Company = request.Company;
        address.Street = request.Street;
        address.Street2 = request.Street2;
        address.City = request.City;
        address.PostalCode = request.PostalCode;
        address.Country = request.Country;
        address.Phone = request.Phone;

        await _userRepository.UpdateAsync(user);

        return new AddressDto(address.Id, address.Label, address.FirstName, address.LastName,
            address.Company, address.Street, address.Street2, address.City, address.PostalCode,
            address.Country, address.Phone);
    }

    public async Task DeleteAddressAsync(Guid userId, Guid addressId)
    {
        var user = await _userRepository.GetByIdAsync(userId)
            ?? throw new NotFoundException("User", userId);

        var address = user.Addresses.FirstOrDefault(a => a.Id == addressId)
            ?? throw new NotFoundException("Address", addressId);

        user.Addresses.Remove(address);
        await _userRepository.UpdateAsync(user);
    }

    // ── Payment Methods ──────────────────────────────────

    public async Task<PaymentMethodDto> AddPaymentMethodAsync(Guid userId, PaymentMethodCreateRequest request)
    {
        var user = await _userRepository.GetByIdAsync(userId)
            ?? throw new NotFoundException("User", userId);

        var pm = new UserPaymentMethod
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Type = request.Type,
            Label = request.Label
        };

        user.PaymentMethods.Add(pm);
        await _userRepository.UpdateAsync(user);

        return new PaymentMethodDto(pm.Id, pm.Type, pm.Label);
    }

    public async Task DeletePaymentMethodAsync(Guid userId, Guid paymentMethodId)
    {
        var user = await _userRepository.GetByIdAsync(userId)
            ?? throw new NotFoundException("User", userId);

        var pm = user.PaymentMethods.FirstOrDefault(p => p.Id == paymentMethodId)
            ?? throw new NotFoundException("PaymentMethod", paymentMethodId);

        user.PaymentMethods.Remove(pm);
        await _userRepository.UpdateAsync(user);
    }

    private static UserDto MapToDto(User user) => new(
        user.Id, user.Name, user.Email, user.Role, user.Status,
        user.Anonymized, user.EmailConfirmed, user.TwoFactorEnabled,
        user.LastLogin, user.CreatedAt,
        user.Addresses.Select(a => new AddressDto(a.Id, a.Label, a.FirstName, a.LastName, a.Company, a.Street, a.Street2, a.City, a.PostalCode, a.Country, a.Phone)),
        user.PaymentMethods.Select(p => new PaymentMethodDto(p.Id, p.Type, p.Label))
    );
}
