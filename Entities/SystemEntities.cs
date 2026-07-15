namespace DeskGuardBackend.Entities
{
    /// <summary>
    /// Generated report record. Maps to 'reports' table.
    /// </summary>
    public class Report
    {
        public long Id { get; set; }
        public long? CompanyId { get; set; }
        public long? GeneratedBy { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? ReportType { get; set; }
        public string? Format { get; set; }
        public string? FilePath { get; set; }
        public string? Status { get; set; }
        public string? Parameters { get; set; } // JSON
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public Company? Company { get; set; }
        public User? Generator { get; set; }
    }

    /// <summary>
    /// In-app notification record. Maps to 'notifications' table.
    /// </summary>
    public class Notification
    {
        public long Id { get; set; }
        public long? CompanyId { get; set; }
        public long? UserId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Message { get; set; }
        public string? Type { get; set; }
        public string? ReferenceType { get; set; }
        public long? ReferenceId { get; set; }
        public bool IsRead { get; set; }
        public DateTime? ReadAt { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public User? User { get; set; }
    }

    /// <summary>
    /// System audit log entry. Maps to 'audit_logs' table.
    /// </summary>
    public class AuditLog
    {
        public long Id { get; set; }
        public long? UserId { get; set; }
        public long? MachineId { get; set; }
        public long? CompanyId { get; set; }
        public string EventType { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? OldValues { get; set; } // JSON
        public string? NewValues { get; set; } // JSON
        public string? IpAddress { get; set; }
        public string? UserAgent { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public User? User { get; set; }
        public Machine? Machine { get; set; }
    }

    /// <summary>
    /// Email recipient for alert notifications. Maps to 'email_recipients' table.
    /// </summary>
    public class EmailRecipient
    {
        public long Id { get; set; }
        public long CompanyId { get; set; }
        public string Email { get; set; } = string.Empty;
        public string? Name { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public Company Company { get; set; } = null!;
    }

    /// <summary>
    /// Mobile OTP code for agent authentication. Maps to 'otp_codes' table.
    /// </summary>
    public class OtpCode
    {
        public long Id { get; set; }
        public string MobileNumber { get; set; } = string.Empty;
        public string Otp { get; set; } = string.Empty;
        public bool IsUsed { get; set; }
        public DateTime? UsedAt { get; set; }
        public DateTime ExpiresAt { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Raw agent payload log for debugging and audit. Maps to 'raw_payload_logs' table.
    /// </summary>
    public class RawPayloadLog
    {
        public long Id { get; set; }
        public long? MachineId { get; set; }
        public string? MachineUid { get; set; }
        public string? Payload { get; set; } // Full JSON payload
        public string? SourceIp { get; set; }
        public DateTime? ReceivedAt { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Sanctum-equivalent personal access token. Maps to 'personal_access_tokens' table.
    /// Used for JWT token tracking and revocation.
    /// </summary>
    public class PersonalAccessToken
    {
        public long Id { get; set; }
        public string TokenableType { get; set; } = string.Empty;
        public long TokenableId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Token { get; set; } = string.Empty;
        public string? Abilities { get; set; }
        public DateTime? LastUsedAt { get; set; }
        public DateTime? ExpiresAt { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }

    /// <summary>Role entity for RBAC. Maps to 'roles' table.</summary>
    public class Role
    {
        public long Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string GuardName { get; set; } = "web";
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
        public ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
    }

    /// <summary>Permission entity for RBAC. Maps to 'permissions' table.</summary>
    public class Permission
    {
        public long Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string GuardName { get; set; } = "web";
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
    }

    /// <summary>Many-to-many join: User ↔ Role. Maps to 'model_has_roles'.</summary>
    public class UserRole
    {
        public long RoleId { get; set; }
        public long UserId { get; set; }
        public string ModelType { get; set; } = "App\\Models\\User";
        public Role Role { get; set; } = null!;
        public User User { get; set; } = null!;
    }

    /// <summary>Many-to-many join: Role ↔ Permission. Maps to 'role_has_permissions'.</summary>
    public class RolePermission
    {
        public long PermissionId { get; set; }
        public long RoleId { get; set; }
        public Permission Permission { get; set; } = null!;
        public Role Role { get; set; } = null!;
    }
}
