using System.Threading.Tasks;
using DeskGuardBackend.DTOs.Notification;
using DeskGuardBackend.Entities;

namespace DeskGuardBackend.Services.Interfaces
{
    public interface ISmtpEmailService
    {
        Task<SmtpConfigDto> GetSmtpConfigAsync(long companyId);
        Task<SmtpConfigDto> UpdateSmtpConfigAsync(long companyId, UpdateSmtpConfigRequest request);
        Task<(bool Success, string Message)> TestSmtpConnectionAsync(long companyId, TestSmtpConnectionRequest request);
        Task SendAlertEmailAsync(Alert alert, string recipientEmail);
        string EncryptPassword(string password);
        string DecryptPassword(string cipherText);
    }
}
