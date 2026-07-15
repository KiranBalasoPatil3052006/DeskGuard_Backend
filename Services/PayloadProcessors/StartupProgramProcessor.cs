using System;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using DeskGuardBackend.Data;
using DeskGuardBackend.Entities;
using DeskGuardBackend.Extensions;

namespace DeskGuardBackend.Services.PayloadProcessors
{
    public class StartupProgramProcessor : IPayloadProcessor
    {
        private readonly DeskGuardDbContext _dbContext;
        private readonly ILogger<StartupProgramProcessor> _logger;

        public StartupProgramProcessor(DeskGuardDbContext dbContext, ILogger<StartupProgramProcessor> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        public async Task ProcessAsync(Machine machine, JsonElement payload, HealthLog healthLog)
        {
            try
            {
                var startupProp = payload.GetPropertyOrNull("startupPrograms");
                if (startupProp == null || startupProp.Value.ValueKind != JsonValueKind.Array) return;

                var existingPrograms = await _dbContext.StartupPrograms
                    .Where(s => s.MachineId == machine.Id)
                    .ToListAsync();

                foreach (var prog in startupProp.Value.EnumerateArray())
                {
                    var name = prog.GetStringProperty("processName") ?? prog.GetStringProperty("process_name") ?? prog.GetStringProperty("name");
                    if (string.IsNullOrEmpty(name)) continue;

                    var dbProg = existingPrograms.FirstOrDefault(s => s.Name == name);
                    if (dbProg == null)
                    {
                        dbProg = new StartupProgram { MachineId = machine.Id, Name = name };
                        await _dbContext.StartupPrograms.AddAsync(dbProg);
                    }

                    dbProg.Command = prog.GetStringProperty("executablePath") ?? prog.GetStringProperty("executable_path") ?? prog.GetStringProperty("command");
                    dbProg.User = prog.GetStringProperty("userName") ?? prog.GetStringProperty("user_name") ?? prog.GetStringProperty("user");
                    dbProg.Status = prog.GetStringProperty("status") ?? "Enabled";
                    dbProg.Location = prog.GetStringProperty("location");
                    dbProg.RegistryKey = prog.GetStringProperty("registryKey") ?? prog.GetStringProperty("registry_key");
                    dbProg.UpdatedAt = DateTime.UtcNow;
                }

                await _dbContext.SaveChangesAsync();
                _logger.LogDebug("StartupProgramProcessor: Processed startup programs for machine {MachineId}", machine.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "StartupProgramProcessor: Failed to process startup programs for machine {MachineId}", machine.Id);
            }
        }
    }
}
