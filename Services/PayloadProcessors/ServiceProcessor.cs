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
    public class ServiceProcessor : IPayloadProcessor
    {
        private readonly DeskGuardDbContext _dbContext;
        private readonly ILogger<ServiceProcessor> _logger;

        public ServiceProcessor(DeskGuardDbContext dbContext, ILogger<ServiceProcessor> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        public async Task ProcessAsync(Machine machine, JsonElement payload, HealthLog healthLog)
        {
            try
            {
                var servicesProp = payload.GetPropertyOrNull("services");
                if (servicesProp == null || servicesProp.Value.ValueKind != JsonValueKind.Array) return;

                var existingServices = await _dbContext.WindowsServices
                    .Where(s => s.MachineId == machine.Id)
                    .ToListAsync();

                foreach (var svc in servicesProp.Value.EnumerateArray())
                {
                    var serviceName = svc.GetStringProperty("serviceName") ?? svc.GetStringProperty("service_name");
                    if (string.IsNullOrEmpty(serviceName)) continue;

                    var dbSvc = existingServices.FirstOrDefault(s => s.ServiceName == serviceName);
                    if (dbSvc == null)
                    {
                        dbSvc = new WindowsService { MachineId = machine.Id, ServiceName = serviceName };
                        await _dbContext.WindowsServices.AddAsync(dbSvc);
                    }

                    dbSvc.DisplayName = svc.GetStringProperty("displayName") ?? svc.GetStringProperty("display_name");
                    dbSvc.Status = svc.GetStringProperty("status");
                    dbSvc.StartType = svc.GetStringProperty("startType") ?? svc.GetStringProperty("start_type");
                    dbSvc.ServiceType = svc.GetStringProperty("serviceType") ?? svc.GetStringProperty("service_type");
                    dbSvc.UpdatedAt = DateTime.UtcNow;
                }

                await _dbContext.SaveChangesAsync();
                _logger.LogDebug("ServiceProcessor: Processed services for machine {MachineId}", machine.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ServiceProcessor: Failed to process services for machine {MachineId}", machine.Id);
            }
        }
    }
}
