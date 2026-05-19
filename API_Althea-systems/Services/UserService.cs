using API_Althea_systems.Common.Exceptions;
using API_Althea_systems.Models.Users;
using API_Althea_systems.Models.Shared;
using API_Althea_systems.Repositories.IRepositories;
using API_Althea_systems.Services.IServices;

namespace API_Althea_systems.Services;

public class UserService : IUserService
{
    private readonly IUserRepository _userRepository;
    private readonly IStripeService _stripe;

    public UserService(IUserRepository userRepository, IStripeService stripe)
    {
        _userRepository = userRepository;
        _stripe = stripe;
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
        if (request.Email != null)
        {
            var newEmail = request.Email.ToLowerInvariant();
            if (!string.Equals(newEmail, user.Email, StringComparison.OrdinalIgnoreCase))
            {
                if (await _userRepository.EmailExistsAsync(newEmail))
                    throw new ConflictException("User", "email", newEmail);

                user.Email = newEmail;
                user.EmailConfirmed = false;
            }
        }
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
            // Don't pre-set Id with Guid.NewGuid(): when adding a child through
            // a tracked parent's navigation collection (user.Addresses.Add),
            // EF Core's heuristic treats non-default PKs as "existing entity"
            // and emits UPDATE … WHERE Id = <new-guid> instead of INSERT.
            // That UPDATE matches 0 rows → DbUpdateConcurrencyException.
            // Leaving Id at default(Guid) lets EF mark the entity as Added
            // and generate a fresh Guid client-side at SaveChanges time.
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
            // Same EF-navigation-Add caveat as AddAddressAsync: leave Id at
            // default so EF treats this as a fresh entity (INSERT) instead of
            // assuming it exists in DB (UPDATE WHERE Id → 0 rows).
            UserId = userId,
            Type = request.Type,
            Label = request.Label
        };

        user.PaymentMethods.Add(pm);
        await _userRepository.UpdateAsync(user);

        return MapPaymentMethod(pm);
    }

    public async Task DeletePaymentMethodAsync(Guid userId, Guid paymentMethodId)
    {
        var user = await _userRepository.GetByIdAsync(userId)
            ?? throw new NotFoundException("User", userId);

        var pm = user.PaymentMethods.FirstOrDefault(p => p.Id == paymentMethodId)
            ?? throw new NotFoundException("PaymentMethod", paymentMethodId);

        // Detach FIRST on Stripe — if it fails, we still have the DB row to
        // retry on. The reverse (DB delete then Stripe fail) would leave an
        // orphan card on the customer with no way for our UI to see it again.
        if (!string.IsNullOrEmpty(pm.StripePaymentMethodId))
        {
            await _stripe.DetachPaymentMethodAsync(pm.StripePaymentMethodId);
        }

        user.PaymentMethods.Remove(pm);
        await _userRepository.UpdateAsync(user);
    }

    private static UserDto MapToDto(User user) => new(
        user.Id, user.Name, user.Email, user.Role, user.Status,
        user.Anonymized, user.EmailConfirmed, user.TwoFactorEnabled,
        user.LastLogin, user.CreatedAt,
        user.Addresses.Select(a => new AddressDto(a.Id, a.Label, a.FirstName, a.LastName, a.Company, a.Street, a.Street2, a.City, a.PostalCode, a.Country, a.Phone)),
        user.PaymentMethods.Select(MapPaymentMethod)
    );

    private static PaymentMethodDto MapPaymentMethod(UserPaymentMethod p) => new(
        p.Id, p.Type, p.Label,
        p.StripePaymentMethodId, p.Brand, p.Last4, p.ExpMonth, p.ExpYear);
}
