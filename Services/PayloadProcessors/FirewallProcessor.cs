using System;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using DeskGuardBackend.Data;
using DeskGuardBackend.Entities;
using DeskGuardBackend.Extensions;

namespace DeskGuardBackend.Services.PayloadProcessors
{
    public class FirewallProcessor : IPayloadProcessor
    {
        private readonly DeskGuardDbContext _dbContext;
        private readonly ILogger<FirewallProcessor> _logger;

        public FirewallProcessor(DeskGuardDbContext dbContext, ILogger<FirewallProcessor> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        public async Task ProcessAsync(Machine machine, JsonElement payload, HealthLog healthLog)
        {
            try
            {
                var fwProp = payload.GetPropertyOrNull("firewall");
                if (fwProp?.ValueKind != JsonValueKind.Object) return;

                var fw = fwProp.Value;

                var isDomain = fw.GetBooleanProperty("isDomainFirewallEnabled") ?? fw.GetBooleanProperty("is_domain_firewall_enabled");
                var isPrivate = fw.GetBooleanProperty("isPrivateFirewallEnabled") ?? fw.GetBooleanProperty("is_private_firewall_enabled");
                var isPublic = fw.GetBooleanProperty("isPublicFirewallEnabled") ?? fw.GetBooleanProperty("is_public_firewall_enabled");
                var activeProfile = fw.GetStringProperty("activeProfile") ?? fw.GetStringProperty("active_profile");

                // Upsert FirewallStatus table
                var existing = await _dbContext.FirewallStatuses
                    .FirstOrDefaultAsync(s => s.MachineId == machine.Id);

                if (existing == null)
                {
                    existing = new FirewallStatus { MachineId = machine.Id };
                    await _dbContext.FirewallStatuses.AddAsync(existing);
                }

                existing.DisplayName = "Windows Firewall";
                existing.IsDomainFirewallEnabled = isDomain;
                existing.IsPrivateFirewallEnabled = isPrivate;
                existing.IsPublicFirewallEnabled = isPublic;
                existing.ActiveProfile = activeProfile;
                existing.UpdatedAt = DateTime.UtcNow;

                // Update MachineCurrentStatus firewall field
                var currentStatus = await _dbContext.MachineCurrentStatuses
                    .FirstOrDefaultAsync(s => s.MachineId == machine.Id);

                if (currentStatus == null)
                {
                    currentStatus = new MachineCurrentStatus { MachineId = machine.Id };
                    await _dbContext.MachineCurrentStatuses.AddAsync(currentStatus);
                }

                currentStatus.FirewallEnabled = isDomain == true || isPrivate == true || isPublic == true;
                currentStatus.CollectedAt = DateTime.UtcNow;

                await _dbContext.SaveChangesAsync();

                _logger.LogDebug("FirewallProcessor: Processed firewall status for machine {MachineId}: Domain={Domain}, Private={Private}, Public={Public}",
                    machine.Id, isDomain, isPrivate, isPublic);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "FirewallProcessor: Failed to process firewall data for machine {MachineId}", machine.Id);
            }
        }
    }
}
