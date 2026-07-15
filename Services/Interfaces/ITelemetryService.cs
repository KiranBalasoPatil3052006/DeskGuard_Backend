using System.Text.Json;
using System.Threading.Tasks;

namespace DeskGuardBackend.Services.Interfaces
{
    public interface ITelemetryService
    {
        Task ProcessTelemetryAsync(JsonElement payload, string sourceIp);
    }
}
