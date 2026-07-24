using System;

namespace DeskGuardBackend.Entities
{
    /// <summary>
    /// System security settings configuration. Maps to 'security_settings' table in PostgreSQL.
    /// </summary>
    public class SecuritySetting
    {
        public long Id { get; set; }
        public long? CompanyId { get; set; }
        public int MinPasswordLength { get; set; } = 6;
        public bool RequireUppercase { get; set; } = true;
        public bool RequireLowercase { get; set; } = true;
        public bool RequireNumbers { get; set; } = true;
        public bool RequireSpecialChars { get; set; } = true;
        public int IdleSessionTimeoutMinutes { get; set; } = 30; // 15, 30, 60, 120, 0 = Never
        public int MaxFailedLoginAttempts { get; set; } = 5; // 3, 5, 10, 0 = Unlimited
        public int AccountLockoutDurationMinutes { get; set; } = 30;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public Company? Company { get; set; }
    }

    /// <summary>
    /// Web user login history log. Maps to 'user_login_histories' table in PostgreSQL.
    /// </summary>
    public class UserLoginHistory
    {
        public long Id { get; set; }
        public long? UserId { get; set; }
        public long? CompanyId { get; set; }
        public string Email { get; set; } = string.Empty;
        public DateTime LoginTime { get; set; } = DateTime.UtcNow;
        public DateTime? LogoutTime { get; set; }
        public string? IpAddress { get; set; }
        public string? UserAgent { get; set; }
        public string? Browser { get; set; }
        public string? OperatingSystem { get; set; }
        public string Status { get; set; } = "Success"; // "Success" or "Failed"
        public string? FailureReason { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public User? User { get; set; }
    }
}
