using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using OpenMarketplace.Domain.Users;
using OpenMarketplace.Infrastructure.Persistence;
using OpenMarketplace.Shared.Api;

namespace OpenMarketplace.Api.Controllers;

[ApiController]
[Route("api/v1/auth")]
public sealed class AuthController(AppDbContext db, IConfiguration config) : ControllerBase
{
    [HttpPost("register")]
    public async Task<ActionResult<ApiResponse<object>>> Register(RegisterRequest request, CancellationToken ct)
    {
        var name = (request.Name ?? string.Empty).Trim();
        var email = (request.Email ?? string.Empty).Trim().ToLowerInvariant();
        var password = request.Password ?? string.Empty;

        if (string.IsNullOrWhiteSpace(name))
            return BadRequest(ApiResponse<object>.Fail("Validation", "Full name is required.", HttpContext.TraceIdentifier));
        if (string.IsNullOrWhiteSpace(email) || !email.Contains('@'))
            return BadRequest(ApiResponse<object>.Fail("Validation", "A valid email is required.", HttpContext.TraceIdentifier));
        if (string.IsNullOrWhiteSpace(password) || password.Length < 6)
            return BadRequest(ApiResponse<object>.Fail("Validation", "Password must be at least 6 characters.", HttpContext.TraceIdentifier));

        var exists = await db.UserProfiles.AnyAsync(x => x.Email == email, ct);
        if (exists)
            return Conflict(ApiResponse<object>.Fail("EmailExists", "This email is already registered. Please login instead.", HttpContext.TraceIdentifier));

        var user = new UserProfile
        {
            Name = name,
            Email = email,
            Phone = request.Phone?.Trim() ?? string.Empty,
            Location = request.Location?.Trim() ?? string.Empty,
            Role = "Customer",
            Source = "WebCustomer",
            PasswordHash = HashPassword(password),
            AvatarUrl = DefaultAvatar(email),
            EmailVerified = false,
            TrustScore = 50,
            Status = "Active"
        };
        db.UserProfiles.Add(user);
        await db.SaveChangesAsync(ct);

        return Ok(ApiResponse<object>.Ok(new
        {
            user = ToSafeUser(user),
            token = CreateJwtToken(user, isAdminToken: false, expires: TimeSpan.FromDays(7)),
            tokenType = "Bearer"
        }, HttpContext.TraceIdentifier));
    }

    [HttpPost("admin-login")]
    public async Task<ActionResult<ApiResponse<object>>> AdminLogin(AdminLoginRequest request, CancellationToken ct)
    {
        var email = (request.Email ?? string.Empty).Trim().ToLowerInvariant();
        var password = request.Password ?? string.Empty;

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            return BadRequest(ApiResponse<object>.Fail("Validation", "Email and password are required.", HttpContext.TraceIdentifier));

        var user = await db.UserProfiles.FirstOrDefaultAsync(x => x.Email == email && !x.IsDeleted, ct);
        if (user is null || string.IsNullOrWhiteSpace(user.PasswordHash) || !VerifyPassword(password, user.PasswordHash))
            return Unauthorized(ApiResponse<object>.Fail("InvalidLogin", "Invalid admin email or password.", HttpContext.TraceIdentifier));

        if (!string.Equals(user.Status, "Active", StringComparison.OrdinalIgnoreCase))
            return Unauthorized(ApiResponse<object>.Fail("AccountDisabled", "This admin account is not active.", HttpContext.TraceIdentifier));

        if (!IsAdminRole(user.Role))
            return StatusCode(StatusCodes.Status403Forbidden, ApiResponse<object>.Fail("AccessDenied", "This account does not have admin permission.", HttpContext.TraceIdentifier));

        var expires = request.RememberMe ? TimeSpan.FromDays(14) : TimeSpan.FromHours(8);
        return Ok(ApiResponse<object>.Ok(new
        {
            user = ToSafeUser(user),
            token = CreateJwtToken(user, isAdminToken: true, expires),
            tokenType = "Bearer",
            expiresInMinutes = (int)expires.TotalMinutes
        }, HttpContext.TraceIdentifier));
    }

    [HttpPost("login")]
    public async Task<ActionResult<ApiResponse<object>>> Login(LoginRequest request, CancellationToken ct)
    {
        var email = (request.Email ?? string.Empty).Trim().ToLowerInvariant();
        var password = request.Password ?? string.Empty;

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            return BadRequest(ApiResponse<object>.Fail("Validation", "Email and password are required.", HttpContext.TraceIdentifier));

        var user = await db.UserProfiles.FirstOrDefaultAsync(x => x.Email == email && !x.IsDeleted, ct);
        if (user is null || string.IsNullOrWhiteSpace(user.PasswordHash) || !VerifyPassword(password, user.PasswordHash))
            return Unauthorized(ApiResponse<object>.Fail("InvalidLogin", "Invalid email or password.", HttpContext.TraceIdentifier));

        return Ok(ApiResponse<object>.Ok(new
        {
            user = ToSafeUser(user),
            token = CreateJwtToken(user, isAdminToken: false, expires: TimeSpan.FromDays(7)),
            tokenType = "Bearer"
        }, HttpContext.TraceIdentifier));
    }

    internal static string HashPassword(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(16);
        using var pbkdf2 = new Rfc2898DeriveBytes(password, salt, 100_000, HashAlgorithmName.SHA256);
        var hash = pbkdf2.GetBytes(32);
        return $"v1.{Convert.ToBase64String(salt)}.{Convert.ToBase64String(hash)}";
    }

    internal static bool VerifyPassword(string password, string storedHash)
    {
        var parts = storedHash.Split('.');
        if (parts.Length != 3 || parts[0] != "v1") return false;
        try
        {
            var salt = Convert.FromBase64String(parts[1]);
            var expected = Convert.FromBase64String(parts[2]);
            using var pbkdf2 = new Rfc2898DeriveBytes(password, salt, 100_000, HashAlgorithmName.SHA256);
            var actual = pbkdf2.GetBytes(32);
            return CryptographicOperations.FixedTimeEquals(actual, expected);
        }
        catch { return false; }
    }

    private string CreateJwtToken(UserProfile user, bool isAdminToken, TimeSpan expires)
    {
        var now = DateTimeOffset.UtcNow;
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(GetJwtSecret(config)));
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new(JwtRegisteredClaimNames.Name, user.Name),
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.Name),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Role, user.Role),
            new("role", user.Role),
            new("source", user.Source ?? string.Empty),
            new("token_use", isAdminToken ? "admin" : "customer")
        };

        var token = new JwtSecurityToken(
            issuer: config["Jwt:Issuer"] ?? "OpenMarketplace.Api",
            audience: config["Jwt:Audience"] ?? "OpenMarketplace.Web",
            claims: claims,
            notBefore: now.UtcDateTime,
            expires: now.Add(expires).UtcDateTime,
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static bool IsAdminRole(string? role) => role is "Admin" or "SuperAdmin" or "System" or "Moderator" or "Support";

    internal static string GetJwtSecret(IConfiguration config)
    {
        var secret = config["Jwt:Secret"] ?? Environment.GetEnvironmentVariable("OPENMARKETPLACE_JWT_SECRET");
        if (string.IsNullOrWhiteSpace(secret) || secret.Length < 32)
        {
            secret = "OpenMarketplace_Local_Development_Jwt_Secret_Change_Me_At_Least_64_Chars";
        }
        return secret;
    }

    private static object ToSafeUser(UserProfile user) => new { user.Id, user.Name, user.Email, user.Phone, user.Location, user.AvatarUrl, user.Role, user.Source, user.EmailVerified, user.PhoneVerified, user.IdVerified, user.BusinessVerified, user.Rating, user.ReviewCount, user.TrustScore, user.Status };

    private static string DefaultAvatar(string email)
    {
        var seed = Math.Abs(email.GetHashCode()) % 12 + 1;
        return $"/avatars/avatar-{seed}.svg";
    }
}

public sealed record RegisterRequest(string Name, string Email, string? Phone, string? Location, string Password);
public sealed record LoginRequest(string Email, string Password);
public sealed record AdminLoginRequest(string Email, string Password, bool RememberMe = false);
