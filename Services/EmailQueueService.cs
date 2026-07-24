using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using DeskGuardBackend.Data;
using DeskGuardBackend.Entities;
using DeskGuardBackend.Services.Interfaces;

namespace DeskGuardBackend.Services
{
    public class EmailQueueService : BackgroundService, IEmailQueueService
    {
        private readonly Channel<EmailWorkItem> _channel;
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<EmailQueueService> _logger;

        public EmailQueueService(
            IServiceProvider serviceProvider,
            ILogger<EmailQueueService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
            _channel = Channel.CreateUnbounded<EmailWorkItem>(new UnboundedChannelOptions
            {
                SingleReader = true
            });
        }

        public void QueueEmail(Alert alert, string recipientEmail)
        {
            if (alert == null || string.IsNullOrWhiteSpace(recipientEmail)) return;

            var item = new EmailWorkItem
            {
                Alert = alert,
                RecipientEmail = recipientEmail.Trim(),
                CompanyId = alert.CompanyId
            };

            _channel.Writer.TryWrite(item);
            _logger.LogDebug("Queued email work item for recipient {Recipient} (Alert: {Title})", recipientEmail, alert.Title);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Email Queue Background Worker started");

            while (await _channel.Reader.WaitToReadAsync(stoppingToken))
            {
                while (_channel.Reader.TryRead(out var item))
                {
                    if (stoppingToken.IsCancellationRequested) break;

                    await ProcessEmailItemAsync(item, stoppingToken);
                }
            }

            _logger.LogInformation("Email Queue Background Worker stopped");
        }

        private async Task ProcessEmailItemAsync(EmailWorkItem item, CancellationToken cancellationToken)
        {
            using var scope = _serviceProvider.CreateScope();
            var smtpService = scope.ServiceProvider.GetRequiredService<ISmtpEmailService>();
            var dbContext = scope.ServiceProvider.GetRequiredService<DeskGuardDbContext>();

            var emailLog = new EmailLog
            {
                CompanyId = item.CompanyId,
                AlertId = item.Alert.Id,
                MachineId = item.Alert.MachineId,
                RecipientEmail = item.RecipientEmail,
                Subject = $"[{item.Alert.Severity.ToUpperInvariant()}] {item.Alert.Title}",
                Status = "queued",
                RetryCount = 0,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await dbContext.EmailLogs.AddAsync(emailLog, cancellationToken);
            await dbContext.SaveChangesAsync(cancellationToken);

            int maxRetries = 3;
            int retryDelayMs = 5000;
            int attempts = 0;
            bool success = false;
            string? lastError = null;

            while (attempts < maxRetries && !success && !cancellationToken.IsCancellationRequested)
            {
                attempts++;
                try
                {
                    await smtpService.SendAlertEmailAsync(item.Alert, item.RecipientEmail);
                    success = true;
                    emailLog.Status = "sent";
                    emailLog.SentAt = DateTime.UtcNow;
                    emailLog.SmtpResponse = "250 OK - Message accepted for delivery";
                    emailLog.RetryCount = attempts - 1;
                    emailLog.UpdatedAt = DateTime.UtcNow;

                    await dbContext.SaveChangesAsync(cancellationToken);
                    _logger.LogInformation("Successfully sent email to {Recipient} (Attempt {Attempt})", item.RecipientEmail, attempts);
                }
                catch (Exception ex)
                {
                    lastError = ex.Message;
                    emailLog.RetryCount = attempts;
                    emailLog.FailureReason = ex.Message;
                    emailLog.UpdatedAt = DateTime.UtcNow;

                    _logger.LogWarning(ex, "Failed attempt {Attempt}/{Max} sending email to {Recipient}", attempts, maxRetries, item.RecipientEmail);

                    if (attempts < maxRetries)
                    {
                        await Task.Delay(retryDelayMs, cancellationToken);
                    }
                }
            }

            if (!success)
            {
                emailLog.Status = "failed";
                emailLog.FailureReason = lastError ?? "Max retries exceeded";
                emailLog.UpdatedAt = DateTime.UtcNow;
                await dbContext.SaveChangesAsync(cancellationToken);

                _logger.LogError("Permanently failed to send email to {Recipient} after {Max} attempts", item.RecipientEmail, maxRetries);
            }
        }
    }
}
