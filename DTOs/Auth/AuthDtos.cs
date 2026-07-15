using System.Text.Json.Serialization;

namespace DeskGuardBackend.DTOs.Auth
{
    public class LoginRequest
    {
        [JsonPropertyName("email")]
        public string Email { get; set; } = string.Empty;

        [JsonPropertyName("password")]
        public string Password { get; set; } = string.Empty;
    }

    public class UserDto
    {
        [JsonPropertyName("id")]
        public long Id { get; set; }

        [JsonPropertyName("company_id")]
        public long? CompanyId { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("email")]
        public string? Email { get; set; }

        [JsonPropertyName("mobile_number")]
        public string? MobileNumber { get; set; }

        [JsonPropertyName("phone")]
        public string? Phone { get; set; }

        [JsonPropertyName("avatar")]
        public string? Avatar { get; set; }

        [JsonPropertyName("is_active")]
        public bool IsActive { get; set; }

        [JsonPropertyName("role")]
        public string Role { get; set; } = string.Empty;
    }

    public class LoginResponse
    {
        [JsonPropertyName("token")]
        public string Token { get; set; } = string.Empty;

        [JsonPropertyName("user")]
        public UserDto User { get; set; } = null!;
    }

    public class OtpRequest
    {
        [JsonPropertyName("mobile_number")]
        public string MobileNumber { get; set; } = string.Empty;
    }

    public class OtpVerifyRequest
    {
        [JsonPropertyName("mobile_number")]
        public string MobileNumber { get; set; } = string.Empty;

        [JsonPropertyName("otp")]
        public string Otp { get; set; } = string.Empty;

        [JsonPropertyName("machine_uid")]
        public string MachineUid { get; set; } = string.Empty;
    }

    public class OtpVerifyResponse
    {
        [JsonPropertyName("token")]
        public string Token { get; set; } = string.Empty;

        [JsonPropertyName("user")]
        public UserDto User { get; set; } = null!;
    }
}
