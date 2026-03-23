using API_Althea_systems.Common.Enums;

namespace API_Althea_systems.Models.Users;

public record UserDto(
    Guid Id,
    string Name,
    string Email,
    UserRole Role,
    UserStatus Status,
    bool Anonymized,
    bool EmailConfirmed,
    bool TwoFactorEnabled,
    DateTime? LastLogin,
    DateTime CreatedAt,
    IEnumerable<AddressDto> Addresses,
    IEnumerable<PaymentMethodDto> PaymentMethods
);

public record UserUpdateRequest(
    string? Name,
    string? Email,
    UserStatus? Status
);

public record AddressDto(
    Guid Id,
    string Label,
    string FirstName,
    string LastName,
    string? Company,
    string Street,
    string? Street2,
    string City,
    string PostalCode,
    string Country,
    string? Phone
);

public record AddressCreateRequest(
    string Label,
    string FirstName,
    string LastName,
    string? Company,
    string Street,
    string? Street2,
    string City,
    string PostalCode,
    string Country,
    string? Phone
);

public record PaymentMethodDto(
    Guid Id,
    string Type,
    string Label
);

public record PaymentMethodCreateRequest(
    string Type,
    string Label
);
