using System.Text.Json;
using System.Threading.Tasks;
using DeskGuardBackend.Entities;

namespace DeskGuardBackend.Services.Interfaces
{
    public interface IPayloadProcessorService
    {
        Task ProcessAsync(Machine machine, JsonElement payload);
    }
}
