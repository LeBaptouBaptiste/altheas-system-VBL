using API_Althea_systems.Common.Auth;

namespace API_Althea_systems.Models.Users;

/// <summary>
/// Body of POST /api/auth/step-up. The caller picks one of:
///   - <c>Code</c>:     6-digit TOTP or 4-4-4-4 hex recovery code (required when 2FA is on)
///   - <c>Password</c>: only valid for <see cref="StepUpPurpose.Action"/> on a non-2FA account
/// If both are sent, the code path wins.
/// </summary>
public record StepUpRequest(StepUpPurpose Purpose, string? Code = null, string? Password = null);

public record StepUpResponse(string Token, int ExpiresIn);
