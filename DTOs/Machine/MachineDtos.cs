using System.Text.Json.Serialization;

namespace DeskGuardBackend.DTOs.Machine
{
    public class MachineRegistrationDto
    {
        [JsonPropertyName("machine_uid")]
        public string MachineUid { get; set; } = string.Empty;

        [JsonPropertyName("activation_token")]
        public string ActivationToken { get; set; } = string.Empty;

        [JsonPropertyName("hostname")]
        public string? Hostname { get; set; }

        [JsonPropertyName("operating_system")]
        public string? OperatingSystem { get; set; }

        // Customer/AMC information collected during installation
        [JsonPropertyName("customer_id")]
        public string? CustomerId { get; set; }

        [JsonPropertyName("customer_name")]
        public string? CustomerName { get; set; }

        [JsonPropertyName("company_name")]
        public string? CompanyName { get; set; }

        [JsonPropertyName("mobile_number")]
        public string? MobileNumber { get; set; }

        [JsonPropertyName("email")]
        public string? Email { get; set; }

        [JsonPropertyName("address")]
        public string? Address { get; set; }

        [JsonPropertyName("amc_plan")]
        public string? AmcPlan { get; set; }

        [JsonPropertyName("amc_start_date")]
        public DateTime? AmcStartDate { get; set; }

        [JsonPropertyName("amc_end_date")]
        public DateTime? AmcEndDate { get; set; }
    }

    public class MachineResponseDto
    {
        [JsonPropertyName("id")]
        public long Id { get; set; }

        [JsonPropertyName("company_id")]
        public long? CompanyId { get; set; }

        [JsonPropertyName("user_id")]
        public long? UserId { get; set; }

        [JsonPropertyName("machine_uid")]
        public string MachineUid { get; set; } = string.Empty;

        [JsonPropertyName("hostname")]
        public string? Hostname { get; set; }

        [JsonPropertyName("device_name")]
        public string? DeviceName { get; set; }

        [JsonPropertyName("operating_system")]
        public string? OperatingSystem { get; set; }

        [JsonPropertyName("os_version")]
        public string? OsVersion { get; set; }

        [JsonPropertyName("manufacturer")]
        public string? Manufacturer { get; set; }

        [JsonPropertyName("model")]
        public string? Model { get; set; }

        [JsonPropertyName("serial_number")]
        public string? SerialNumber { get; set; }

        [JsonPropertyName("bios_version")]
        public string? BiosVersion { get; set; }

        [JsonPropertyName("processor")]
        public string? Processor { get; set; }

        [JsonPropertyName("ram_gb")]
        public int? RamGb { get; set; }

        [JsonPropertyName("is_online")]
        public bool IsOnline { get; set; }

        [JsonPropertyName("last_heartbeat_at")]
        public DateTime? LastHeartbeatAt { get; set; }

        [JsonPropertyName("is_active")]
        public bool IsActive { get; set; }

        [JsonPropertyName("employee_mobile_number")]
        public string? EmployeeMobileNumber { get; set; }

        [JsonPropertyName("current_status")]
        public MachineCurrentStatusDto? CurrentStatus { get; set; }
    }

    public class MachineCurrentStatusDto
    {
        [JsonPropertyName("cpu_percentage")]
        public decimal? CpuPercentage { get; set; }

        [JsonPropertyName("cpu_temperature")]
        public decimal? CpuTemperature { get; set; }

        [JsonPropertyName("cpu_clock_speed")]
        public decimal? CpuClockSpeed { get; set; }

        [JsonPropertyName("cpu_core_count")]
        public int? CpuCoreCount { get; set; }

        [JsonPropertyName("ram_total_bytes")]
        public long? RamTotalBytes { get; set; }

        [JsonPropertyName("ram_used_bytes")]
        public long? RamUsedBytes { get; set; }

        [JsonPropertyName("ram_available_bytes")]
        public long? RamAvailableBytes { get; set; }

        [JsonPropertyName("ram_percentage")]
        public decimal? RamPercentage { get; set; }

        [JsonPropertyName("disk_total_bytes")]
        public long? DiskTotalBytes { get; set; }

        [JsonPropertyName("disk_used_bytes")]
        public long? DiskUsedBytes { get; set; }

        [JsonPropertyName("disk_free_bytes")]
        public long? DiskFreeBytes { get; set; }

        [JsonPropertyName("disk_percentage")]
        public decimal? DiskPercentage { get; set; }

        [JsonPropertyName("disk_health_status")]
        public string? DiskHealthStatus { get; set; }

        [JsonPropertyName("battery_percentage")]
        public decimal? BatteryPercentage { get; set; }

        [JsonPropertyName("battery_charging_status")]
        public bool? BatteryChargingStatus { get; set; }

        [JsonPropertyName("battery_wear_level")]
        public decimal? BatteryWearLevel { get; set; }

        [JsonPropertyName("battery_is_present")]
        public bool? BatteryIsPresent { get; set; }

        [JsonPropertyName("battery_design_capacity")]
        public long? BatteryDesignCapacity { get; set; }

        [JsonPropertyName("battery_full_charge_capacity")]
        public long? BatteryFullChargeCapacity { get; set; }

        [JsonPropertyName("network_received_bytes")]
        public long? NetworkReceivedBytes { get; set; }

        [JsonPropertyName("network_sent_bytes")]
        public long? NetworkSentBytes { get; set; }

        [JsonPropertyName("network_interfaces")]
        public object? NetworkInterfaces { get; set; }

        [JsonPropertyName("antivirus_status")]
        public string? AntivirusStatus { get; set; }

        [JsonPropertyName("antivirus_name")]
        public string? AntivirusName { get; set; }

        [JsonPropertyName("antivirus_enabled")]
        public bool? AntivirusEnabled { get; set; }

        [JsonPropertyName("firewall_status")]
        public string? FirewallStatus { get; set; }

        [JsonPropertyName("firewall_enabled")]
        public bool? FirewallEnabled { get; set; }

        [JsonPropertyName("online_status")]
        public bool OnlineStatus { get; set; }
    }
}
