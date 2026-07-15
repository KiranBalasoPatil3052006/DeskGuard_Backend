using System.Text.Json.Serialization;

namespace DeskGuardBackend.DTOs.AlertThreshold
{
    // ──────────────────────────────────────────────
    // Threshold DTO
    // ──────────────────────────────────────────────
    public class AlertThresholdDto
    {
        [JsonPropertyName("cpu_warning_percent")]
        public decimal? CpuWarningPercent { get; set; }

        [JsonPropertyName("cpu_critical_percent")]
        public decimal? CpuCriticalPercent { get; set; }

        [JsonPropertyName("cpu_warning_duration_minutes")]
        public int? CpuWarningDurationMinutes { get; set; }

        [JsonPropertyName("ram_warning_percent")]
        public decimal? RamWarningPercent { get; set; }

        [JsonPropertyName("ram_critical_percent")]
        public decimal? RamCriticalPercent { get; set; }

        [JsonPropertyName("ram_warning_duration_minutes")]
        public int? RamWarningDurationMinutes { get; set; }

        [JsonPropertyName("cpu_temp_warning")]
        public decimal? CpuTempWarning { get; set; }

        [JsonPropertyName("cpu_temp_critical")]
        public decimal? CpuTempCritical { get; set; }

        [JsonPropertyName("disk_warning_percent")]
        public decimal? DiskWarningPercent { get; set; }

        [JsonPropertyName("disk_critical_percent")]
        public decimal? DiskCriticalPercent { get; set; }

        [JsonPropertyName("disk_smart_warning_enabled")]
        public bool? DiskSmartWarningEnabled { get; set; }

        [JsonPropertyName("disk_smart_critical_enabled")]
        public bool? DiskSmartCriticalEnabled { get; set; }

        [JsonPropertyName("offline_warning_minutes")]
        public int? OfflineWarningMinutes { get; set; }

        [JsonPropertyName("offline_critical_minutes")]
        public int? OfflineCriticalMinutes { get; set; }

        [JsonPropertyName("failed_login_warning_count")]
        public int? FailedLoginWarningCount { get; set; }

        [JsonPropertyName("failed_login_critical_count")]
        public int? FailedLoginCriticalCount { get; set; }

        [JsonPropertyName("network_disconnect_warning_count")]
        public int? NetworkDisconnectWarningCount { get; set; }
    }

    // ──────────────────────────────────────────────
    // Profile DTO (includes thresholds)
    // ──────────────────────────────────────────────
    public class AlertProfileDto
    {
        [JsonPropertyName("id")]
        public long Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("is_default")]
        public bool IsDefault { get; set; }

        [JsonPropertyName("assigned_companies_count")]
        public int AssignedCompaniesCount { get; set; }

        [JsonPropertyName("assigned_machines_count")]
        public int AssignedMachinesCount { get; set; }

        [JsonPropertyName("created_at")]
        public DateTime CreatedAt { get; set; }

        [JsonPropertyName("updated_at")]
        public DateTime UpdatedAt { get; set; }

        [JsonPropertyName("thresholds")]
        public AlertThresholdDto? Thresholds { get; set; }
    }

    // ──────────────────────────────────────────────
    // Request DTOs
    // ──────────────────────────────────────────────
    public class CreateAlertProfileRequest
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("thresholds")]
        public AlertThresholdDto? Thresholds { get; set; }
    }

    public class UpdateAlertProfileRequest
    {
        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("is_default")]
        public bool? IsDefault { get; set; }

        [JsonPropertyName("thresholds")]
        public AlertThresholdDto? Thresholds { get; set; }
    }

    // ──────────────────────────────────────────────
    // List response
    // ──────────────────────────────────────────────
    public class AlertProfileListResponse
    {
        [JsonPropertyName("data")]
        public List<AlertProfileDto> Data { get; set; } = new();

        [JsonPropertyName("total")]
        public int Total { get; set; }

        [JsonPropertyName("page")]
        public int Page { get; set; }

        [JsonPropertyName("per_page")]
        public int PerPage { get; set; }

        [JsonPropertyName("total_pages")]
        public int TotalPages { get; set; }
    }

    // ──────────────────────────────────────────────
    // Filter / Query
    // ──────────────────────────────────────────────
    public class AlertProfileFilterRequest
    {
        [JsonPropertyName("search")]
        public string? Search { get; set; }

        [JsonPropertyName("page")]
        public int Page { get; set; } = 1;

        [JsonPropertyName("per_page")]
        public int PerPage { get; set; } = 20;
    }

    // ──────────────────────────────────────────────
    // Assignment DTOs
    // ──────────────────────────────────────────────
    public class AssignProfileToCompanyRequest
    {
        [JsonPropertyName("company_id")]
        public long CompanyId { get; set; }
    }

    public class AssignProfileToMachineRequest
    {
        [JsonPropertyName("machine_id")]
        public long MachineId { get; set; }
    }
}
