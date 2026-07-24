using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace DeskGuardBackend.DTOs.Notification
{
    public class SmtpConfigDto
    {
        public long Id { get; set; }
        public string Host { get; set; } = string.Empty;
        public int Port { get; set; } = 587;
        public string Username { get; set; } = string.Empty;
        public bool IsPasswordSet { get; set; }
        public bool EnableSsl { get; set; } = true;
        public string EncryptionType { get; set; } = "TLS";
        public string FromEmail { get; set; } = string.Empty;
        public string FromName { get; set; } = "DeskGuard Monitoring System";
        public int TimeoutSeconds { get; set; } = 15;
        public int RetryCount { get; set; } = 3;
        public int RetryDelaySeconds { get; set; } = 5;
    }

    public class UpdateSmtpConfigRequest
    {
        public string Host { get; set; } = string.Empty;

        public int Port { get; set; } = 587;

        public string Username { get; set; } = string.Empty;

        /// <summary>Optional. If omitted or empty, previous password is retained.</summary>
        public string? Password { get; set; }

        public bool EnableSsl { get; set; } = true;
        public string EncryptionType { get; set; } = "TLS";

        public string FromEmail { get; set; } = string.Empty;

        public string FromName { get; set; } = "DeskGuard Monitoring System";
        public int TimeoutSeconds { get; set; } = 15;
        public int RetryCount { get; set; } = 3;
        public int RetryDelaySeconds { get; set; } = 5;
    }

    public class TestSmtpConnectionRequest
    {
        public string Host { get; set; } = string.Empty;

        public int Port { get; set; } = 587;

        public string Username { get; set; } = string.Empty;

        /// <summary>Raw password to test. If null, stored password is used.</summary>
        public string? Password { get; set; }

        public bool EnableSsl { get; set; } = true;
        public string EncryptionType { get; set; } = "TLS";

        public string FromEmail { get; set; } = string.Empty;

        public string FromName { get; set; } = "DeskGuard System Test";

        public string? TestRecipientEmail { get; set; }
    }

    public class EmailRecipientDto
    {
        public long Id { get; set; }
        public string Email { get; set; } = string.Empty;
        public string? Name { get; set; }
        public string? Department { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; }
    }

    public class CreateEmailRecipientRequest
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        public string? Name { get; set; }
        public string? Department { get; set; }
        public bool IsActive { get; set; } = true;
    }

    public class UpdateEmailRecipientRequest
    {
        [EmailAddress]
        public string? Email { get; set; }
        public string? Name { get; set; }
        public string? Department { get; set; }
        public bool? IsActive { get; set; }
    }

    public class NotificationRuleDto
    {
        public long Id { get; set; }
        public string Category { get; set; } = string.Empty;
        public string EventType { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public bool SendEmail { get; set; } = true;
    }

    public class UpdateNotificationRulesRequest
    {
        public List<NotificationRuleDto> Rules { get; set; } = new List<NotificationRuleDto>();
    }

    public class EmailLogDto
    {
        public long Id { get; set; }
        public string RecipientEmail { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime? SentAt { get; set; }
        public string? FailureReason { get; set; }
        public int RetryCount { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
