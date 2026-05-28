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
    /// <summary>
    /// Soft-deletes the address (sets Archived=true). The FK from Order is
    /// Restrict so we can't hard-delete rows referenced by historic orders;
    /// the archived flag hides them from the UI + checkout picker without
    /// breaking those orders. If the deleted one was the user's default,
    /// the next-most-recent non-archived address takes over.
    /// </summary>
    Task DeleteAddressAsync(Guid userId, Guid addressId);
    /// <summary>
    /// Sets the address as the user's default. Unsets the flag on any other
    /// address. No-op if the address is already default. Throws if the
    /// address belongs to a different user or is archived.
    /// </summary>
    Task<AddressDto> SetDefaultAddressAsync(Guid userId, Guid addressId);

    // Payment Methods
    Task<PaymentMethodDto> AddPaymentMethodAsync(Guid userId, PaymentMethodCreateRequest request);
    Task DeletePaymentMethodAsync(Guid userId, Guid paymentMethodId);
}
