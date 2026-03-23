namespace API_Althea_systems.Models.Users;

public class UserPaymentMethod
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Type { get; set; } = string.Empty; // visa, mastercard, bank_transfer
    public string Label { get; set; } = string.Empty; // e.g. "Visa •••• 4242"
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public User User { get; set; } = null!;
}
