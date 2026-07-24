using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using DeskGuardBackend.DTOs.Common;
using DeskGuardBackend.Services.Interfaces;
using DeskGuardBackend.Data;
using DeskGuardBackend.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Linq;
using System.Text.Json;

namespace DeskGuardBackend.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/v1/machines")]
    public class MachineController : ControllerBase
    {
        private readonly IMachineService _machineService;
        private readonly DeskGuardDbContext _dbContext;
        private readonly ILogger<MachineController> _logger;

        public MachineController(
            IMachineService machineService,
            DeskGuardDbContext dbContext,
            ILogger<MachineController> logger)
        {
            _machineService = machineService;
            _dbContext = dbContext;
            _logger = logger;
        }

        private long GetCompanyId()
        {
            var compIdStr = User.FindFirst("CompanyId")?.Value;
            if (string.IsNullOrEmpty(compIdStr) || !long.TryParse(compIdStr, out var companyId))
            {
                return 1; // Fallback to company ID 1 for dev
            }
            return companyId;
        }

        [HttpGet]
        public async Task<IActionResult> Index([FromQuery] int page = 1, [FromQuery] int per_page = 15, [FromQuery] string? status = null, [FromQuery] string? search = null)
        {
            try
            {
                var companyId = GetCompanyId();
                var result = await _machineService.GetCompanyMachinesAsync(companyId, page, per_page, status, search);
                return Ok(result); // Return the LengthAwarePaginator structure directly
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get company machines");
                return StatusCode(500, ApiResponse.Fail("Failed to retrieve machines."));
            }
        }

        [HttpGet("online")]
        public async Task<IActionResult> Online()
        {
            var companyId = GetCompanyId();
            var count = await _machineService.GetOnlineCountAsync(companyId);
            return Ok(ApiResponse<object>.Ok(new { count = count }));
        }

        [HttpGet("offline")]
        public async Task<IActionResult> Offline()
        {
            var companyId = GetCompanyId();
            var count = await _machineService.GetOfflineCountAsync(companyId);
            return Ok(ApiResponse<object>.Ok(new { count = count }));
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> Show(long id)
        {
            try
            {
                var machineEntity = await _dbContext.Machines
                    .AsNoTracking()
                    .Include(m => m.CurrentStatus)
                    .Include(m => m.AssignedUser)
                    .Include(m => m.Company)
                    .FirstOrDefaultAsync(m => m.Id == id);

                if (machineEntity == null) return NotFound(ApiResponse.Fail("Machine not found."));

                var currentStatus = machineEntity.CurrentStatus;

                object? networkInterfaces = null;
                if (currentStatus?.NetworkInterfaces != null)
                {
                    try
                    {
                        networkInterfaces = JsonSerializer.Deserialize<object>(currentStatus.NetworkInterfaces);
                    }
                    catch (JsonException)
                    {
                        networkInterfaces = null;
                    }
                }

                var pendingUpdateCount = await _dbContext.WindowsUpdates
                    .AsNoTracking()
                    .CountAsync(w => w.MachineId == id && !w.IsInstalled);

                object? statusDto = currentStatus != null ? new
                {
                    // Primary field names (new standard)
                    cpu_percentage = currentStatus.CpuPercentage,
                    cpu_temperature = currentStatus.CpuTemperature,
                    cpu_clock_speed = currentStatus.CpuClockSpeed,
                    cpu_core_count = currentStatus.CpuCoreCount,
                    ram_total_bytes = currentStatus.RamTotalBytes,
                    ram_used_bytes = currentStatus.RamUsedBytes,
                    ram_available_bytes = currentStatus.RamAvailableBytes,
                    ram_percentage = currentStatus.RamPercentage,
                    disk_total_bytes = currentStatus.DiskTotalBytes,
                    disk_used_bytes = currentStatus.DiskUsedBytes,
                    disk_free_bytes = currentStatus.DiskFreeBytes,
                    disk_percentage = currentStatus.DiskPercentage,
                    disk_health_status = currentStatus.DiskHealthStatus,
                    battery_percentage = currentStatus.BatteryPercentage,
                    battery_charging_status = currentStatus.BatteryChargingStatus,
                    battery_wear_level = currentStatus.BatteryWearLevel,
                    network_received_bytes = currentStatus.NetworkReceivedBytes,
                    network_sent_bytes = currentStatus.NetworkSentBytes,
                    network_interfaces = networkInterfaces,
                    antivirus_status = currentStatus.AntivirusEnabled == true ? "Enabled" : (currentStatus.AntivirusEnabled == false ? "Disabled" : null),
                    antivirus_name = currentStatus.AntivirusName,
                    firewall_status = currentStatus.FirewallEnabled == true ? "Enabled" : (currentStatus.FirewallEnabled == false ? "Disabled" : null),
                    pending_updates = pendingUpdateCount,
                    // Backward-compatible aliases for frontend
                    cpu_usage = currentStatus.CpuPercentage,
                    memory_usage = currentStatus.RamPercentage,
                    disk_usage = currentStatus.DiskPercentage,
                    cpu_temp = currentStatus.CpuTemperature,
                    battery_level = currentStatus.BatteryPercentage
                } : null;

                var machine = new
                {
                    id = machineEntity.Id,
                    company_id = machineEntity.CompanyId,
                    user_id = machineEntity.UserId,
                    machine_uid = machineEntity.MachineUid,
                    hostname = machineEntity.Hostname,
                    device_name = machineEntity.DeviceName,
                    operating_system = machineEntity.OperatingSystem,
                    os_version = machineEntity.OsVersion,
                    manufacturer = machineEntity.Manufacturer,
                    model = machineEntity.Model,
                    serial_number = machineEntity.SerialNumber,
                    bios_version = machineEntity.BiosVersion,
                    processor = machineEntity.Processor,
                    ram_gb = machineEntity.RamGb,
                    is_online = machineEntity.IsOnline,
                    last_heartbeat_at = machineEntity.LastHeartbeatAt,
                    is_active = machineEntity.IsActive,
                    employee_mobile_number = machineEntity.EmployeeMobileNumber,
                    created_at = machineEntity.CreatedAt,
                    updated_at = machineEntity.UpdatedAt,
                    company = machineEntity.Company != null ? new { machineEntity.Company.Name } : null,
                    current_user = machineEntity.AssignedUser != null ? machineEntity.AssignedUser.Name : null,
                    current_status = statusDto
                };

                return Ok(ApiResponse<object>.Ok(machine));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to show machine ID {Id}", id);
                return StatusCode(500, ApiResponse.Fail("Failed to retrieve machine details."));
            }
        }

        [HttpGet("{id}/status")]
        public async Task<IActionResult> Status(long id)
        {
            try
            {
                var status = await _dbContext.MachineCurrentStatuses
                    .AsNoTracking()
                    .FirstOrDefaultAsync(s => s.MachineId == id);

                var pendingUpdateCount = await _dbContext.WindowsUpdates
                    .AsNoTracking()
                    .CountAsync(w => w.MachineId == id && !w.IsInstalled);

                object? networkInterfaces = null;
                if (status?.NetworkInterfaces != null)
                {
                    try
                    {
                        networkInterfaces = JsonSerializer.Deserialize<object>(status.NetworkInterfaces);
                    }
                    catch (JsonException)
                    {
                        networkInterfaces = null;
                    }
                }

                object? dto = status != null ? new
                {
                    // Primary field names (new standard)
                    cpu_percentage = status.CpuPercentage,
                    cpu_temperature = status.CpuTemperature,
                    cpu_clock_speed = status.CpuClockSpeed,
                    cpu_core_count = status.CpuCoreCount,
                    ram_total_bytes = status.RamTotalBytes,
                    ram_used_bytes = status.RamUsedBytes,
                    ram_available_bytes = status.RamAvailableBytes,
                    ram_percentage = status.RamPercentage,
                    disk_total_bytes = status.DiskTotalBytes,
                    disk_used_bytes = status.DiskUsedBytes,
                    disk_free_bytes = status.DiskFreeBytes,
                    disk_percentage = status.DiskPercentage,
                    disk_health_status = status.DiskHealthStatus,
                    battery_percentage = status.BatteryPercentage,
                    battery_charging_status = status.BatteryChargingStatus,
                    battery_wear_level = status.BatteryWearLevel,
                    network_received_bytes = status.NetworkReceivedBytes,
                    network_sent_bytes = status.NetworkSentBytes,
                    network_interfaces = networkInterfaces,
                    antivirus_name = status.AntivirusName,
                    antivirus_status = status.AntivirusEnabled == true ? "Enabled" : (status.AntivirusEnabled == false ? "Disabled" : null),
                    firewall_status = status.FirewallEnabled == true ? "Enabled" : (status.FirewallEnabled == false ? "Disabled" : null),
                    pending_updates = pendingUpdateCount,
                    online_status = status.OnlineStatus,
                    last_collected_at = status.LastCollectedAt,
                    // Backward-compatible aliases for frontend
                    cpu_usage = status.CpuPercentage,
                    memory_usage = status.RamPercentage,
                    disk_usage = status.DiskPercentage,
                    cpu_temp = status.CpuTemperature,
                    battery_level = status.BatteryPercentage
                } : null;

                return Ok(ApiResponse<object>.Ok(dto));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to fetch status for machine ID {Id}", id);
                return StatusCode(500, ApiResponse.Fail("Failed to retrieve machine status."));
            }
        }

        [HttpGet("{id}/history")]
        public async Task<IActionResult> History(long id, [FromQuery] DateTime? from = null, [FromQuery] DateTime? to = null)
        {
            var query = _dbContext.HealthLogs
                .AsNoTracking()
                .Where(h => h.MachineId == id);

            if (from.HasValue)
            {
                var fromUtc = DateTime.SpecifyKind(from.Value, DateTimeKind.Utc);
                query = query.Where(h => h.CollectedAt >= fromUtc);
            }
            if (to.HasValue)
            {
                var toUtc = DateTime.SpecifyKind(to.Value, DateTimeKind.Utc);
                var toLimit = toUtc.Date.AddDays(1).AddTicks(-1);
                query = query.Where(h => h.CollectedAt <= toLimit);
            }

            var history = await query
                .OrderByDescending(h => h.CollectedAt)
                .Take(50)
                .Select(h => new
                {
                    h.Id,
                    cpu_percentage = h.CpuPercentage,
                    ram_percentage = h.RamPercentage,
                    disk_percentage = h.DiskPercentage,
                    cpu_temperature = h.CpuTemperature,
                    collected_at = h.CollectedAt,
                    created_at = h.CreatedAt,
                    // Backward-compatible aliases
                    cpu_usage = h.CpuPercentage,
                    memory_usage = h.RamPercentage,
                    disk_usage = h.DiskPercentage,
                    cpu_temp = h.CpuTemperature
                })
                .ToListAsync();
            return Ok(ApiResponse<object>.Ok(history));
        }

        [HttpPost("{id}/assign")]
        public async Task<IActionResult> Assign(long id, [FromBody] JsonElement body)
        {
            if (body.TryGetProperty("user_id", out var userProp) && userProp.TryGetInt64(out var userId))
            {
                var machine = await _machineService.AssignMachineAsync(id, userId);
                return Ok(ApiResponse<object>.Ok(machine, "Machine assigned successfully."));
            }
            return BadRequest(ApiResponse.Fail("user_id is required."));
        }

        [HttpPost("{id}/unassign")]
        public async Task<IActionResult> Unassign(long id)
        {
            var machine = await _machineService.UnassignMachineAsync(id);
            return Ok(ApiResponse<object>.Ok(machine, "Machine unassigned successfully."));
        }

        // Sub-Resources Details for Machine Details Tabs
        [HttpGet("{id}/inventory")]
        public async Task<IActionResult> Inventory(long id)
        {
            var hw = await _dbContext.HardwareInventories
                .AsNoTracking()
                .Where(x => x.MachineId == id)
                .Select(x => new
                {
                    x.Id,
                    manufacturer = x.Manufacturer,
                    model = x.Model,
                    serial_number = x.SerialNumber,
                    processor_name = x.CpuModel,
                    processor_cores = x.CpuCores,
                    cpu_threads = x.CpuThreads,
                    cpu_max_clock_speed = x.CpuMaxClockSpeed,
                    cpu_architecture = x.CpuArchitecture,
                    ram_total_gb = x.TotalRamBytes != null ? (long?)(x.TotalRamBytes / 1073741824) : null,
                    ram_slots = x.RamSlots,
                    ram_type = x.RamType,
                    ram_speed = x.RamSpeed,
                    bios_version = x.BiosVersion,
                    motherboard_model = x.MotherboardModel,
                    gpu_name = x.GpuName,
                    gpu_driver_version = x.GpuDriverVersion,
                    gpu_memory_bytes = x.GpuMemoryBytes
                })
                .FirstOrDefaultAsync();
            var sw = await _dbContext.SoftwareInventories
                .AsNoTracking()
                .Where(x => x.MachineId == id)
                .Select(x => new { x.Id, software_name = x.Name, version = x.Version, publisher = x.Publisher, install_date = x.InstallDate })
                .ToListAsync();

            var disks = await _dbContext.MachineDisks
                .AsNoTracking()
                .Where(x => x.MachineId == id)
                .Select(x => new { x.Id, drive_letter = x.DriveLetter, volume_label = x.VolumeLabel, file_system = x.FileSystem, drive_type = x.DriveType, total_gb = x.TotalGb, used_gb = x.UsedGb, free_gb = x.FreeGb, health_status = x.HealthStatus })
                .ToListAsync();

            return Ok(ApiResponse<object>.Ok(new { hardware = hw, software = sw, disks = disks }));
        }

        [HttpGet("{id}/security")]
        public async Task<IActionResult> Security(long id)
        {
            var av = await _dbContext.AntivirusStatuses
                .AsNoTracking()
                .Where(x => x.MachineId == id)
                .Select(x => new { x.Id, display_name = x.DisplayName, is_enabled = x.IsRealTimeProtectionEnabled, is_updated = x.IsSignatureUpToDate, x.ProductVersion })
                .FirstOrDefaultAsync();
            var fw = await _dbContext.FirewallStatuses
                .AsNoTracking()
                .Where(x => x.MachineId == id)
                .Select(x => new { x.Id,
                    is_enabled = (x.IsDomainFirewallEnabled == true) || (x.IsPrivateFirewallEnabled == true) || (x.IsPublicFirewallEnabled == true),
                    domain_profile = x.IsDomainFirewallEnabled,
                    private_profile = x.IsPrivateFirewallEnabled,
                    public_profile = x.IsPublicFirewallEnabled,
                    active_profile = x.ActiveProfile
                })
                .FirstOrDefaultAsync();
            var logins = await _dbContext.LoginActivities
                .AsNoTracking()
                .Where(x => x.MachineId == id)
                .OrderByDescending(x => x.EventTime)
                .Select(x => new { x.Id, username = x.Username, logon_type = x.LogonType, created_at = x.EventTime })
                .Take(20)
                .ToListAsync();
            var updates = await _dbContext.WindowsUpdates
                .AsNoTracking()
                .Where(x => x.MachineId == id)
                .OrderByDescending(x => x.InstalledOn)
                .Select(x => new { x.Id, title = x.UpdateTitle, kb_id = x.KbArticleId, is_installed = x.IsInstalled, severity = x.Severity, installed_on = x.InstalledOn })
                .Take(20)
                .ToListAsync();
            return Ok(ApiResponse<object>.Ok(new { antivirus = av, firewall = fw, login_activity = logins, pending_updates = updates }));
        }

        [HttpGet("{id}/devices")]
        public async Task<IActionResult> Devices(long id, [FromQuery] int page = 1, [FromQuery] int per_page = 50)
        {
            var query = _dbContext.MachineConnectedDevices
                .AsNoTracking()
                .Where(x => x.MachineId == id);

            var total = await query.CountAsync();
            per_page = Math.Clamp(per_page, 1, 100);
            var lastPage = (int)Math.Ceiling((double)total / per_page);

            var connected = await query
                .OrderBy(x => x.DeviceName)
                .Skip((page - 1) * per_page)
                .Take(per_page)
                .Select(x => new
                {
                    x.Id,
                    device_name = x.DeviceName,
                    device_type = x.DeviceType,
                    device_id = x.DeviceId,
                    status = x.Status,
                    manufacturer = x.Manufacturer,
                    driver_version = x.DriverVersion,
                    has_problem = x.HasProblem,
                    problem_description = x.ProblemDescription
                })
                .ToListAsync();
            var usb = await _dbContext.UsbActivities
                .AsNoTracking()
                .Where(x => x.MachineId == id)
                .OrderByDescending(x => x.EventTime)
                .Select(x => new { x.Id, device_name = x.DeviceName, device_serial = x.DeviceSerial, event_type = x.EventType, event_time = x.EventTime })
                .Take(20)
                .ToListAsync();
            var deviceEvents = await _dbContext.DeviceEvents
                .AsNoTracking()
                .Where(x => x.MachineId == id)
                .OrderByDescending(x => x.CreatedAt)
                .Select(x => new { x.Id, device_name = x.DeviceName, device_type = x.DeviceType, event_type = x.EventType, created_at = x.CreatedAt })
                .Take(20)
                .ToListAsync();
            return Ok(ApiResponse<object>.Ok(new {
                connected_devices = new {
                    data = connected,
                    current_page = page,
                    per_page,
                    total,
                    last_page = lastPage
                },
                usb_activity = usb,
                device_events = deviceEvents
            }));
        }

        [HttpGet("{id}/device-issues")]
        public async Task<IActionResult> DeviceIssues(long id, [FromQuery] string? device_name = null)
        {
            IQueryable<MachineConnectedDevice> q = _dbContext.MachineConnectedDevices
                .AsNoTracking()
                .Where(x => x.MachineId == id && x.HasProblem == true);
            if (!string.IsNullOrEmpty(device_name))
                q = q.Where(x => x.DeviceName == device_name);

            var device = await q
                .Select(x => new { x.Id, device_name = x.DeviceName, device_type = x.DeviceType, manufacturer = x.Manufacturer, status = x.Status, problem_description = x.ProblemDescription })
                .FirstOrDefaultAsync();

            var alerts = await _dbContext.Alerts
                .AsNoTracking()
                .Where(x => x.MachineId == id && x.Severity == "critical" && x.Status == "open")
                .OrderByDescending(x => x.CreatedAt)
                .Select(x => new { x.Id, x.Title, x.Severity, x.Status, created_at = x.CreatedAt })
                .Take(10)
                .ToListAsync();

            var events = await _dbContext.DeviceEvents
                .AsNoTracking()
                .Where(x => x.MachineId == id)
                .OrderByDescending(x => x.CreatedAt)
                .Select(x => new { x.Id, event_type = x.EventType, device_type = x.DeviceType, event_time = x.CreatedAt })
                .Take(20)
                .ToListAsync();

            return Ok(ApiResponse<object>.Ok(new { device, alerts, events }));
        }

        [HttpGet("{id}/alerts")]
        public async Task<IActionResult> MachineAlerts(long id, [FromQuery] int page = 1, [FromQuery] int per_page = 50)
        {
            var query = _dbContext.Alerts
                .AsNoTracking()
                .Where(x => x.MachineId == id);

            var total = await query.CountAsync();
            per_page = Math.Clamp(per_page, 1, 100);
            var lastPage = (int)Math.Ceiling((double)total / per_page);

            var items = await query
                .OrderByDescending(x => x.CreatedAt)
                .Skip((page - 1) * per_page)
                .Take(per_page)
                .Select(x => new
                {
                    x.Id,
                    title = x.Title,
                    description = x.Description,
                    severity = x.Severity,
                    status = x.Status,
                    created_at = x.CreatedAt,
                    acknowledged_at = x.AcknowledgedAt,
                    resolved_at = x.ResolvedAt,
                    resolution_note = x.ResolutionNote
                })
                .ToListAsync();

            return Ok(ApiResponse<object>.Ok(new
            {
                data = items,
                current_page = page,
                per_page,
                total,
                last_page = lastPage
            }));
        }

        [HttpGet("{id}/timeline")]
        public async Task<IActionResult> Timeline(long id)
        {
            var alerts = await _dbContext.Alerts
                .AsNoTracking()
                .Where(x => x.MachineId == id)
                .Select(x => new { type = "alert", title = x.Title, description = x.Description ?? string.Empty, timestamp = x.CreatedAt })
                .Take(20)
                .ToListAsync();
            var usb = await _dbContext.UsbActivities
                .AsNoTracking()
                .Where(x => x.MachineId == id)
                .Select(x => new { type = "usb", title = x.DeviceName ?? string.Empty, description = x.EventType ?? string.Empty, timestamp = x.EventTime ?? DateTime.UtcNow })
                .Take(20)
                .ToListAsync();
            var logins = await _dbContext.LoginActivities
                .AsNoTracking()
                .Where(x => x.MachineId == id)
                .Select(x => new { type = "login", title = x.Username ?? string.Empty, description = x.EventType ?? string.Empty, timestamp = x.EventTime ?? DateTime.UtcNow })
                .Take(20)
                .ToListAsync();

            var combined = alerts.Concat(usb).Concat(logins)
                .OrderByDescending(x => x.timestamp)
                .Take(30)
                .ToList();
            return Ok(ApiResponse<object>.Ok(combined));
        }

        [HttpGet("{id}/processes")]
        public async Task<IActionResult> Processes(long id, [FromQuery] int page = 1, [FromQuery] int per_page = 50)
        {
            var query = _dbContext.ProcessLogs
                .AsNoTracking()
                .Where(x => x.MachineId == id);

            var total = await query.CountAsync();
            per_page = Math.Clamp(per_page, 1, 100);
            var lastPage = (int)Math.Ceiling((double)total / per_page);

            var items = await query
                .OrderByDescending(x => x.CpuUsagePercentage)
                .Skip((page - 1) * per_page)
                .Take(per_page)
                .Select(x => new
                {
                    x.Id,
                    process_name = x.ProcessName,
                    process_id = x.ProcessId,
                    cpu_usage = x.CpuUsagePercentage,
                    memory_usage = x.WorkingSetBytes,
                    thread_count = x.ThreadCount,
                    user_name = x.UserName
                })
                .ToListAsync();

            return Ok(ApiResponse<object>.Ok(new
            {
                data = items,
                current_page = page,
                per_page,
                total,
                last_page = lastPage
            }));
        }

        [HttpGet("{id}/services")]
        public async Task<IActionResult> Services(long id, [FromQuery] int page = 1, [FromQuery] int per_page = 50)
        {
            var query = _dbContext.WindowsServices
                .AsNoTracking()
                .Where(x => x.MachineId == id);

            var total = await query.CountAsync();
            per_page = Math.Clamp(per_page, 1, 100);
            var lastPage = (int)Math.Ceiling((double)total / per_page);

            var items = await query
                .OrderBy(x => x.DisplayName)
                .Skip((page - 1) * per_page)
                .Take(per_page)
                .Select(x => new
                {
                    x.Id, service_name = x.ServiceName, display_name = x.DisplayName, status = x.Status, start_type = x.StartType, service_type = x.ServiceType
                })
                .ToListAsync();

            return Ok(ApiResponse<object>.Ok(new
            {
                data = items,
                current_page = page,
                per_page,
                total,
                last_page = lastPage
            }));
        }

        [HttpGet("{id}/startup-programs")]
        public async Task<IActionResult> StartupPrograms(long id, [FromQuery] int page = 1, [FromQuery] int per_page = 50)
        {
            var query = _dbContext.StartupPrograms
                .AsNoTracking()
                .Where(x => x.MachineId == id);

            var total = await query.CountAsync();
            per_page = Math.Clamp(per_page, 1, 100);
            var lastPage = (int)Math.Ceiling((double)total / per_page);

            var items = await query
                .OrderBy(x => x.Name)
                .Skip((page - 1) * per_page)
                .Take(per_page)
                .Select(x => new
                {
                    x.Id, program_name = x.Name, program_path = x.Command, startup_type = x.Location, user = x.User, status = x.Status
                })
                .ToListAsync();

            return Ok(ApiResponse<object>.Ok(new
            {
                data = items,
                current_page = page,
                per_page,
                total,
                last_page = lastPage
            }));
        }

        [HttpGet("{id}/event-logs")]
        public async Task<IActionResult> EventLogs(long id, [FromQuery] int page = 1, [FromQuery] int per_page = 50)
        {
            var query = _dbContext.EventLogs
                .AsNoTracking()
                .Where(x => x.MachineId == id);

            var total = await query.CountAsync();
            per_page = Math.Clamp(per_page, 1, 100);
            var lastPage = (int)Math.Ceiling((double)total / per_page);

            var items = await query
                .OrderByDescending(x => x.TimeGenerated)
                .Skip((page - 1) * per_page)
                .Take(per_page)
                .Select(x => new
                {
                    x.Id, log_name = x.LogName, source = x.Source, event_id = x.EventId, level = x.Level, message = x.Message, event_time = x.TimeGenerated
                })
                .ToListAsync();

            return Ok(ApiResponse<object>.Ok(new
            {
                data = items,
                current_page = page,
                per_page,
                total,
                last_page = lastPage
            }));
        }

        [HttpGet("{id}/network")]
        public async Task<IActionResult> NetworkAdapters(long id)
        {
            var adapters = await _dbContext.MachineNetworkAdapters
                .AsNoTracking()
                .Where(x => x.MachineId == id)
                .Select(x => new
                {
                    x.Id, adapter_name = x.AdapterName, ip_address = x.IpAddress, mac_address = x.MacAddress, adapter_type = x.AdapterType, speed = x.Speed, status = x.Status
                })
                .ToListAsync();
            var disks = await _dbContext.MachineDisks
                .AsNoTracking()
                .Where(x => x.MachineId == id)
                .Select(x => new
                {
                    x.Id, drive_letter = x.DriveLetter, volume_label = x.VolumeLabel, file_system = x.FileSystem, drive_type = x.DriveType,
                    total_gb = x.TotalGb, used_gb = x.UsedGb, free_gb = x.FreeGb, health_status = x.HealthStatus
                })
                .ToListAsync();
            return Ok(ApiResponse<object>.Ok(new { adapters = adapters, disks = disks }));
        }
    }
}
