using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace DeskGuardBackend.DTOs.Customer
{
    public class CustomerDto
    {
        [JsonPropertyName("id")]
        public long Id { get; set; }

        [JsonPropertyName("customer_code")]
        public string CustomerCode { get; set; } = string.Empty;

        [JsonPropertyName("company_name")]
        public string CompanyName { get; set; } = string.Empty;

        [JsonPropertyName("customer_name")]
        public string CustomerName { get; set; } = string.Empty;

        [JsonPropertyName("mobile_number")]
        public string MobileNumber { get; set; } = string.Empty;

        [JsonPropertyName("email")]
        public string? Email { get; set; }

        [JsonPropertyName("status")]
        public string Status { get; set; } = "Active";

        [JsonPropertyName("remarks")]
        public string? Remarks { get; set; }

        [JsonPropertyName("total_systems")]
        public int TotalSystems { get; set; }

        [JsonPropertyName("online_systems")]
        public int OnlineSystems { get; set; }

        [JsonPropertyName("offline_systems")]
        public int OfflineSystems { get; set; }

        [JsonPropertyName("sleeping_systems")]
        public int SleepingSystems { get; set; }

        [JsonPropertyName("critical_alerts_count")]
        public int CriticalAlertsCount { get; set; }

        [JsonPropertyName("last_activity_at")]
        public DateTime? LastActivityAt { get; set; }

        [JsonPropertyName("created_at")]
        public DateTime CreatedAt { get; set; }

        [JsonPropertyName("updated_at")]
        public DateTime UpdatedAt { get; set; }
    }

    public class CustomerDetailDto : CustomerDto
    {
        [JsonPropertyName("maintenance_systems")]
        public int MaintenanceSystems { get; set; }

        [JsonPropertyName("disabled_systems")]
        public int DisabledSystems { get; set; }

        [JsonPropertyName("deleted_systems")]
        public int DeletedSystems { get; set; }

        [JsonPropertyName("uninstalled_systems")]
        public int UninstalledSystems { get; set; }

        [JsonPropertyName("pending_systems")]
        public int PendingSystems { get; set; }

        [JsonPropertyName("unknown_systems")]
        public int UnknownSystems { get; set; }

        [JsonPropertyName("machines")]
        public List<CustomerMachineDto> Machines { get; set; } = new List<CustomerMachineDto>();
    }

    public class CustomerMachineDto
    {
        [JsonPropertyName("id")]
        public long Id { get; set; }

        [JsonPropertyName("machine_uid")]
        public string MachineUid { get; set; } = string.Empty;

        [JsonPropertyName("hostname")]
        public string? Hostname { get; set; }

        [JsonPropertyName("device_name")]
        public string? DeviceName { get; set; }

        [JsonPropertyName("operating_system")]
        public string? OperatingSystem { get; set; }

        [JsonPropertyName("status")]
        public string Status { get; set; } = "Unknown";

        [JsonPropertyName("is_online")]
        public bool IsOnline { get; set; }

        [JsonPropertyName("last_heartbeat_at")]
        public DateTime? LastHeartbeatAt { get; set; }

        [JsonPropertyName("ip_address")]
        public string? IpAddress { get; set; }

        [JsonPropertyName("ram_gb")]
        public int? RamGb { get; set; }

        [JsonPropertyName("processor")]
        public string? Processor { get; set; }

        [JsonPropertyName("critical_alerts_count")]
        public int CriticalAlertsCount { get; set; }

        [JsonPropertyName("created_at")]
        public DateTime CreatedAt { get; set; }
    }
}
