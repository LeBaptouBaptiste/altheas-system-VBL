namespace API_Althea_systems.Models.Users;

public class UserRecoveryCode
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }

    /// <summary>
    /// BCrypt hash of the 16-character recovery code (format xxxx-xxxx-xxxx-xxxx).
    /// The plaintext is shown to the user exactly once at generation.
    /// </summary>
    public string CodeHash { get; set; } = string.Empty;

    /// <summary>
    /// Set when the code is consumed. Once non-null, the code cannot be reused.
    /// </summary>
    public DateTime? UsedAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public User User { get; set; } = null!;
}
