using System;
using System.Text.Json.Serialization;

namespace DeskGuardBackend.DTOs.Profile
{
    public class ProfileDto
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

        [JsonPropertyName("employee_id")]
        public string? EmployeeId { get; set; }

        [JsonPropertyName("role")]
        public string Role { get; set; } = "User";

        [JsonPropertyName("is_active")]
        public bool IsActive { get; set; } = true;

        [JsonPropertyName("created_at")]
        public DateTime? CreatedAt { get; set; }

        [JsonPropertyName("last_login_at")]
        public DateTime? LastLoginAt { get; set; }
    }

    public class UpdateProfileRequest
    {
        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("email")]
        public string? Email { get; set; }

        [JsonPropertyName("mobile_number")]
        public string? MobileNumber { get; set; }

        [JsonPropertyName("phone")]
        public string? Phone { get; set; }
    }

    public class ChangePasswordRequest
    {
        [JsonPropertyName("current_password")]
        public string CurrentPassword { get; set; } = string.Empty;

        [JsonPropertyName("new_password")]
        public string NewPassword { get; set; } = string.Empty;

        [JsonPropertyName("confirm_password")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}
