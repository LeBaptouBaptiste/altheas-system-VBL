using API_Althea_systems.Models.Users;
using API_Althea_systems.Models.Shared;

namespace API_Althea_systems.Services.IServices;

public interface IUserService
{
    Task<UserDto> GetByIdAsync(Guid id);
    Task<PaginatedResponse<UserDto>> GetAllAsync(int page, int pageSize);
    Task<UserDto> UpdateAsync(Guid id, UserUpdateRequest request);
    Task DeleteAsync(Guid id);
    Task<UserDto> AnonymizeAsync(Guid id);

    // Addresses
    Task<AddressDto> AddAddressAsync(Guid userId, AddressCreateRequest request);
    Task<AddressDto> UpdateAddressAsync(Guid userId, Guid addressId, AddressCreateRequest request);
    Task DeleteAddressAsync(Guid userId, Guid addressId);

    // Payment Methods
    Task<PaymentMethodDto> AddPaymentMethodAsync(Guid userId, PaymentMethodCreateRequest request);
    Task DeletePaymentMethodAsync(Guid userId, Guid paymentMethodId);
}
