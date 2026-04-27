using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using API_Althea_systems.Common.Auth;
using API_Althea_systems.Models.Users;
using API_Althea_systems.Services.IServices;

namespace API_Althea_systems.Services;

public class TokenService : ITokenService
{
    // Special-purpose token TTLs.
    private static readonly TimeSpan ChallengeTtl = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan AdminSetupTtl = TimeSpan.FromMinutes(15);

    private readonly IConfiguration _configuration;

    public TokenService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    // ─────────────────────────────────────────────────────────
    //  Access token
    // ─────────────────────────────────────────────────────────

    public string GenerateAccessToken(User user, bool mfaVerified = false)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new(ClaimTypes.Role, user.Role.ToString()),
            new("role", user.Role.ToString()),
            new("purpose", TokenPurpose.Access),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        };

        // RFC 8176 Authentication Method References. Step-up guards can
        // require "mfa" here (set to "pwd mfa") to gate sensitive actions.
        var amr = mfaVerified ? "pwd mfa" : "pwd";
        claims.Add(new Claim("amr", amr));

        var jwtSettings = _configuration.GetSection("JwtSettings");
        var minutes = int.Parse(jwtSettings["AccessTokenExpirationMinutes"] ?? "60");
        return BuildToken(claims, TimeSpan.FromMinutes(minutes));
    }

    public string GenerateRefreshToken()
    {
        var randomBytes = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);
        return Convert.ToBase64String(randomBytes);
    }

    // ─────────────────────────────────────────────────────────
    //  Special-purpose tokens
    // ─────────────────────────────────────────────────────────

    public string GenerateChallengeToken(User user) =>
        BuildSingleUserPurposeToken(user, TokenPurpose.TwoFactorChallenge, ChallengeTtl);

    public string GenerateAdminSetupToken(User user) =>
        BuildSingleUserPurposeToken(user, TokenPurpose.TwoFactorSetupRequired, AdminSetupTtl);

    public Guid? ValidateSpecialToken(string token, string expectedPurpose)
    {
        if (string.IsNullOrWhiteSpace(token)) return null;

        var jwtSettings = _configuration.GetSection("JwtSettings");
        var key = Encoding.UTF8.GetBytes(jwtSettings["SecretKey"]!);

        var parameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings["Issuer"],
            ValidAudience = jwtSettings["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(key),
            ClockSkew = TimeSpan.Zero,
        };

        try
        {
            var handler = new JwtSecurityTokenHandler();
            var principal = handler.ValidateToken(token, parameters, out var validated);
            if (validated is not JwtSecurityToken jwt) return null;

            var purpose = jwt.Claims.FirstOrDefault(c => c.Type == "purpose")?.Value;
            if (!string.Equals(purpose, expectedPurpose, StringComparison.Ordinal)) return null;

            var sub = jwt.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Sub)?.Value;
            return Guid.TryParse(sub, out var userId) ? userId : null;
        }
        catch
        {
            // Any validation failure (signature, expiry, malformed) -> null.
            return null;
        }
    }

    // ─────────────────────────────────────────────────────────
    //  Helpers
    // ─────────────────────────────────────────────────────────

    private string BuildSingleUserPurposeToken(User user, string purpose, TimeSpan ttl)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new("purpose", purpose),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        };
        return BuildToken(claims, ttl);
    }

    private string BuildToken(IEnumerable<Claim> claims, TimeSpan ttl)
    {
        var jwtSettings = _configuration.GetSection("JwtSettings");
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings["SecretKey"]!));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: jwtSettings["Issuer"],
            audience: jwtSettings["Audience"],
            claims: claims,
            expires: DateTime.UtcNow.Add(ttl),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
