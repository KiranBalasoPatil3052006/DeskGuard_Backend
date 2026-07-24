using System;
using System.Text.Json.Serialization;

namespace DeskGuardBackend.DTOs.Security
{
    public class SecuritySettingDto
    {
        [JsonPropertyName("id")]
        public long Id { get; set; }

        [JsonPropertyName("min_password_length")]
        public int MinPasswordLength { get; set; } = 6;

        [JsonPropertyName("require_uppercase")]
        public bool RequireUppercase { get; set; } = true;

        [JsonPropertyName("require_lowercase")]
        public bool RequireLowercase { get; set; } = true;

        [JsonPropertyName("require_numbers")]
        public bool RequireNumbers { get; set; } = true;

        [JsonPropertyName("require_special_chars")]
        public bool RequireSpecialChars { get; set; } = true;

        [JsonPropertyName("idle_session_timeout_minutes")]
        public int IdleSessionTimeoutMinutes { get; set; } = 30;

        [JsonPropertyName("max_failed_login_attempts")]
        public int MaxFailedLoginAttempts { get; set; } = 5;

        [JsonPropertyName("account_lockout_duration_minutes")]
        public int AccountLockoutDurationMinutes { get; set; } = 30;

        [JsonPropertyName("updated_at")]
        public DateTime UpdatedAt { get; set; }
    }

    public class UpdateSecuritySettingRequest
    {
        [JsonPropertyName("min_password_length")]
        public int MinPasswordLength { get; set; } = 6;

        [JsonPropertyName("require_uppercase")]
        public bool RequireUppercase { get; set; } = true;

        [JsonPropertyName("require_lowercase")]
        public bool RequireLowercase { get; set; } = true;

        [JsonPropertyName("require_numbers")]
        public bool RequireNumbers { get; set; } = true;

        [JsonPropertyName("require_special_chars")]
        public bool RequireSpecialChars { get; set; } = true;

        [JsonPropertyName("idle_session_timeout_minutes")]
        public int IdleSessionTimeoutMinutes { get; set; } = 30;

        [JsonPropertyName("max_failed_login_attempts")]
        public int MaxFailedLoginAttempts { get; set; } = 5;

        [JsonPropertyName("account_lockout_duration_minutes")]
        public int AccountLockoutDurationMinutes { get; set; } = 30;
    }

    public class UserLoginHistoryDto
    {
        [JsonPropertyName("id")]
        public long Id { get; set; }

        [JsonPropertyName("user_id")]
        public long? UserId { get; set; }

        [JsonPropertyName("email")]
        public string Email { get; set; } = string.Empty;

        [JsonPropertyName("user_name")]
        public string? UserName { get; set; }

        [JsonPropertyName("login_time")]
        public DateTime LoginTime { get; set; }

        [JsonPropertyName("logout_time")]
        public DateTime? LogoutTime { get; set; }

        [JsonPropertyName("ip_address")]
        public string? IpAddress { get; set; }

        [JsonPropertyName("user_agent")]
        public string? UserAgent { get; set; }

        [JsonPropertyName("browser")]
        public string? Browser { get; set; }

        [JsonPropertyName("operating_system")]
        public string? OperatingSystem { get; set; }

        [JsonPropertyName("status")]
        public string Status { get; set; } = "Success";

        [JsonPropertyName("failure_reason")]
        public string? FailureReason { get; set; }
    }

    public class SecurityAuditLogDto
    {
        [JsonPropertyName("id")]
        public long Id { get; set; }

        [JsonPropertyName("event_type")]
        public string EventType { get; set; } = string.Empty;

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("performed_by")]
        public string PerformedBy { get; set; } = string.Empty;

        [JsonPropertyName("target_user")]
        public string? TargetUser { get; set; }

        [JsonPropertyName("ip_address")]
        public string? IpAddress { get; set; }

        [JsonPropertyName("user_agent")]
        public string? UserAgent { get; set; }

        [JsonPropertyName("created_at")]
        public DateTime CreatedAt { get; set; }
    }
}
