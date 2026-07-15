using System.Text.Json;
using System.Threading.Tasks;
using DeskGuardBackend.Entities;

namespace DeskGuardBackend.Services.PayloadProcessors
{
    public interface IPayloadProcessor
    {
        Task ProcessAsync(Machine machine, JsonElement payload, HealthLog healthLog);
    }
}
