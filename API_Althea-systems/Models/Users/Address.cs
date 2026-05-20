namespace API_Althea_systems.Models.Users;

public class Address
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Label { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? Company { get; set; }
    public string Street { get; set; } = string.Empty;
    public string? Street2 { get; set; }
    public string City { get; set; } = string.Empty;
    public string PostalCode { get; set; } = string.Empty;
    public string Country { get; set; } = "France";
    public string? Phone { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Marks the user's preferred address — pre-selected on the checkout
    /// picker, badged "Par défaut" in /account/addresses. Exactly ONE
    /// non-archived address per user can hold this flag; UserService is the
    /// authority (it unsets others when setting one).
    /// </summary>
    public bool IsDefault { get; set; }

    /// <summary>
    /// Soft-delete flag. The FK from Order.BillingAddressId / ShippingAddressId
    /// is <c>Restrict</c> so we can't hard-delete an address that any order
    /// references (the invoice PDF would lose its billing block). When the
    /// user "deletes" from /account/addresses we flip this true and the
    /// address disappears from the UI + the checkout picker, but remains in
    /// place for historic orders.
    /// </summary>
    public bool Archived { get; set; }

    // Navigation
    public User User { get; set; } = null!;
}
