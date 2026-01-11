using System.Net.Http.Headers;
using System.Text.Json;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Http;

namespace BLL.Services
{
    public class ClerkService
    {
        private readonly HttpClient _httpClient;
        private readonly string _secretKey;
        private readonly string _webhookSecret;
        private readonly IConfiguration _configuration;

        public ClerkService(IHttpClientFactory httpClientFactory, IConfiguration configuration)
        {
            _httpClient = httpClientFactory.CreateClient();
            _configuration = configuration;
            _secretKey = configuration["Clerk:SecretKey"] ?? throw new InvalidOperationException("Clerk:SecretKey is not configured");
            _webhookSecret = configuration["Clerk:WebhookSecret"] ?? throw new InvalidOperationException("Clerk:WebhookSecret is not configured");

            // Set default headers for Clerk API
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _secretKey);
            _httpClient.BaseAddress = new Uri("https://api.clerk.com/v1/");
        }

        /// <summary>
        /// Verify JWT token và lấy user info từ Clerk
        /// </summary>
        public async Task<ClerkUserInfo?> VerifyTokenAndGetUserAsync(string token)
        {
            try
            {
                var request = new HttpRequestMessage(HttpMethod.Get, "users/me");
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

                var response = await _httpClient.SendAsync(request);
                
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
                return JsonSerializer.Deserialize<ClerkWebhookEvent>(jsonPayload, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
            }
            catch
            {
                return null;
            }
        }
    }

    // DTOs cho Clerk API responses
    public class ClerkUserInfo
    {
        public string? Id { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? EmailAddress { get; set; }
        public string? PhoneNumber { get; set; }
        public string? ImageUrl { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public DateTime? LastSignInAt { get; set; }
    }

    public class ClerkWebhookEvent
    {
        public string? Type { get; set; }
        public ClerkWebhookData? Data { get; set; }
    }

    public class ClerkWebhookData
    {
        public string? Id { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public List<ClerkEmailAddress>? EmailAddresses { get; set; }
        public List<ClerkPhoneNumber>? PhoneNumbers { get; set; }
        public string? ImageUrl { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public DateTime? LastSignInAt { get; set; }
    }

    public class ClerkEmailAddress
    {
        public string? EmailAddress { get; set; }
        public bool? Verified { get; set; }
    }

    public class ClerkPhoneNumber
    {
        public string? PhoneNumber { get; set; }
        public bool? Verified { get; set; }
    }
}
