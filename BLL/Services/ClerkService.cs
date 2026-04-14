using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Collections.Concurrent;
using System.Security.Claims;

namespace BLL.Services
{
    public class ClerkService
    {
        private readonly HttpClient _httpClient;
        private readonly string _secretKey;
        private readonly string _webhookSecret;
        private readonly IConfiguration _configuration;
        private static readonly ConcurrentDictionary<string, ConfigurationManager<OpenIdConnectConfiguration>> _oidcManagers = new();
        private static readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            NumberHandling = JsonNumberHandling.AllowReadingFromString
        };

        // Constructor cho Typed HttpClient pattern (dùng với AddHttpClient<ClerkService>())
        public ClerkService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _secretKey = configuration["Clerk:SecretKey"] ?? throw new InvalidOperationException("Clerk:SecretKey is not configured");
            _webhookSecret = configuration["Clerk:WebhookSecret"] ?? throw new InvalidOperationException("Clerk:WebhookSecret is not configured");

            // Set default headers for Clerk API
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _secretKey);
            _httpClient.BaseAddress = new Uri("https://api.clerk.com/v1/");
        }

        /// <summary>
        /// Verify JWT token bằng OIDC/JWKS (issuer=Clerk InstanceUrl) và trả về Clerk userId (sub).
        /// </summary>
        public async Task<string?> VerifyTokenAndGetUserIdAsync(string token)
        {
            try
            {
                var sub = await ValidateTokenAndGetSubOrThrowAsync(token);
                return string.IsNullOrWhiteSpace(sub) ? null : sub;
            }
            catch
            {
                return null;
            }
        }

        private async Task<string?> ValidateTokenAndGetSubOrThrowAsync(string token)
        {
            // NOTE:
            // - Không tin hoàn toàn vào config Clerk:InstanceUrl (có thể bị override sai khi chạy dev).
            // - Lấy issuer trực tiếp từ JWT (unsigned read) để tải OIDC/JWKS đúng instance.
            var handler = new JwtSecurityTokenHandler
            {
                // Quan trọng: không map 'sub' -> NameIdentifier
                MapInboundClaims = false
            };

            var jwt = handler.ReadJwtToken(token);
            var issuer = (jwt.Issuer ?? "").Trim().TrimEnd('/');
            if (string.IsNullOrWhiteSpace(issuer))
            {
                throw new SecurityTokenInvalidIssuerException("Missing issuer (iss) in token");
            }

            // Basic sanity check (dev): chỉ chấp nhận issuer từ Clerk
            if (!issuer.EndsWith(".clerk.accounts.dev", StringComparison.OrdinalIgnoreCase))
            {
                throw new SecurityTokenInvalidIssuerException($"Unexpected issuer: {issuer}");
            }

            var metadataAddress = $"{issuer}/.well-known/openid-configuration";
            var mgr = _oidcManagers.GetOrAdd(
                metadataAddress,
                addr => new ConfigurationManager<OpenIdConnectConfiguration>(addr, new OpenIdConnectConfigurationRetriever())
            );

            var oidc = await mgr.GetConfigurationAsync(CancellationToken.None);

            var parameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = issuer,
                ValidateAudience = false,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                IssuerSigningKeys = oidc.SigningKeys,
                ClockSkew = TimeSpan.FromMinutes(2),
                NameClaimType = "sub"
            };

            var principal = handler.ValidateToken(token, parameters, out _);
            return principal.FindFirst("sub")?.Value
                   ?? principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        }

        /// <summary>
        /// Dev helper: trả về lỗi validate token để debug.
        /// </summary>
        public async Task<(string? UserId, string? Error)> VerifyTokenAndGetUserIdWithErrorAsync(string token)
        {
            try
            {
                var sub = await ValidateTokenAndGetSubOrThrowAsync(token);
                return !string.IsNullOrWhiteSpace(sub) ? (sub, null) : (null, "Token validated but missing sub claim");
            }
            catch (Exception ex)
            {
                return (null, ex.ToString());
            }
        }

        /// <summary>
        /// Verify session JWT và lấy user info từ Clerk REST API bằng secret key.
        /// </summary>
        public async Task<ClerkUserInfo?> VerifyTokenAndGetUserAsync(string token)
        {
            var userId = await VerifyTokenAndGetUserIdAsync(token);
            if (string.IsNullOrWhiteSpace(userId))
            {
                return null;
            }

            return await GetUserByIdAsync(userId);
        }

        /// <summary>
        /// Lấy user info từ Clerk bằng user ID
        /// </summary>
        public async Task<ClerkUserInfo?> GetUserByIdAsync(string userId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"users/{userId}");
                
                if (!response.IsSuccessStatusCode)
                {
                    return null;
                }

                var content = await response.Content.ReadAsStringAsync();
                var userInfo = JsonSerializer.Deserialize<ClerkUserInfo>(content, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                return userInfo;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Verify webhook signature từ Clerk (sử dụng Svix)
        /// </summary>
        public bool VerifyWebhookSignature(string payload, string signature)
        {
            // Clerk sử dụng Svix để sign webhooks
            // Signature format: v1,<timestamp>,<signature1> v1,<timestamp>,<signature2> ...
            if (string.IsNullOrEmpty(signature))
            {
                return false;
            }

            try
            {
                // Parse signature header (có thể có nhiều signatures)
                var signatures = signature.Split(' ');
                foreach (var sig in signatures)
                {
                    var parts = sig.Split(',');
                    if (parts.Length != 3 || parts[0] != "v1")
                    {
                        continue;
                    }

                    var timestamp = parts[1];
                    var signatureValue = parts[2];

                    // Tạo signed payload: timestamp.payload
                    var signedPayload = $"{timestamp}.{payload}";
                    var secretBytes = Encoding.UTF8.GetBytes(_webhookSecret);
                    
                    using var hmac = new System.Security.Cryptography.HMACSHA256(secretBytes);
                    var computedHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(signedPayload));
                    var computedSignature = Convert.ToBase64String(computedHash).Replace("+", "-").Replace("/", "_").TrimEnd('=');
                    
                    // So sánh signature (Svix sử dụng base64url encoding)
                    if (computedSignature == signatureValue)
                    {
                        return true;
                    }
                }

                return false;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Parse webhook event từ Clerk
        /// </summary>
        public ClerkWebhookEvent? ParseWebhookEvent(string jsonPayload)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(jsonPayload))
                {
                    throw new ArgumentException("JSON payload is null or empty");
                }

                var result = JsonSerializer.Deserialize<ClerkWebhookEvent>(jsonPayload, _jsonOptions);
                
                if (result == null)
                {
                    throw new InvalidOperationException("Deserialized result is null");
                }

                return result;
            }
            catch (JsonException ex)
            {
                // Throw exception với message chi tiết để controller có thể log
                throw new InvalidOperationException(
                    $"JSON Parse Error: {ex.Message}. Path: {ex.Path}, LineNumber: {ex.LineNumber}, BytePositionInLine: {ex.BytePositionInLine}. " +
                    $"Payload preview: {(jsonPayload.Length > 200 ? jsonPayload.Substring(0, 200) + "..." : jsonPayload)}", 
                    ex);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    $"Parse Error: {ex.Message}. Payload preview: {(jsonPayload?.Length > 200 ? jsonPayload.Substring(0, 200) + "..." : jsonPayload ?? "null")}", 
                    ex);
            }
        }

        /// <summary>
        /// Convert Unix timestamp (milliseconds) to DateTime
        /// </summary>
        public static DateTime? ConvertFromUnixTimestamp(long? timestamp)
        {
            if (timestamp == null || timestamp == 0)
                return null;
            
            return DateTimeOffset.FromUnixTimeMilliseconds(timestamp.Value).UtcDateTime;
        }
    }

    // DTOs cho Clerk API responses
    public class ClerkUserInfo
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }
        
        [JsonPropertyName("first_name")]
        public string? FirstName { get; set; }
        
        [JsonPropertyName("last_name")]
        public string? LastName { get; set; }
        
        [JsonPropertyName("email_address")]
        public string? EmailAddress { get; set; }
        
        /// <summary>GET /users/:id trả về email_addresses (mảng), không phải email_address. Dùng GetPrimaryEmail() để lấy email.</summary>
        [JsonPropertyName("email_addresses")]
        public List<ClerkEmailAddress>? EmailAddresses { get; set; }
        
        [JsonPropertyName("primary_email_address_id")]
        public string? PrimaryEmailAddressId { get; set; }
        
        [JsonPropertyName("phone_number")]
        public string? PhoneNumber { get; set; }
        
        [JsonPropertyName("image_url")]
        public string? ImageUrl { get; set; }
        
        [JsonPropertyName("created_at")]
        public long? CreatedAt { get; set; }
        
        [JsonPropertyName("updated_at")]
        public long? UpdatedAt { get; set; }
        
        [JsonPropertyName("last_sign_in_at")]
        public long? LastSignInAt { get; set; }
        
        /// <summary>Lấy primary email: từ EmailAddress hoặc từ mảng email_addresses (primary theo id rồi phần tử đầu).</summary>
        public string? GetPrimaryEmail() =>
            EmailAddress
            ?? EmailAddresses?.FirstOrDefault(e => e.Id == PrimaryEmailAddressId)?.EmailAddress
            ?? EmailAddresses?.FirstOrDefault()?.EmailAddress;
        
        // Helper methods để convert timestamp
        public DateTime? GetCreatedAtDateTime() => ClerkService.ConvertFromUnixTimestamp(CreatedAt);
        public DateTime? GetUpdatedAtDateTime() => ClerkService.ConvertFromUnixTimestamp(UpdatedAt);
        public DateTime? GetLastSignInAtDateTime() => ClerkService.ConvertFromUnixTimestamp(LastSignInAt);
    }

    public class ClerkWebhookEvent
    {
        [JsonPropertyName("type")]
        public string? Type { get; set; }
        
        [JsonPropertyName("data")]
        public ClerkWebhookData? Data { get; set; }
    }

    public class ClerkWebhookData
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }
        
        [JsonPropertyName("first_name")]
        public string? FirstName { get; set; }
        
        [JsonPropertyName("last_name")]
        public string? LastName { get; set; }
        
        [JsonPropertyName("email_addresses")]
        public List<ClerkEmailAddress>? EmailAddresses { get; set; }
        
        [JsonPropertyName("phone_numbers")]
        public List<ClerkPhoneNumber>? PhoneNumbers { get; set; }
        
        [JsonPropertyName("image_url")]
        public string? ImageUrl { get; set; }
        
        [JsonPropertyName("created_at")]
        public long? CreatedAt { get; set; }
        
        [JsonPropertyName("updated_at")]
        public long? UpdatedAt { get; set; }
        
        [JsonPropertyName("last_sign_in_at")]
        public long? LastSignInAt { get; set; }
        
        // Hỗ trợ cho session.created event
        [JsonPropertyName("user_id")]
        public string? UserId { get; set; }
        
        [JsonPropertyName("user")]
        public ClerkWebhookData? User { get; set; }
        
        // Các field khác từ Clerk (có thể null, không bắt buộc)
        [JsonPropertyName("backup_code_enabled")]
        public bool? BackupCodeEnabled { get; set; }
        
        [JsonPropertyName("banned")]
        public bool? Banned { get; set; }
        
        [JsonPropertyName("bypass_client_trust")]
        public bool? BypassClientTrust { get; set; }
        
        [JsonPropertyName("create_organization_enabled")]
        public bool? CreateOrganizationEnabled { get; set; }
        
        [JsonPropertyName("delete_self_enabled")]
        public bool? DeleteSelfEnabled { get; set; }
        
        [JsonPropertyName("enterprise_accounts")]
        public List<object>? EnterpriseAccounts { get; set; }
        
        [JsonPropertyName("external_accounts")]
        public List<object>? ExternalAccounts { get; set; }
        
        [JsonPropertyName("external_id")]
        public string? ExternalId { get; set; }
        
        [JsonPropertyName("has_image")]
        public bool? HasImage { get; set; }
        
        [JsonPropertyName("last_active_at")]
        public long? LastActiveAt { get; set; }
        
        [JsonPropertyName("legal_accepted_at")]
        public long? LegalAcceptedAt { get; set; }
        
        [JsonPropertyName("locale")]
        public string? Locale { get; set; }
        
        [JsonPropertyName("locked")]
        public bool? Locked { get; set; }
        
        [JsonPropertyName("lockout_expires_in_seconds")]
        public long? LockoutExpiresInSeconds { get; set; }
        
        [JsonPropertyName("mfa_disabled_at")]
        public long? MfaDisabledAt { get; set; }
        
        [JsonPropertyName("mfa_enabled_at")]
        public long? MfaEnabledAt { get; set; }
        
        [JsonPropertyName("object")]
        public string? Object { get; set; }
        
        [JsonPropertyName("passkeys")]
        public List<object>? Passkeys { get; set; }
        
        [JsonPropertyName("password_enabled")]
        public bool? PasswordEnabled { get; set; }
        
        [JsonPropertyName("password_last_updated_at")]
        public long? PasswordLastUpdatedAt { get; set; }
        
        [JsonPropertyName("primary_email_address_id")]
        public string? PrimaryEmailAddressId { get; set; }
        
        [JsonPropertyName("primary_phone_number_id")]
        public string? PrimaryPhoneNumberId { get; set; }
        
        [JsonPropertyName("primary_web3_wallet_id")]
        public string? PrimaryWeb3WalletId { get; set; }
        
        [JsonPropertyName("private_metadata")]
        public Dictionary<string, object>? PrivateMetadata { get; set; }
        
        [JsonPropertyName("profile_image_url")]
        public string? ProfileImageUrl { get; set; }
        
        [JsonPropertyName("public_metadata")]
        public Dictionary<string, object>? PublicMetadata { get; set; }
        
        [JsonPropertyName("requires_password_reset")]
        public bool? RequiresPasswordReset { get; set; }
        
        [JsonPropertyName("saml_accounts")]
        public List<object>? SamlAccounts { get; set; }
        
        [JsonPropertyName("totp_enabled")]
        public bool? TotpEnabled { get; set; }
        
        [JsonPropertyName("two_factor_enabled")]
        public bool? TwoFactorEnabled { get; set; }
        
        [JsonPropertyName("unsafe_metadata")]
        public Dictionary<string, object>? UnsafeMetadata { get; set; }
        
        [JsonPropertyName("username")]
        public string? Username { get; set; }
        
        [JsonPropertyName("verification_attempts_remaining")]
        public int? VerificationAttemptsRemaining { get; set; }
        
        [JsonPropertyName("web3_wallets")]
        public List<object>? Web3Wallets { get; set; }
        
        // Helper methods để convert timestamp
        public DateTime? GetCreatedAtDateTime() => ClerkService.ConvertFromUnixTimestamp(CreatedAt);
        public DateTime? GetUpdatedAtDateTime() => ClerkService.ConvertFromUnixTimestamp(UpdatedAt);
        public DateTime? GetLastSignInAtDateTime() => ClerkService.ConvertFromUnixTimestamp(LastSignInAt);
    }

    public class ClerkEmailAddress
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }
        
        [JsonPropertyName("email_address")]
        public string? EmailAddress { get; set; }
        
        [JsonPropertyName("verification")]
        public ClerkVerification? Verification { get; set; }
        
        // Các field khác từ Clerk (có thể null, không bắt buộc)
        [JsonPropertyName("created_at")]
        public long? CreatedAt { get; set; }
        
        [JsonPropertyName("updated_at")]
        public long? UpdatedAt { get; set; }
        
        [JsonPropertyName("linked_to")]
        public List<object>? LinkedTo { get; set; }
        
        [JsonPropertyName("matches_sso_connection")]
        public bool? MatchesSsoConnection { get; set; }
        
        [JsonPropertyName("object")]
        public string? Object { get; set; }
        
        [JsonPropertyName("reserved")]
        public bool? Reserved { get; set; }
        
        // Helper để check verified
        public bool IsVerified => Verification?.Status == "verified";
    }

    public class ClerkVerification
    {
        [JsonPropertyName("status")]
        public string? Status { get; set; }
        
        [JsonPropertyName("strategy")]
        public string? Strategy { get; set; }
        
        // Các field khác từ Clerk (có thể null, không bắt buộc)
        [JsonPropertyName("attempts")]
        public int? Attempts { get; set; }
        
        [JsonPropertyName("expire_at")]
        public long? ExpireAt { get; set; }
        
        [JsonPropertyName("object")]
        public string? Object { get; set; }
    }

    public class ClerkPhoneNumber
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }
        
        [JsonPropertyName("phone_number")]
        public string? PhoneNumber { get; set; }
        
        [JsonPropertyName("verification")]
        public ClerkVerification? Verification { get; set; }
        
        // Helper để check verified
        public bool IsVerified => Verification?.Status == "verified";
    }
}
