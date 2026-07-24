using System;
using System.IO;
using System.Net;
using System.Net.Mail;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using DeskGuardBackend.Data;
using DeskGuardBackend.DTOs.Notification;
using DeskGuardBackend.Entities;
using DeskGuardBackend.Services.Interfaces;

namespace DeskGuardBackend.Services
{
    public class SmtpEmailService : ISmtpEmailService
    {
        private readonly DeskGuardDbContext _dbContext;
        private readonly ILogger<SmtpEmailService> _logger;
        private readonly byte[] _encryptionKey;

        public SmtpEmailService(
            DeskGuardDbContext dbContext,
            IConfiguration configuration,
            ILogger<SmtpEmailService> logger)
        {
            _dbContext = dbContext;
            _logger = logger;

            var secretKey = configuration["JwtSettings:Secret"] ?? "DeskGuard_Enterprise_SMTP_Encryption_Secret_Key_2026";
            using var sha = SHA256.Create();
            _encryptionKey = sha.ComputeHash(Encoding.UTF8.GetBytes(secretKey));
        }

        public async Task<SmtpConfigDto> GetSmtpConfigAsync(long companyId)
        {
            var config = await _dbContext.SmtpConfigurations
                .FirstOrDefaultAsync(s => s.CompanyId == companyId || s.CompanyId == null);

            if (config == null)
            {
                return new SmtpConfigDto
                {
                    Host = "",
                    Port = 587,
                    Username = "",
                    IsPasswordSet = false,
                    EnableSsl = true,
                    EncryptionType = "TLS",
                    FromEmail = "alerts@company.com",
                    FromName = "DeskGuard Monitoring System",
                    TimeoutSeconds = 15,
                    RetryCount = 3,
                    RetryDelaySeconds = 5
                };
            }

            return MapToDto(config);
        }

        public async Task<SmtpConfigDto> UpdateSmtpConfigAsync(long companyId, UpdateSmtpConfigRequest request)
        {
            var config = await _dbContext.SmtpConfigurations
                .FirstOrDefaultAsync(s => s.CompanyId == companyId);

            if (config == null)
            {
                config = new SmtpConfiguration
                {
                    CompanyId = companyId,
                    CreatedAt = DateTime.UtcNow
                };
                await _dbContext.SmtpConfigurations.AddAsync(config);
            }

            config.Host = request.Host?.Trim() ?? "";
            config.Port = request.Port;
            config.Username = request.Username?.Trim() ?? "";
            config.EnableSsl = request.EnableSsl;
            config.EncryptionType = string.IsNullOrWhiteSpace(request.EncryptionType) ? "TLS" : request.EncryptionType.Trim();
            config.FromEmail = string.IsNullOrWhiteSpace(request.FromEmail) ? "alerts@company.com" : request.FromEmail.Trim();
            config.FromName = string.IsNullOrWhiteSpace(request.FromName) ? "DeskGuard Monitoring System" : request.FromName.Trim();
            config.TimeoutSeconds = Math.Max(5, request.TimeoutSeconds);
            config.RetryCount = Math.Max(1, request.RetryCount);
            config.RetryDelaySeconds = Math.Max(1, request.RetryDelaySeconds);
            config.UpdatedAt = DateTime.UtcNow;

            if (!string.IsNullOrEmpty(request.Password))
            {
                config.EncryptedPassword = EncryptPassword(request.Password);
            }

            await _dbContext.SaveChangesAsync();
            _logger.LogInformation("SMTP Configuration updated for company ID {CompanyId}", companyId);

            return MapToDto(config);
        }

        public async Task<(bool Success, string Message)> TestSmtpConnectionAsync(long companyId, TestSmtpConnectionRequest request)
        {
            try
            {
                string passwordToUse = request.Password ?? string.Empty;
                if (string.IsNullOrEmpty(passwordToUse))
                {
                    var existingConfig = await _dbContext.SmtpConfigurations
                        .FirstOrDefaultAsync(s => s.CompanyId == companyId);
                    if (existingConfig != null && !string.IsNullOrEmpty(existingConfig.EncryptedPassword))
                    {
                        passwordToUse = DecryptPassword(existingConfig.EncryptedPassword);
                    }
                }

                if (string.IsNullOrWhiteSpace(request.Host))
                {
                    return (false, "SMTP Host is required.");
                }

                if (string.IsNullOrWhiteSpace(request.FromEmail))
                {
                    return (false, "From Email is required.");
                }

                using var client = new SmtpClient(request.Host, request.Port)
                {
                    EnableSsl = request.EnableSsl,
                    Timeout = 10000 // 10s for connection test
                };

                if (!string.IsNullOrEmpty(request.Username))
                {
                    client.Credentials = new NetworkCredential(request.Username, passwordToUse);
                }

                if (!string.IsNullOrEmpty(request.TestRecipientEmail))
                {
                    var testMessage = new MailMessage
                    {
                        From = new MailAddress(request.FromEmail, request.FromName),
                        Subject = "DeskGuard System Test Notification",
                        Body = "<p>This is a test notification email from your DeskGuard Monitoring System.</p><p>SMTP configuration verified successfully.</p>",
                        IsBodyHtml = true
                    };
                    testMessage.To.Add(request.TestRecipientEmail);
                    await client.SendMailAsync(testMessage);
                }
                else
                {
                    // Verify connection sending a no-op or lightweight check
                    var testMessage = new MailMessage
                    {
                        From = new MailAddress(request.FromEmail, request.FromName),
                        Subject = "DeskGuard SMTP Handshake Check",
                        Body = "Handshake verification",
                        IsBodyHtml = false
                    };
                    testMessage.To.Add(request.FromEmail);
                    await client.SendMailAsync(testMessage);
                }

                _logger.LogInformation("SMTP test connection successful for host {Host}:{Port}", request.Host, request.Port);
                return (true, "SMTP connection and authentication successful.");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "SMTP test connection failed for host {Host}", request.Host);
                return (false, $"SMTP Connection Failed: {ex.Message}");
            }
        }

        public async Task SendAlertEmailAsync(Alert alert, string recipientEmail)
        {
            var config = await _dbContext.SmtpConfigurations
                .FirstOrDefaultAsync(s => s.CompanyId == alert.CompanyId || s.CompanyId == null);

            if (config == null || string.IsNullOrWhiteSpace(config.Host))
            {
                _logger.LogWarning("No SMTP configuration found for company ID {CompanyId}. Email skipped.", alert.CompanyId);
                return;
            }

            var password = DecryptPassword(config.EncryptedPassword);

            using var client = new SmtpClient(config.Host, config.Port)
            {
                EnableSsl = config.EnableSsl,
                Timeout = config.TimeoutSeconds * 1000
            };

            if (!string.IsNullOrEmpty(config.Username))
            {
                client.Credentials = new NetworkCredential(config.Username, password);
            }

            var machineName = alert.Machine?.Hostname ?? alert.Machine?.DeviceName ?? $"Machine #{alert.MachineId}";
            var htmlBody = BuildAlertHtmlTemplate(alert, machineName);

            var message = new MailMessage
            {
                From = new MailAddress(config.FromEmail, config.FromName),
                Subject = $"[{alert.Severity.ToUpperInvariant()}] {alert.Title} - {machineName}",
                Body = htmlBody,
                IsBodyHtml = true
            };
            message.To.Add(recipientEmail);

            await client.SendMailAsync(message);
            _logger.LogInformation("Alert email successfully dispatched to {Recipient} via SMTP {Host}", recipientEmail, config.Host);
        }

        public string EncryptPassword(string password)
        {
            if (string.IsNullOrEmpty(password)) return string.Empty;

            using var aes = Aes.Create();
            aes.Key = _encryptionKey;
            aes.GenerateIV();

            using var encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
            using var ms = new MemoryStream();
            ms.Write(aes.IV, 0, aes.IV.Length);

            using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
            using (var sw = new StreamWriter(cs))
            {
                sw.Write(password);
            }

            return Convert.ToBase64String(ms.ToArray());
        }

        public string DecryptPassword(string cipherText)
        {
            if (string.IsNullOrEmpty(cipherText)) return string.Empty;

            try
            {
                var fullCipher = Convert.FromBase64String(cipherText);
                using var aes = Aes.Create();
                aes.Key = _encryptionKey;

                var iv = new byte[aes.BlockSize / 8];
                var cipher = new byte[fullCipher.Length - iv.Length];

                Buffer.BlockCopy(fullCipher, 0, iv, 0, iv.Length);
                Buffer.BlockCopy(fullCipher, iv.Length, cipher, 0, cipher.Length);

                aes.IV = iv;

                using var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
                using var ms = new MemoryStream(cipher);
                using var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read);
                using var sr = new StreamReader(cs);

                return sr.ReadToEnd();
            }
            catch
            {
                return string.Empty;
            }
        }

        private static SmtpConfigDto MapToDto(SmtpConfiguration config)
        {
            return new SmtpConfigDto
            {
                Id = config.Id,
                Host = config.Host,
                Port = config.Port,
                Username = config.Username,
                IsPasswordSet = !string.IsNullOrEmpty(config.EncryptedPassword),
                EnableSsl = config.EnableSsl,
                EncryptionType = config.EncryptionType,
                FromEmail = config.FromEmail,
                FromName = config.FromName,
                TimeoutSeconds = config.TimeoutSeconds,
                RetryCount = config.RetryCount,
                RetryDelaySeconds = config.RetryDelaySeconds
            };
        }

        private static string BuildAlertHtmlTemplate(Alert alert, string machineName)
        {
            var severityColor = alert.Severity.ToLowerInvariant() switch
            {
                "critical" => "#DC2626",
                "warning" => "#D97706",
                _ => "#2563EB"
            };

            return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'/>
    <style>
        body {{ font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Helvetica, Arial, sans-serif; background-color: #F3F4F6; margin: 0; padding: 24px; }}
        .container {{ max-width: 600px; margin: 0 auto; background: #FFFFFF; border-radius: 8px; overflow: hidden; box-shadow: 0 4px 6px rgba(0,0,0,0.05); }}
        .header {{ background: #1E293B; color: #FFFFFF; padding: 20px 24px; display: flex; align-items: center; }}
        .header h2 {{ margin: 0; font-size: 20px; font-weight: 700; letter-spacing: -0.5px; }}
        .badge {{ display: inline-block; padding: 4px 12px; border-radius: 9999px; color: #FFFFFF; font-weight: 700; font-size: 12px; text-transform: uppercase; background-color: {severityColor}; }}
        .content {{ padding: 24px; color: #334155; line-height: 1.6; }}
        .metric-box {{ background: #F8FAFC; border: 1px solid #E2E8F0; border-radius: 6px; padding: 16px; margin: 16px 0; }}
        .metric-row {{ display: flex; justify-content: space-between; padding: 6px 0; border-bottom: 1px solid #F1F5F9; font-size: 14px; }}
        .footer {{ background: #F8FAFC; border-top: 1px solid #E2E8F0; padding: 16px 24px; font-size: 12px; color: #64748B; text-align: center; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h2>DeskGuard Endpoint Monitoring</h2>
        </div>
        <div class='content'>
            <div style='display: flex; justify-content: space-between; align-items: center; margin-bottom: 16px;'>
                <h3 style='margin:0; font-size: 18px; color: #0F172A;'>{WebUtility.HtmlEncode(alert.Title)}</h3>
                <span class='badge'>{WebUtility.HtmlEncode(alert.Severity)}</span>
            </div>
            <p>{WebUtility.HtmlEncode(alert.Description)}</p>
            <div class='metric-box'>
                <div class='metric-row'><strong>Machine Host:</strong> <span>{WebUtility.HtmlEncode(machineName)}</span></div>
                {(alert.CurrentValue.HasValue ? $"<div class='metric-row'><strong>Current Value:</strong> <span>{alert.CurrentValue.Value}%</span></div>" : "")}
                {(alert.ThresholdValue.HasValue ? $"<div class='metric-row'><strong>Configured Threshold:</strong> <span>{alert.ThresholdValue.Value}%</span></div>" : "")}
                {(alert.MaxRecordedValue.HasValue ? $"<div class='metric-row'><strong>Peak Recorded:</strong> <span>{alert.MaxRecordedValue.Value}%</span></div>" : "")}
                <div class='metric-row'><strong>First Detected:</strong> <span>{alert.FirstDetectedAt:yyyy-MM-dd HH:mm:ss} UTC</span></div>
                <div class='metric-row'><strong>Hits Count:</strong> <span>{alert.OccurrenceCount}</span></div>
            </div>
            <p style='font-size: 13px; color: #64748B;'>Action Required: Please log in to the DeskGuard console to review and acknowledge this incident.</p>
        </div>
        <div class='footer'>
            DeskGuard Enterprise Endpoint Security & Asset Management Platform
        </div>
    </div>
</body>
</html>";
        }
    }
}
