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
        if (request.PreferredLocale != null)
        {
            // Two-letter code only; reject anything else to keep the email
            // renderer's locale-suffix lookup safe (no path injection via
            // ../another-folder/welcome.html).
            var loc = request.PreferredLocale.ToLowerInvariant();
            if (loc is "fr" or "en" or "ms" or "ar")
            {
                user.PreferredLocale = loc;
            }
        }

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

        // Dedup: if the user already has a NON-ARCHIVED address with the
        // same postal fingerprint, return THAT one instead of inserting.
        // (Archived rows are ignored — if the user explicitly deleted it,
        // re-adding the same address should be a real new entry, otherwise
        // they couldn't recover a deleted address by re-entering it.)
        var existing = user.Addresses.FirstOrDefault(a =>
            !a.Archived
            && string.Equals(a.FirstName, request.FirstName, StringComparison.OrdinalIgnoreCase)
            && string.Equals(a.LastName, request.LastName, StringComparison.OrdinalIgnoreCase)
            && string.Equals(NormalizeStreet(a.Street), NormalizeStreet(request.Street), StringComparison.OrdinalIgnoreCase)
            && string.Equals(a.PostalCode, request.PostalCode, StringComparison.OrdinalIgnoreCase)
            && string.Equals(a.City, request.City, StringComparison.OrdinalIgnoreCase)
            && string.Equals(a.Country, request.Country, StringComparison.OrdinalIgnoreCase));
        if (existing is not null)
        {
            return MapAddress(existing);
        }

        // First non-archived address for this user → mark as default
        // automatically (so the picker always has a pre-selected option).
        var isFirstAddress = !user.Addresses.Any(a => !a.Archived);

        var address = new Address
        {
            IsDefault = isFirstAddress,
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

        return MapAddress(address);
    }

    /// <summary>
    /// Normalises street strings for fingerprint comparison: trims, collapses
    /// runs of whitespace to single spaces. Lets "12 Rue de la Paix" match
    /// "12  Rue de la Paix " (typo / paste artifact) without considering
    /// "12 rue paix" equivalent (street numbers / words matter).
    /// </summary>
    private static string NormalizeStreet(string s) =>
        System.Text.RegularExpressions.Regex.Replace(s?.Trim() ?? "", @"\s+", " ");

    public async Task<AddressDto> UpdateAddressAsync(Guid userId, Guid addressId, AddressCreateRequest request)
    {
        var user = await _userRepository.GetByIdAsync(userId)
            ?? throw new NotFoundException("User", userId);

        var address = user.Addresses.FirstOrDefault(a => a.Id == addressId && !a.Archived)
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

        return MapAddress(address);
    }

    public async Task DeleteAddressAsync(Guid userId, Guid addressId)
    {
        var user = await _userRepository.GetByIdAsync(userId)
            ?? throw new NotFoundException("User", userId);

        var address = user.Addresses.FirstOrDefault(a => a.Id == addressId && !a.Archived)
            ?? throw new NotFoundException("Address", addressId);

        // Soft-delete — historic orders still reference this row via FK
        // (Restrict). Hiding from UI is enough.
        address.Archived = true;
        var wasDefault = address.IsDefault;
        address.IsDefault = false;

        // If the deleted address was the user's default, promote the most
        // recently created remaining one. Keeps "always one default" so
        // the checkout picker never has nothing pre-selected.
        if (wasDefault)
        {
            var fallback = user.Addresses
                .Where(a => !a.Archived && a.Id != addressId)
                .OrderByDescending(a => a.CreatedAt)
                .FirstOrDefault();
            if (fallback is not null)
            {
                fallback.IsDefault = true;
            }
        }

        await _userRepository.UpdateAsync(user);
    }

    public async Task<AddressDto> SetDefaultAddressAsync(Guid userId, Guid addressId)
    {
        var user = await _userRepository.GetByIdAsync(userId)
            ?? throw new NotFoundException("User", userId);

        var target = user.Addresses.FirstOrDefault(a => a.Id == addressId && !a.Archived)
            ?? throw new NotFoundException("Address", addressId);

        if (target.IsDefault)
        {
            // Already default — no DB roundtrip needed.
            return MapAddress(target);
        }

        // Atomic-ish: flip all to false then flip the target on. EF tracks
        // the changes and SaveChanges serialises a single UPDATE per row.
        foreach (var a in user.Addresses)
        {
            a.IsDefault = false;
        }
        target.IsDefault = true;

        await _userRepository.UpdateAsync(user);
        return MapAddress(target);
    }

    private static AddressDto MapAddress(Address a) => new(
        a.Id, a.Label, a.FirstName, a.LastName, a.Company,
        a.Street, a.Street2, a.City, a.PostalCode, a.Country, a.Phone, a.IsDefault);

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
        // Filter archived addresses out — historic-order rows that the user
        // soft-deleted from /account. Default first so the UI / checkout
        // picker can rely on order.
        user.Addresses
            .Where(a => !a.Archived)
            .OrderByDescending(a => a.IsDefault)
            .ThenByDescending(a => a.CreatedAt)
            .Select(MapAddress),
        user.PaymentMethods.Select(MapPaymentMethod),
        user.CreditBalanceCents,
        user.PreferredLocale
    );

    private static PaymentMethodDto MapPaymentMethod(UserPaymentMethod p) => new(
        p.Id, p.Type, p.Label,
        p.StripePaymentMethodId, p.Brand, p.Last4, p.ExpMonth, p.ExpYear);
}
