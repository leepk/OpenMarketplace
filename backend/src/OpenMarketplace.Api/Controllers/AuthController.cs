using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.DataProtection;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Net;
using System.Net.Mail;
using System.Net.Http.Headers;
using OpenMarketplace.Domain.Users;
using OpenMarketplace.Infrastructure.Persistence;
using OpenMarketplace.Shared.Api;

namespace OpenMarketplace.Api.Controllers;

[ApiController]
[Route("api/v1/auth")]
public sealed class AuthController(AppDbContext db, IConfiguration config, IHttpClientFactory httpClientFactory, IDataProtectionProvider dataProtectionProvider) : ControllerBase
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



    [HttpGet("external/google")]
    public async Task<IActionResult> GoogleLogin([FromQuery] string? returnUrl, CancellationToken ct)
    {
        var settings = await GetOAuthSettingsAsync(ct);
        if (!IsTrue(settings, "auth.google_enabled")) return OAuthFailure("Google login is disabled.", returnUrl);
        var clientId = GetSetting(settings, "auth.google_client_id");
        if (string.IsNullOrWhiteSpace(clientId)) return OAuthFailure("Google Client ID is not configured.", returnUrl);
        var callback = BuildCallbackUrl("google");
        var state = ProtectOAuthState("google", returnUrl);
        var url = "https://accounts.google.com/o/oauth2/v2/auth" +
                  $"?client_id={Uri.EscapeDataString(clientId)}&redirect_uri={Uri.EscapeDataString(callback)}&response_type=code&state={Uri.EscapeDataString(state)}&prompt=select_account";
        return Redirect(url);
    }

    [HttpGet("external/google/callback")]
    public async Task<IActionResult> GoogleCallback([FromQuery] string? code, [FromQuery] string? state, [FromQuery] string? error, CancellationToken ct)
    {
        var oauthState = UnprotectOAuthState(state, "google");
        if (!string.IsNullOrWhiteSpace(error) || string.IsNullOrWhiteSpace(code)) return OAuthFailure(error ?? "Google did not return an authorization code.", oauthState.ReturnUrl);
        try
        {
            var settings = await GetOAuthSettingsAsync(ct);
            var clientId = GetSetting(settings, "auth.google_client_id");
            var clientSecret = GetSetting(settings, "auth.google_client_secret");
            var client = httpClientFactory.CreateClient();
            if (string.IsNullOrWhiteSpace(clientId) || string.IsNullOrWhiteSpace(clientSecret))
                throw new InvalidOperationException("Google Client ID or Client Secret is not configured.");

            var callbackUrl = BuildCallbackUrl("google");
            using var tokenResponse = await client.PostAsync("https://oauth2.googleapis.com/token", new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["code"] = code,
                ["client_id"] = clientId.Trim(),
                ["client_secret"] = clientSecret.Trim(),
                ["redirect_uri"] = callbackUrl,
                ["grant_type"] = "authorization_code"
            }), ct);
            var tokenBody = await tokenResponse.Content.ReadAsStringAsync(ct);
            if (!tokenResponse.IsSuccessStatusCode)
                throw new InvalidOperationException($"Google token exchange failed ({(int)tokenResponse.StatusCode}): {GetOAuthError(tokenBody)}");

            using var tokenJson = JsonDocument.Parse(tokenBody);
            var accessToken = tokenJson.RootElement.GetProperty("access_token").GetString() ?? throw new InvalidOperationException("Google access token is missing.");
            using var request = new HttpRequestMessage(HttpMethod.Get, "https://openidconnect.googleapis.com/v1/userinfo");
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
            using var profileResponse = await client.SendAsync(request, ct);
            var profileBody = await profileResponse.Content.ReadAsStringAsync(ct);
            if (!profileResponse.IsSuccessStatusCode)
                throw new InvalidOperationException($"Google user profile request failed ({(int)profileResponse.StatusCode}): {GetOAuthError(profileBody)}");

            using var profile = JsonDocument.Parse(profileBody);
            return await CompleteExternalLoginAsync("Google", profile.RootElement.GetProperty("sub").GetString(), profile.RootElement.GetProperty("email").GetString(), GetJsonString(profile.RootElement, "name"), GetJsonString(profile.RootElement, "picture"), oauthState.ReturnUrl, settings, ct);
        }
        catch (Exception ex) { return OAuthFailure($"Google login failed: {ex.Message}", oauthState.ReturnUrl); }
    }

    [HttpGet("external/facebook")]
    public async Task<IActionResult> FacebookLogin([FromQuery] string? returnUrl, CancellationToken ct)
    {
        var settings = await GetOAuthSettingsAsync(ct);
        if (!IsTrue(settings, "auth.facebook_enabled")) return OAuthFailure("Facebook login is disabled.", returnUrl);
        var appId = GetSetting(settings, "auth.facebook_app_id");
        if (string.IsNullOrWhiteSpace(appId)) return OAuthFailure("Facebook App ID is not configured.", returnUrl);
        var callback = BuildCallbackUrl("facebook");
        var state = ProtectOAuthState("facebook", returnUrl);
        var url = "https://www.facebook.com/v23.0/dialog/oauth" +
                  $"?client_id={Uri.EscapeDataString(appId)}" +
                  $"&redirect_uri={Uri.EscapeDataString(callback)}" +
                  "&response_type=code" +
                  $"&scope={Uri.EscapeDataString("email")}" +
                  $"&state={Uri.EscapeDataString(state)}";
        return Redirect(url);
    }

    [HttpGet("external/facebook/callback")]
    public async Task<IActionResult> FacebookCallback([FromQuery] string? code, [FromQuery] string? state, [FromQuery] string? error_message, CancellationToken ct)
    {
        var oauthState = UnprotectOAuthState(state, "facebook");
        if (!string.IsNullOrWhiteSpace(error_message) || string.IsNullOrWhiteSpace(code)) return OAuthFailure(error_message ?? "Facebook did not return an authorization code.", oauthState.ReturnUrl);
        try
        {
            var settings = await GetOAuthSettingsAsync(ct);
            var appId = GetSetting(settings, "auth.facebook_app_id");
            var appSecret = GetSetting(settings, "auth.facebook_app_secret");
            var callback = BuildCallbackUrl("facebook");
            var client = httpClientFactory.CreateClient();
            var tokenUrl = $"https://graph.facebook.com/v23.0/oauth/access_token?client_id={Uri.EscapeDataString(appId)}&client_secret={Uri.EscapeDataString(appSecret)}&redirect_uri={Uri.EscapeDataString(callback)}&code={Uri.EscapeDataString(code)}";
            using var tokenResponse = await client.GetAsync(tokenUrl, ct);
            tokenResponse.EnsureSuccessStatusCode();
            using var tokenJson = JsonDocument.Parse(await tokenResponse.Content.ReadAsStringAsync(ct));
            var accessToken = tokenJson.RootElement.GetProperty("access_token").GetString() ?? throw new InvalidOperationException("Facebook access token is missing.");
            var profileUrl = $"https://graph.facebook.com/me?fields=id,name,email,picture.type(large)&access_token={Uri.EscapeDataString(accessToken)}";
            using var profileResponse = await client.GetAsync(profileUrl, ct);
            profileResponse.EnsureSuccessStatusCode();
            using var profile = JsonDocument.Parse(await profileResponse.Content.ReadAsStringAsync(ct));
            var picture = profile.RootElement.TryGetProperty("picture", out var pictureNode) && pictureNode.TryGetProperty("data", out var dataNode) ? GetJsonString(dataNode, "url") : string.Empty;
            var providerUserId = GetJsonString(profile.RootElement, "id");
            var email = GetJsonString(profile.RootElement, "email");
            var name = GetJsonString(profile.RootElement, "name");

            if (string.IsNullOrWhiteSpace(email))
            {
                if (string.IsNullOrWhiteSpace(providerUserId))
                    return OAuthFailure("Facebook did not provide a valid user identifier.", oauthState.ReturnUrl);

                var completionTicket = ProtectExternalProfileCompletion(new ExternalProfileCompletion(
                    "Facebook",
                    providerUserId,
                    name,
                    picture,
                    NormalizeReturnUrl(oauthState.ReturnUrl),
                    DateTimeOffset.UtcNow.AddMinutes(15)));
                var customerBaseUrl = (config["Customer:BaseUrl"] ?? config["Frontend:BaseUrl"] ?? "http://localhost:3000").TrimEnd('/');
                return Redirect($"{customerBaseUrl}/auth/complete-profile?ticket={Uri.EscapeDataString(completionTicket)}");
            }

            return await CompleteExternalLoginAsync("Facebook", providerUserId, email, name, picture, oauthState.ReturnUrl, settings, ct);
        }
        catch (Exception ex) { return OAuthFailure($"Facebook login failed: {ex.Message}", oauthState.ReturnUrl); }
    }


    [HttpPost("external/complete-profile")]
    public async Task<ActionResult<ApiResponse<object>>> CompleteExternalProfile(CompleteExternalProfileRequest request, CancellationToken ct)
    {
        ExternalProfileCompletion completion;
        try
        {
            completion = UnprotectExternalProfileCompletion(request.Ticket);
        }
        catch
        {
            return BadRequest(ApiResponse<object>.Fail("InvalidTicket", "This sign-in request is invalid or has expired. Please try Facebook login again.", HttpContext.TraceIdentifier));
        }

        var email = (request.Email ?? string.Empty).Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(email) || !email.Contains('@'))
            return BadRequest(ApiResponse<object>.Fail("Validation", "A valid email address is required.", HttpContext.TraceIdentifier));

        var existing = await db.UserProfiles.FirstOrDefaultAsync(x => x.Email == email && !x.IsDeleted, ct);
        if (existing is not null)
            return Conflict(ApiResponse<object>.Fail("EmailExists", "This email is already registered. Sign in with your existing account, then connect Facebook from account settings.", HttpContext.TraceIdentifier));

        var user = new UserProfile
        {
            Name = string.IsNullOrWhiteSpace(completion.Name) ? email.Split('@')[0] : completion.Name.Trim(),
            Email = email,
            Role = "Customer",
            Source = completion.Provider,
            PasswordHash = string.Empty,
            AvatarUrl = string.IsNullOrWhiteSpace(completion.AvatarUrl) ? DefaultAvatar(email) : completion.AvatarUrl,
            EmailVerified = false,
            TrustScore = 50,
            Status = "Active"
        };
        db.UserProfiles.Add(user);
        await db.SaveChangesAsync(ct);

        return Ok(ApiResponse<object>.Ok(new
        {
            user = ToSafeUser(user),
            token = CreateJwtToken(user, false, TimeSpan.FromDays(7)),
            tokenType = "Bearer",
            returnUrl = NormalizeReturnUrl(completion.ReturnUrl),
            emailVerificationRequired = true
        }, HttpContext.TraceIdentifier));
    }

    [HttpPost("send-phone-verification")]
    public async Task<ActionResult<ApiResponse<object>>> SendPhoneVerification(SendPhoneVerificationRequest request, CancellationToken ct)
    {
        if (request.UserId == Guid.Empty)
            return Unauthorized(ApiResponse<object>.Fail("Unauthorized", "Please login first.", HttpContext.TraceIdentifier));

        var user = await db.UserProfiles.FirstOrDefaultAsync(x => x.Id == request.UserId && !x.IsDeleted, ct);
        if (user is null)
            return NotFound(ApiResponse<object>.Fail("NotFound", "User not found.", HttpContext.TraceIdentifier));
        if (string.IsNullOrWhiteSpace(user.Phone))
            return BadRequest(ApiResponse<object>.Fail("PhoneRequired", "Add a phone number to your profile first.", HttpContext.TraceIdentifier));
        if (user.PhoneVerified)
            return Ok(ApiResponse<object>.Ok(new { message = "Phone number is already verified.", alreadyVerified = true }, HttpContext.TraceIdentifier));

        var settings = await db.AppSettings.AsNoTracking()
            .Where(x => !x.IsDeleted && (x.Key.StartsWith("sms.") || x.Key.StartsWith("otp.") || x.Key == "template.sms_verification" || x.Key == "site.name"))
            .ToDictionaryAsync(x => x.Key, x => x.Value, ct);
        string Get(string key, string fallback = "") => settings.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value) ? value : fallback;
        if (!bool.TryParse(Get("sms.enabled", "false"), out var smsEnabled) || !smsEnabled)
            return BadRequest(ApiResponse<object>.Fail("SmsDisabled", "SMS verification is not enabled. Please contact support.", HttpContext.TraceIdentifier));

        var resendSeconds = int.TryParse(Get("otp.resend_seconds", "60"), out var rs) ? Math.Clamp(rs, 30, 600) : 60;
        if (user.PhoneVerificationSentAt.HasValue && user.PhoneVerificationSentAt.Value.AddSeconds(resendSeconds) > DateTimeOffset.UtcNow)
        {
            var retryAfter = (int)Math.Ceiling((user.PhoneVerificationSentAt.Value.AddSeconds(resendSeconds) - DateTimeOffset.UtcNow).TotalSeconds);
            return StatusCode(StatusCodes.Status429TooManyRequests, ApiResponse<object>.Fail("TooManyRequests", $"Please wait {retryAfter} seconds before requesting another code.", HttpContext.TraceIdentifier));
        }

        var length = int.TryParse(Get("otp.length", "6"), out var len) ? Math.Clamp(len, 4, 8) : 6;
        var expiresMinutes = int.TryParse(Get("otp.expires_minutes", "5"), out var em) ? Math.Clamp(em, 1, 30) : 5;
        var max = (int)Math.Pow(10, length);
        var code = RandomNumberGenerator.GetInt32(max / 10, max).ToString();
        user.PhoneVerificationCodeHash = Sha256(code);
        user.PhoneVerificationExpiresAt = DateTimeOffset.UtcNow.AddMinutes(expiresMinutes);
        user.PhoneVerificationSentAt = DateTimeOffset.UtcNow;
        user.PhoneVerificationAttempts = 0;
        user.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);

        try
        {
            await SendVerificationSmsAsync(user.Phone, code, expiresMinutes, settings, ct);
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status502BadGateway, ApiResponse<object>.Fail("SmsDeliveryFailed", $"Unable to send verification SMS: {ex.Message}", HttpContext.TraceIdentifier));
        }

        return Ok(ApiResponse<object>.Ok(new { message = "Verification code sent.", expiresInMinutes = expiresMinutes, resendAfterSeconds = resendSeconds }, HttpContext.TraceIdentifier));
    }

    [HttpPost("verify-phone")]
    public async Task<ActionResult<ApiResponse<object>>> VerifyPhone(VerifyPhoneRequest request, CancellationToken ct)
    {
        if (request.UserId == Guid.Empty)
            return Unauthorized(ApiResponse<object>.Fail("Unauthorized", "Please login first.", HttpContext.TraceIdentifier));
        var user = await db.UserProfiles.FirstOrDefaultAsync(x => x.Id == request.UserId && !x.IsDeleted, ct);
        if (user is null)
            return NotFound(ApiResponse<object>.Fail("NotFound", "User not found.", HttpContext.TraceIdentifier));
        if (user.PhoneVerified)
            return Ok(ApiResponse<object>.Ok(new { user = ToSafeUser(user), message = "Phone number is already verified." }, HttpContext.TraceIdentifier));

        var settings = await db.AppSettings.AsNoTracking().Where(x => !x.IsDeleted && x.Key.StartsWith("otp.")).ToDictionaryAsync(x => x.Key, x => x.Value, ct);
        var maxAttempts = settings.TryGetValue("otp.max_attempts", out var ma) && int.TryParse(ma, out var parsed) ? Math.Clamp(parsed, 3, 10) : 5;
        if (user.PhoneVerificationAttempts >= maxAttempts)
            return BadRequest(ApiResponse<object>.Fail("TooManyAttempts", "Too many invalid attempts. Request a new verification code.", HttpContext.TraceIdentifier));
        if (string.IsNullOrWhiteSpace(user.PhoneVerificationCodeHash) || user.PhoneVerificationExpiresAt is null || user.PhoneVerificationExpiresAt <= DateTimeOffset.UtcNow)
            return BadRequest(ApiResponse<object>.Fail("CodeExpired", "The verification code is invalid or expired. Request a new code.", HttpContext.TraceIdentifier));

        user.PhoneVerificationAttempts++;
        if (!CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(user.PhoneVerificationCodeHash), Encoding.UTF8.GetBytes(Sha256((request.Code ?? string.Empty).Trim()))))
        {
            await db.SaveChangesAsync(ct);
            return BadRequest(ApiResponse<object>.Fail("InvalidCode", "The verification code is incorrect.", HttpContext.TraceIdentifier));
        }

        user.PhoneVerified = true;
        user.PhoneVerificationCodeHash = string.Empty;
        user.PhoneVerificationExpiresAt = null;
        user.PhoneVerificationSentAt = null;
        user.PhoneVerificationAttempts = 0;
        user.TrustScore = Math.Min(100, user.TrustScore + 10);
        user.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);
        return Ok(ApiResponse<object>.Ok(new { user = ToSafeUser(user), message = "Phone number verified successfully." }, HttpContext.TraceIdentifier));
    }


    [HttpPost("forgot-password")]
    public async Task<ActionResult<ApiResponse<object>>> ForgotPassword(ForgotPasswordRequest request, CancellationToken ct)
    {
        var email = (request.Email ?? string.Empty).Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(email) || !email.Contains('@'))
            return Ok(ApiResponse<object>.Ok(new { message = "If the email exists, a reset link will be sent." }, HttpContext.TraceIdentifier));

        var user = await db.UserProfiles.FirstOrDefaultAsync(x => x.Email == email && !x.IsDeleted, ct);
        if (user is not null)
        {
            var rawToken = Convert.ToHexString(RandomNumberGenerator.GetBytes(32)).ToLowerInvariant();
            user.PasswordResetTokenHash = Sha256(rawToken);
            user.PasswordResetExpiresAt = DateTimeOffset.UtcNow.AddMinutes(30);
            user.UpdatedAt = DateTimeOffset.UtcNow;
            await db.SaveChangesAsync(ct);

            var settings = await db.AppSettings.AsNoTracking()
                .Where(x => !x.IsDeleted && (x.Key.StartsWith("email.") || x.Key.StartsWith("template.email_password_reset") || x.Key == "site.name"))
                .ToDictionaryAsync(x => x.Key, x => x.Value, ct);
            var customerBaseUrl = (config["CustomerApp:BaseUrl"] ?? config["Frontend:BaseUrl"] ?? "http://localhost:3000").TrimEnd('/');
            var resetUrl = $"{customerBaseUrl}/reset-password?token={Uri.EscapeDataString(rawToken)}&email={Uri.EscapeDataString(email)}";
            await TrySendPasswordResetEmailAsync(user, resetUrl, settings, ct);
        }

        return Ok(ApiResponse<object>.Ok(new { message = "If the email exists, a reset link will be sent." }, HttpContext.TraceIdentifier));
    }

    [HttpPost("reset-password")]
    public async Task<ActionResult<ApiResponse<object>>> ResetPassword(ResetPasswordRequest request, CancellationToken ct)
    {
        var email = (request.Email ?? string.Empty).Trim().ToLowerInvariant();
        var tokenHash = Sha256(request.Token ?? string.Empty);
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(request.Token) || string.IsNullOrWhiteSpace(request.NewPassword) || request.NewPassword.Length < 6)
            return BadRequest(ApiResponse<object>.Fail("Validation", "Email, valid reset token, and a password of at least 6 characters are required.", HttpContext.TraceIdentifier));

        var user = await db.UserProfiles.FirstOrDefaultAsync(x => x.Email == email && x.PasswordResetTokenHash == tokenHash && !x.IsDeleted, ct);
        if (user is null || user.PasswordResetExpiresAt is null || user.PasswordResetExpiresAt <= DateTimeOffset.UtcNow)
            return BadRequest(ApiResponse<object>.Fail("InvalidResetToken", "The password reset link is invalid or expired.", HttpContext.TraceIdentifier));

        user.PasswordHash = HashPassword(request.NewPassword);
        user.PasswordResetTokenHash = string.Empty;
        user.PasswordResetExpiresAt = null;
        user.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);
        return Ok(ApiResponse<object>.Ok(new { message = "Password reset successfully." }, HttpContext.TraceIdentifier));
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


    private async Task<Dictionary<string, string>> GetOAuthSettingsAsync(CancellationToken ct) => await db.AppSettings.AsNoTracking()
        .Where(x => !x.IsDeleted && x.Key.StartsWith("auth."))
        .ToDictionaryAsync(x => x.Key, x => x.Value ?? string.Empty, ct);

    private string BuildCallbackUrl(string provider) => $"{Request.Scheme}://{Request.Host}{Request.PathBase}/api/v1/auth/external/{provider}/callback";

    private string ProtectOAuthState(string provider, string? returnUrl)
    {
        var safeReturnUrl = NormalizeReturnUrl(returnUrl);
        var payload = JsonSerializer.Serialize(new OAuthState(provider, safeReturnUrl, DateTimeOffset.UtcNow.AddMinutes(10)));
        return dataProtectionProvider.CreateProtector("OpenMarketplace.ExternalOAuth.State.v1").Protect(payload);
    }

    private OAuthState UnprotectOAuthState(string? state, string expectedProvider)
    {
        if (string.IsNullOrWhiteSpace(state)) return new OAuthState(expectedProvider, "/", DateTimeOffset.UtcNow);
        try
        {
            var json = dataProtectionProvider.CreateProtector("OpenMarketplace.ExternalOAuth.State.v1").Unprotect(state);
            var value = JsonSerializer.Deserialize<OAuthState>(json);
            if (value is null || !string.Equals(value.Provider, expectedProvider, StringComparison.OrdinalIgnoreCase) || value.ExpiresAt < DateTimeOffset.UtcNow)
                throw new InvalidOperationException("OAuth state is invalid or expired.");
            return value with { ReturnUrl = NormalizeReturnUrl(value.ReturnUrl) };
        }
        catch { return new OAuthState(expectedProvider, "/", DateTimeOffset.UtcNow); }
    }


    private string ProtectExternalProfileCompletion(ExternalProfileCompletion value)
    {
        var payload = JsonSerializer.Serialize(value);
        return dataProtectionProvider.CreateProtector("OpenMarketplace.ExternalOAuth.ProfileCompletion.v1").Protect(payload);
    }

    private ExternalProfileCompletion UnprotectExternalProfileCompletion(string? ticket)
    {
        if (string.IsNullOrWhiteSpace(ticket)) throw new InvalidOperationException("Missing completion ticket.");
        var json = dataProtectionProvider.CreateProtector("OpenMarketplace.ExternalOAuth.ProfileCompletion.v1").Unprotect(ticket);
        var value = JsonSerializer.Deserialize<ExternalProfileCompletion>(json) ?? throw new InvalidOperationException("Invalid completion ticket.");
        if (value.ExpiresAt < DateTimeOffset.UtcNow || !string.Equals(value.Provider, "Facebook", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Completion ticket expired.");
        return value with { ReturnUrl = NormalizeReturnUrl(value.ReturnUrl) };
    }

    private async Task<IActionResult> CompleteExternalLoginAsync(string provider, string? providerUserId, string? email, string? name, string? avatarUrl, string returnUrl, Dictionary<string, string> settings, CancellationToken ct)
    {
        email = (email ?? string.Empty).Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(providerUserId) || string.IsNullOrWhiteSpace(email)) return OAuthFailure($"{provider} did not provide a verified email address.", returnUrl);
        var user = await db.UserProfiles.FirstOrDefaultAsync(x => x.Email == email && !x.IsDeleted, ct);
        if (user is null)
        {
            if (!IsTrue(settings, "auth.auto_create_user", true)) return OAuthFailure("No account is linked to this email and automatic account creation is disabled.", returnUrl);
            user = new UserProfile
            {
                Name = string.IsNullOrWhiteSpace(name) ? email.Split('@')[0] : name.Trim(),
                Email = email,
                Role = "Customer",
                Source = provider,
                PasswordHash = string.Empty,
                AvatarUrl = string.IsNullOrWhiteSpace(avatarUrl) ? DefaultAvatar(email) : avatarUrl,
                EmailVerified = true,
                TrustScore = 50,
                Status = "Active"
            };
            db.UserProfiles.Add(user);
        }
        else
        {
            if (!string.Equals(user.Status, "Active", StringComparison.OrdinalIgnoreCase)) return OAuthFailure("This account is not active.", returnUrl);
            user.EmailVerified = true;
            if (string.IsNullOrWhiteSpace(user.AvatarUrl) && !string.IsNullOrWhiteSpace(avatarUrl)) user.AvatarUrl = avatarUrl;
            if (string.IsNullOrWhiteSpace(user.Source) || user.Source == "WebCustomer") user.Source = provider;
            user.UpdatedAt = DateTimeOffset.UtcNow;
        }
        await db.SaveChangesAsync(ct);
        var token = CreateJwtToken(user, false, TimeSpan.FromDays(7));
        var customerBaseUrl = (config["Customer:BaseUrl"] ?? config["Frontend:BaseUrl"] ?? "http://localhost:3000").TrimEnd('/');
        return Redirect($"{customerBaseUrl}/auth/callback?token={Uri.EscapeDataString(token)}&returnUrl={Uri.EscapeDataString(NormalizeReturnUrl(returnUrl))}");
    }

    private IActionResult OAuthFailure(string message, string? returnUrl)
    {
        var customerBaseUrl = (config["Customer:BaseUrl"] ?? config["Frontend:BaseUrl"] ?? "http://localhost:3000").TrimEnd('/');
        return Redirect($"{customerBaseUrl}/auth/callback?error={Uri.EscapeDataString(message)}&returnUrl={Uri.EscapeDataString(NormalizeReturnUrl(returnUrl))}");
    }

    private static string NormalizeReturnUrl(string? value)
    {
        if (string.IsNullOrWhiteSpace(value) || !value.StartsWith('/') || value.StartsWith("//")) return "/";
        return value.Length > 500 ? "/" : value;
    }
    private static string GetOAuthError(string responseBody)
    {
        if (string.IsNullOrWhiteSpace(responseBody)) return "No response body.";
        try
        {
            using var json = JsonDocument.Parse(responseBody);
            var root = json.RootElement;
            var error = GetJsonString(root, "error");
            var description = GetJsonString(root, "error_description");
            if (!string.IsNullOrWhiteSpace(error) && !string.IsNullOrWhiteSpace(description)) return $"{error}: {description}";
            if (!string.IsNullOrWhiteSpace(error)) return error;
        }
        catch { }
        return responseBody.Length <= 500 ? responseBody : responseBody[..500];
    }

    private static string GetSetting(IReadOnlyDictionary<string, string> settings, string key) => settings.TryGetValue(key, out var value) ? value : string.Empty;
    private static bool IsTrue(IReadOnlyDictionary<string, string> settings, string key, bool fallback = false) => settings.TryGetValue(key, out var value) ? bool.TryParse(value, out var enabled) && enabled : fallback;
    private static string GetJsonString(JsonElement element, string property) => element.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() ?? string.Empty : string.Empty;

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


    private static string Sha256(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();

    private static async Task TrySendPasswordResetEmailAsync(UserProfile user, string resetUrl, Dictionary<string, string> settings, CancellationToken ct)
    {
        string Get(string key, string fallback = "") => settings.TryGetValue(key, out var value) ? value : fallback;
        if (!bool.TryParse(Get("email.enabled"), out var enabled) || !enabled) return;
        if (!string.Equals(Get("email.provider", "SMTP"), "SMTP", StringComparison.OrdinalIgnoreCase)) return;
        var host = Get("email.smtp_host");
        if (string.IsNullOrWhiteSpace(host)) return;

        var siteName = Get("site.name", "Vunoca");
        var subject = Get("template.email_password_reset_subject", "Reset your {{siteName}} password")
            .Replace("{{siteName}}", siteName);
        var body = Get("template.email_password_reset_body", "Hello {{userName}}, reset your password here: {{resetUrl}}")
            .Replace("{{siteName}}", siteName)
            .Replace("{{userName}}", user.Name)
            .Replace("{{resetUrl}}", resetUrl)
            .Replace("{{expiresMinutes}}", "30");

        using var message = new MailMessage();
        message.From = new MailAddress(Get("email.from_address", "no-reply@vunoca.com"), Get("email.from_name", siteName));
        message.To.Add(user.Email);
        message.Subject = subject;
        message.Body = body;
        message.IsBodyHtml = body.Contains('<');

        using var smtp = new SmtpClient(host);
        smtp.Port = int.TryParse(Get("email.smtp_port", "587"), out var port) ? port : 587;
        smtp.EnableSsl = bool.TryParse(Get("email.smtp_use_ssl", "true"), out var ssl) && ssl;
        var username = Get("email.smtp_username");
        if (!string.IsNullOrWhiteSpace(username)) smtp.Credentials = new NetworkCredential(username, Get("email.smtp_password"));
        await smtp.SendMailAsync(message, ct);
    }


    private static async Task SendVerificationSmsAsync(string phone, string code, int expiresMinutes, IReadOnlyDictionary<string, string> settings, CancellationToken ct)
    {
        string Get(string key, string fallback = "") => settings.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value) ? value : fallback;
        var siteName = Get("site.name", "Vunoca");
        var body = Get("template.sms_verification", "{{siteName}} verification code: {{code}}. Expires in {{expiresMinutes}} minutes.")
            .Replace("{{siteName}}", siteName)
            .Replace("{{code}}", code)
            .Replace("{{expiresMinutes}}", expiresMinutes.ToString());
        var provider = Get("sms.provider", "Twilio");

        using var http = new HttpClient();
        if (provider.Equals("Twilio", StringComparison.OrdinalIgnoreCase))
        {
            var sid = Get("sms.account_sid");
            var token = Get("sms.auth_token");
            var from = Get("sms.from_number");
            if (string.IsNullOrWhiteSpace(sid) || string.IsNullOrWhiteSpace(token) || string.IsNullOrWhiteSpace(from))
                throw new InvalidOperationException("Twilio SMS settings are incomplete.");
            http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.ASCII.GetBytes($"{sid}:{token}")));
            using var content = new FormUrlEncodedContent(new Dictionary<string, string> { ["To"] = phone, ["From"] = from, ["Body"] = body });
            using var response = await http.PostAsync($"https://api.twilio.com/2010-04-01/Accounts/{Uri.EscapeDataString(sid)}/Messages.json", content, ct);
            if (!response.IsSuccessStatusCode) throw new InvalidOperationException($"Twilio returned {(int)response.StatusCode}.");
            return;
        }

        if (provider.Equals("Vonage", StringComparison.OrdinalIgnoreCase))
        {
            var key = Get("sms.account_sid");
            var secret = Get("sms.auth_token");
            var from = Get("sms.from_number", siteName);
            if (string.IsNullOrWhiteSpace(key) || string.IsNullOrWhiteSpace(secret))
                throw new InvalidOperationException("Vonage SMS settings are incomplete.");
            using var content = new FormUrlEncodedContent(new Dictionary<string, string> { ["api_key"] = key, ["api_secret"] = secret, ["to"] = phone.TrimStart('+'), ["from"] = from, ["text"] = body });
            using var response = await http.PostAsync("https://rest.nexmo.com/sms/json", content, ct);
            if (!response.IsSuccessStatusCode) throw new InvalidOperationException($"Vonage returned {(int)response.StatusCode}.");
            return;
        }

        throw new InvalidOperationException("The selected SMS provider is not supported for automatic delivery.");
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
public sealed record ForgotPasswordRequest(string Email);
public sealed record ResetPasswordRequest(string Email, string Token, string NewPassword);
public sealed record SendPhoneVerificationRequest(Guid UserId);
public sealed record VerifyPhoneRequest(Guid UserId, string Code);
public sealed record CompleteExternalProfileRequest(string Ticket, string Email);

public sealed record ExternalProfileCompletion(string Provider, string ProviderUserId, string Name, string AvatarUrl, string ReturnUrl, DateTimeOffset ExpiresAt);
public sealed record OAuthState(string Provider, string ReturnUrl, DateTimeOffset ExpiresAt);
