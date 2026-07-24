using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using DeskGuardBackend.DTOs.Common;
using DeskGuardBackend.Entities;
using DeskGuardBackend.Data;
using DeskGuardBackend.Extensions;
using DeskGuardBackend.Services.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace DeskGuardBackend.Controllers
{
    [ApiController]
    [Route("api/v1/health")]
    public class AgentHealthController : ControllerBase
    {
        private readonly DeskGuardDbContext _dbContext;
        private readonly IPayloadProcessorService _payloadProcessorService;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AgentHealthController> _logger;

        public AgentHealthController(
            DeskGuardDbContext dbContext,
            IPayloadProcessorService payloadProcessorService,
            IConfiguration configuration,
            ILogger<AgentHealthController> logger)
        {
            _dbContext = dbContext;
            _payloadProcessorService = payloadProcessorService;
            _configuration = configuration;
            _logger = logger;
        }

        [HttpPost]
        public async Task<IActionResult> HandleHealthPayload([FromBody] JsonElement rawPayload)
        {
            try
            {
                var machineUid = ResolveMachineUid(rawPayload);
                if (string.IsNullOrEmpty(machineUid))
                {
                    return UnprocessableEntity(ApiResponse.Fail("Machine identifier is required."));
                }

                var companyId = await GetOrCreateCompanyIdAsync(rawPayload);

                var systemInfoProp = rawPayload.GetPropertyOrNull("systemInfo");
                var systemInfo = systemInfoProp?.ValueKind == JsonValueKind.Object ? systemInfoProp.Value : default;

                var computerName = rawPayload.GetStringProperty("computerName") ?? systemInfo.GetStringProperty("computerName");

                var machine = await _dbContext.Machines
                    .FirstOrDefaultAsync(m => m.MachineUid == machineUid);

                if (machine == null)
                {
                    machine = new Machine
                    {
                        MachineUid = machineUid,
                        CompanyId = companyId,
                        Hostname = computerName,
                        DeviceName = computerName,
                        OperatingSystem = systemInfo.ValueKind == JsonValueKind.Object ? systemInfo.GetStringProperty("operatingSystem") : null,
                        IsOnline = true,
                        IsActive = true,
                        LastHeartbeatAt = DateTime.UtcNow
                    };
                    await _dbContext.Machines.AddAsync(machine);
                    await _dbContext.SaveChangesAsync();
                }

                // Auto-register machine token from the Authorization header
                var authHeader = HttpContext.Request.Headers["Authorization"].ToString();
                if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                {
                    var apiKey = authHeader.Substring("Bearer ".Length).Trim();
                    if (!string.IsNullOrEmpty(apiKey))
                    {
                        var hashedToken = HashToken(apiKey);
                        var existingToken = await _dbContext.MachineTokens
                            .FirstOrDefaultAsync(t => t.MachineId == machine.Id);

                        if (existingToken == null)
                        {
                            var machineToken = new MachineToken
                            {
                                MachineId = machine.Id,
                                Token = hashedToken,
                                CreatedAt = DateTime.UtcNow,
                                UpdatedAt = DateTime.UtcNow
                            };
                            await _dbContext.MachineTokens.AddAsync(machineToken);
                            await _dbContext.SaveChangesAsync();
                            _logger.LogInformation("Auto-registered machine token for machine {MachineId}", machine.Id);
                        }
                    }
                }

                // Log raw payload
                var rawLog = new RawPayloadLog
                {
                    MachineId = machine.Id,
                    MachineUid = machineUid,
                    Payload = JsonSerializer.Serialize(rawPayload),
                    SourceIp = HttpContext.Connection.RemoteIpAddress?.ToString(),
                    ReceivedAt = DateTime.UtcNow
                };
                await _dbContext.RawPayloadLogs.AddAsync(rawLog);
                await _dbContext.SaveChangesAsync();

                // Normalise the payload format to the structure expected by section processors
                var normalizedPayload = NormalisePayload(rawPayload);

                using var doc = JsonDocument.Parse(JsonSerializer.Serialize(normalizedPayload));
                await _payloadProcessorService.ProcessAsync(machine, doc.RootElement);

                _logger.LogInformation("AgentHealthController: Health payload processed for machine {MachineId}", machine.Id);
                return Ok(ApiResponse.Ok("Health data processed successfully."));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "AgentHealthController: Failed to process health payload");
                return StatusCode(500, ApiResponse.Fail("Failed to process health data."));
            }
        }

        private string ResolveMachineUid(JsonElement payload)
        {
            var candidates = new[]
            {
                payload.GetStringProperty("machineId"),
                payload.GetStringProperty("machine_uid"),
                payload.GetStringProperty("machineUid"),
                payload.GetStringProperty("agentId"),
                Request.Headers["X-Agent-Id"].ToString()
            };

            foreach (var candidate in candidates)
            {
                if (!string.IsNullOrEmpty(candidate)) return candidate;
            }

            return string.Empty;
        }

        private async Task<long> GetOrCreateCompanyIdAsync(JsonElement payload)
        {
            var companyName = payload.GetStringProperty("companyName") ?? payload.GetStringProperty("company_name");
            var customerId = payload.GetStringProperty("customerId") ?? payload.GetStringProperty("customer_id");

            // If customer info provided, find or create a matching company
            if (!string.IsNullOrWhiteSpace(companyName) || !string.IsNullOrWhiteSpace(customerId))
            {
                Company? company = null;

                if (!string.IsNullOrWhiteSpace(customerId))
                {
                    company = await _dbContext.Companies
                        .FirstOrDefaultAsync(c => c.CustomerId == customerId);
                }

                company ??= !string.IsNullOrWhiteSpace(companyName)
                    ? await _dbContext.Companies
                        .FirstOrDefaultAsync(c => c.Name == companyName)
                    : null;

                if (company != null)
                {
                    UpdateCompanyFromPayload(company, payload);
                    company.UpdatedAt = DateTime.UtcNow;
                    await _dbContext.SaveChangesAsync();
                    return company.Id;
                }

                // Create new company
                company = new Company
                {
                    CustomerId = customerId,
                    Name = companyName ?? "Unknown Company",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                UpdateCompanyFromPayload(company, payload);
                await _dbContext.Companies.AddAsync(company);
                await _dbContext.SaveChangesAsync();
                return company.Id;
            }

            // Fallback to default/first company
            var preferredId = _configuration.GetValue<long>("AgentSettings:DefaultCompanyId");
            if (preferredId > 0)
            {
                var company = await _dbContext.Companies.FindAsync(preferredId);
                if (company != null) return company.Id;
            }

            var firstCompany = await _dbContext.Companies.FirstOrDefaultAsync();
            if (firstCompany != null) return firstCompany.Id;

            var newCompany = new Company
            {
                Name = "Default Company",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            await _dbContext.Companies.AddAsync(newCompany);
            await _dbContext.SaveChangesAsync();

            return newCompany.Id;
        }

        private static void UpdateCompanyFromPayload(Company company, JsonElement payload)
        {
            var customerId = payload.GetStringProperty("customerId") ?? payload.GetStringProperty("customer_id");
            var email = payload.GetStringProperty("customerEmail") ?? payload.GetStringProperty("customer_email") ?? payload.GetStringProperty("email");
            var mobile = payload.GetStringProperty("customerMobileNumber") ?? payload.GetStringProperty("customer_mobile_number") ?? payload.GetStringProperty("mobile_number");
            var address = payload.GetStringProperty("customerAddress") ?? payload.GetStringProperty("customer_address") ?? payload.GetStringProperty("address");
            var amcPlan = payload.GetStringProperty("amcPlan") ?? payload.GetStringProperty("amc_plan");
            var amcStartDate = payload.GetStringProperty("amcStartDate") ?? payload.GetStringProperty("amc_start_date");
            var amcEndDate = payload.GetStringProperty("amcEndDate") ?? payload.GetStringProperty("amc_end_date");

            if (!string.IsNullOrWhiteSpace(customerId)) company.CustomerId = customerId;
            if (!string.IsNullOrWhiteSpace(email)) company.Email = email;
            if (!string.IsNullOrWhiteSpace(mobile)) company.Phone = mobile;
            if (!string.IsNullOrWhiteSpace(address)) company.Address = address;
            if (!string.IsNullOrWhiteSpace(amcPlan)) company.AmcPlan = amcPlan;
            if (DateTime.TryParse(amcStartDate, out var startParsed)) company.AmcStartDate = startParsed;
            if (DateTime.TryParse(amcEndDate, out var endParsed)) company.AmcEndDate = endParsed;
        }

        private static string HashToken(string token)
        {
            using var sha256 = SHA256.Create();
            var bytes = Encoding.UTF8.GetBytes(token);
            var hashBytes = sha256.ComputeHash(bytes);
            return Convert.ToHexString(hashBytes).ToLowerInvariant();
        }

        private static Dictionary<string, object?> NormalisePayload(JsonElement payload)
        {
            var systemInfoProp = payload.GetPropertyOrNull("systemInfo");
            var cpuProp = payload.GetPropertyOrNull("cpuInfo");
            var memoryProp = payload.GetPropertyOrNull("memoryInfo");
            var diskProp = payload.GetPropertyOrNull("diskInfo");
            var batteryProp = payload.GetPropertyOrNull("batteryInfo");
            var networkProp = payload.GetPropertyOrNull("networkInfo");
            var antivirusProp = payload.GetPropertyOrNull("antivirusInfo");
            var firewallProp = payload.GetPropertyOrNull("firewallInfo");
            var processProp = payload.GetPropertyOrNull("processInfo");
            var serviceProp = payload.GetPropertyOrNull("serviceInfo");
            var startupProp = payload.GetPropertyOrNull("startupProgramInfo");
            var updateProp = payload.GetPropertyOrNull("updateInfo");
            var eventLogProp = payload.GetPropertyOrNull("eventLogInfo");
            var loginProp = payload.GetPropertyOrNull("loginActivityInfo");
            var usbProp = payload.GetPropertyOrNull("usbActivityInfo");
            var peripheralProp = payload.GetPropertyOrNull("peripheralInfo");

            return new Dictionary<string, object?>
            {
                ["machineId"] = payload.GetStringProperty("machineId"),
                ["collectedAt"] = payload.GetStringProperty("timestamp"),
                ["systemInfo"] = systemInfoProp?.ValueKind == JsonValueKind.Object ? (object)systemInfoProp.Value : null,
                ["cpu"] = cpuProp?.ValueKind == JsonValueKind.Object ? (object)cpuProp.Value : null,
                ["memory"] = memoryProp?.ValueKind == JsonValueKind.Object ? (object)memoryProp.Value : null,
                ["disks"] = diskProp?.ValueKind == JsonValueKind.Array ? (object)diskProp.Value : null,
                ["battery"] = batteryProp?.ValueKind == JsonValueKind.Object ? (object)batteryProp.Value : null,
                ["networkAdapters"] = networkProp?.ValueKind == JsonValueKind.Array ? (object)networkProp.Value : null,
                ["antivirus"] = antivirusProp?.ValueKind == JsonValueKind.Object ? (object)antivirusProp.Value : null,
                ["firewall"] = firewallProp?.ValueKind == JsonValueKind.Object ? (object)firewallProp.Value : null,
                ["processes"] = processProp?.ValueKind == JsonValueKind.Array ? (object)processProp.Value : null,
                ["services"] = serviceProp?.ValueKind == JsonValueKind.Array ? (object)serviceProp.Value : null,
                ["startupPrograms"] = startupProp?.ValueKind == JsonValueKind.Array ? (object)startupProp.Value : null,
                ["updates"] = updateProp?.ValueKind == JsonValueKind.Object ? (object)updateProp.Value : null,
                ["eventLogs"] = eventLogProp?.ValueKind == JsonValueKind.Array ? (object)eventLogProp.Value : null,
                ["loginActivity"] = loginProp?.ValueKind == JsonValueKind.Array ? (object)loginProp.Value : null,
                ["usbActivity"] = usbProp?.ValueKind == JsonValueKind.Array ? (object)usbProp.Value : null,
                ["peripherals"] = peripheralProp?.ValueKind == JsonValueKind.Array ? (object)peripheralProp.Value : null
            };
        }
    }
}
