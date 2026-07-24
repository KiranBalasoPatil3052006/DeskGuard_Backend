using System.Threading.Tasks;
using DeskGuardBackend.Entities;

namespace DeskGuardBackend.Services.Interfaces
{
    public class EmailWorkItem
    {
        public Alert Alert { get; set; } = null!;
        public string RecipientEmail { get; set; } = string.Empty;
        public long CompanyId { get; set; }
    }

    public interface IEmailQueueService
    {
        void QueueEmail(Alert alert, string recipientEmail);
    }
}
