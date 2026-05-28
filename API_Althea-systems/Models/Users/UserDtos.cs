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
    IEnumerable<PaymentMethodDto> PaymentMethods,
    // Phase 7 (store credit): current available balance in cents EUR.
    // Used by /account to show the balance and by /checkout to cap the
    // "apply credit" input.
    long CreditBalanceCents = 0,
    // Two-letter locale code used for transactional emails. Null = system
    // default (French). Editable via the account preferences screen.
    string? PreferredLocale = null
);

public record UserUpdateRequest(
    string? Name,
    string? Email,
    UserStatus? Status,
    // Customer can switch the email language without going through the full
    // profile update — sent as null when not updating (record default).
    string? PreferredLocale = null
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
    string? Phone,
    // Default flag: pre-selected at checkout, badged in /account/addresses.
    // Default value so existing callers (controllers, anonymization) keep
    // compiling without explicit changes.
    bool IsDefault = false
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
    string Label,
    // Stripe-side identifier, used at checkout to re-confirm a PaymentIntent
    // with a saved card (stripe.confirmCardPayment({ payment_method: pm_xxx })).
    string? StripePaymentMethodId,
    // Display metadata — safe to ship to the browser, never sensitive.
    string? Brand,
    string? Last4,
    int? ExpMonth,
    int? ExpYear
);

public record PaymentMethodCreateRequest(
    string Type,
    string Label
);
