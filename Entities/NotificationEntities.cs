using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DeskGuardBackend.Entities
{
    /// <summary>
    /// SMTP Server Configuration entity.
    /// Maps to 'smtp_configurations' table.
    /// Password is stored as AES-256 encrypted string.
    /// </summary>
    public class SmtpConfiguration
    {
        public long Id { get; set; }
        public long? CompanyId { get; set; }
        
        [Required]
        [MaxLength(255)]
        public string Host { get; set; } = string.Empty;

        public int Port { get; set; } = 587;

        [MaxLength(255)]
        public string Username { get; set; } = string.Empty;

        /// <summary>AES-256 encrypted password.</summary>
        public string EncryptedPassword { get; set; } = string.Empty;

        public bool EnableSsl { get; set; } = true;

        /// <summary>TLS, SSL, None.</summary>
        [MaxLength(50)]
        public string EncryptionType { get; set; } = "TLS";

        [Required]
        [MaxLength(255)]
        public string FromEmail { get; set; } = string.Empty;

        [MaxLength(255)]
        public string FromName { get; set; } = "DeskGuard Monitoring System";

        public int TimeoutSeconds { get; set; } = 15;
        public int RetryCount { get; set; } = 3;
        public int RetryDelaySeconds { get; set; } = 5;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        public Company? Company { get; set; }
    }

    /// <summary>
    /// Event notification rule mapping which alert types trigger email dispatches.
    /// Maps to 'notification_rules' table.
    /// </summary>
    public class NotificationRule
    {
        public long Id { get; set; }
        public long? CompanyId { get; set; }

        /// <summary>Category: Critical Alerts, Security Events, Change Detection Events.</summary>
        [Required]
        [MaxLength(100)]
        public string Category { get; set; } = "Critical Alerts";

        /// <summary>Event key: cpu_critical, ram_critical, disk_critical, firewall_disabled, antivirus_disabled, etc.</summary>
        [Required]
        [MaxLength(100)]
        public string EventType { get; set; } = string.Empty;

        [Required]
        [MaxLength(255)]
        public string DisplayName { get; set; } = string.Empty;

        public bool SendEmail { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        public Company? Company { get; set; }
    }

    /// <summary>
    /// Audit and delivery tracking log for outgoing notification emails.
    /// Maps to 'email_logs' table.
    /// </summary>
    public class EmailLog
    {
        public long Id { get; set; }
        public long? CompanyId { get; set; }
        public long? AlertId { get; set; }
        public long? MachineId { get; set; }

        [Required]
        [MaxLength(255)]
        public string RecipientEmail { get; set; } = string.Empty;

        [Required]
        [MaxLength(255)]
        public string Subject { get; set; } = string.Empty;

        /// <summary>queued, sent, failed.</summary>
        [Required]
        [MaxLength(50)]
        public string Status { get; set; } = "queued";

        public DateTime? SentAt { get; set; }
        public string? FailureReason { get; set; }
        public int RetryCount { get; set; }
        public string? SmtpResponse { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        public Company? Company { get; set; }
        public Alert? Alert { get; set; }
        public Machine? Machine { get; set; }
    }
}
