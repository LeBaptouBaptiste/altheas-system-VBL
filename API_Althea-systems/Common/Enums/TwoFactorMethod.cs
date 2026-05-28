using System.Text.Json.Serialization;

namespace API_Althea_systems.Common.Enums;

// Order MUST match front/src/lib/enums.ts. Stored in PostgreSQL as a string
// via UserConfiguration.HasConversion<string>(), so adding new entries
// doesn't break existing data — only the indices matter for the front's
// numeric serialisation (kept aligned via comments).
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum TwoFactorMethod
{
    None,           // 0 — 2FA off (TwoFactorEnabled = false)
    Authenticator,  // 1 — TOTP from an authenticator app (Google Authenticator, 1Password…)
    Email,          // 2 — 6-digit code mailed to User.Email at each login
}
