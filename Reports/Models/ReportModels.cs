using System;
using System.Collections.Generic;

namespace DeskGuardBackend.Reports.Models
{
    public class AmcHealthSummaryQueryParameters
    {
        public long? CompanyId { get; set; }
        public long? CustomerId { get; set; }
        public DateTime? DateFrom { get; set; }
        public DateTime? DateTo { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string? AmcPlan { get; set; }
    }

    public class AmcHealthSummaryReportData
    {
        public string ReportId { get; set; } = string.Empty;
        public string CompanyName { get; set; } = string.Empty;
        public string AmcPlan { get; set; } = "No Data Available";
        public string AmcStartDateStr { get; set; } = "No Data Available";
        public string AmcEndDateStr { get; set; } = "No Data Available";
        public string ReportPeriodStr { get; set; } = string.Empty;
        public string GeneratedBy { get; set; } = string.Empty;
        public DateTime GeneratedAt { get; set; }

        // Executive Summary Metrics
        public int TotalSystems { get; set; }
        public int HealthySystems { get; set; }
        public int WarningSystems { get; set; }
        public int CriticalSystems { get; set; }
        public int OfflineSystems { get; set; }
        public double AverageHealthScore { get; set; }
        public int OpenAlerts { get; set; }
        public int ResolvedAlerts { get; set; }
        public int HardwareChangesCount { get; set; }
        public int SoftwareChangesCount { get; set; }

        // System Health Table Rows
        public List<SystemHealthRow> Systems { get; set; } = new();

        // Alert Summary Details
        public int AlertCriticalCount { get; set; }
        public int AlertImportantCount { get; set; }
        public int AlertWarningCount { get; set; }
        public int AlertInfoCount { get; set; }
        public int AlertResolvedCount { get; set; }
        public int AlertPendingCount { get; set; }
        public string AverageResponseTime { get; set; } = "No Data Available";
        public List<RecentAlertRow> RecentCriticalAlerts { get; set; } = new();

        // Change Summary Categories
        public int ChangesHardwareCount { get; set; }
        public int ChangesSoftwareCount { get; set; }
        public int ChangesUsbCount { get; set; }
        public int ChangesConfigCount { get; set; }
        public int ChangesSecurityCount { get; set; }
        public int ChangesNetworkCount { get; set; }
        public List<RecentChangeRow> RecentChanges { get; set; } = new();

        // Security Overview
        public int FirewallEnabledCount { get; set; }
        public int AntivirusEnabledCount { get; set; }
        public string WindowsUpdateStatus { get; set; } = "No Data Available";
        public string BitLockerStatus { get; set; } = "No Data Available";
        public int UnauthorizedChangesCount { get; set; }
        public double SecurityScore { get; set; }

        // System Status Summary
        public int OnlineCount { get; set; }
        public int OfflineCount { get; set; }
        public string LastCollectionTime { get; set; } = "No Data Available";
        public string LastSyncTime { get; set; } = "No Data Available";
        public Dictionary<string, int> AgentVersionDistribution { get; set; } = new();

        // Recommendations
        public List<string> HealthRecommendations { get; set; } = new();
    }

    public class SystemHealthRow
    {
        public string MachineName { get; set; } = string.Empty;
        public string MachineId { get; set; } = string.Empty;
        public double HealthScore { get; set; }
        public string CurrentStatus { get; set; } = string.Empty;
        public string LastHeartbeat { get; set; } = string.Empty;
        public string CpuStatus { get; set; } = string.Empty;
        public string RamStatus { get; set; } = string.Empty;
        public string DiskStatus { get; set; } = string.Empty;
        public string SecurityStatus { get; set; } = string.Empty;
        public string OverallCondition { get; set; } = string.Empty;
    }

    public class RecentAlertRow
    {
        public string Title { get; set; } = string.Empty;
        public string MachineName { get; set; } = string.Empty;
        public string Severity { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string CreatedAt { get; set; } = string.Empty;
    }

    public class RecentChangeRow
    {
        public string MachineName { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string ChangeType { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string DetectedAt { get; set; } = string.Empty;
    }

    public class AssetInventoryQueryParameters
    {
        public long? CompanyId { get; set; }
        public long? CustomerId { get; set; }
        public long? MachineId { get; set; }
        public DateTime? DateFrom { get; set; }
        public DateTime? DateTo { get; set; }
        public string? AmcPlan { get; set; }
    }

    public class AssetInventoryReportData
    {
        public string ReportId { get; set; } = string.Empty;
        public string CompanyName { get; set; } = string.Empty;
        public string RegisteredMobileNumber { get; set; } = "Not Available";
        public string Email { get; set; } = "Not Available";
        public string AmcPlan { get; set; } = "Not Available";
        public string AmcStartDateStr { get; set; } = "Not Available";
        public string AmcEndDateStr { get; set; } = "Not Available";
        public int TotalSystemsCovered { get; set; }
        public string ReportPeriodStr { get; set; } = string.Empty;
        public string GeneratedBy { get; set; } = string.Empty;
        public DateTime GeneratedAt { get; set; }

        // Executive Summary metrics
        public int TotalSystems { get; set; }
        public int TotalCpus { get; set; }
        public double TotalRamGb { get; set; }
        public double TotalStorageGb { get; set; }
        public int TotalSsds { get; set; }
        public int TotalHdds { get; set; }
        public int TotalPrinters { get; set; }
        public int TotalMonitors { get; set; }
        public int TotalNetworkAdapters { get; set; }
        public int TotalGpus { get; set; }
        public int TotalBatteries { get; set; }
        public int TotalUsbDevices { get; set; }

        // System Inventory rows
        public List<SystemInventoryItem> Systems { get; set; } = new();

        // Asset Statistics
        public Dictionary<string, int> CpuBrandDistribution { get; set; } = new(); // Intel, AMD, etc.
        public Dictionary<string, int> GpuBrandDistribution { get; set; } = new(); // NVIDIA, AMD, Intel, etc.
        public int LaptopCount { get; set; }
        public int DesktopCount { get; set; }

        // Missing Devices
        public List<MissingDeviceItem> MissingDevices { get; set; } = new();

        // Replaced Components
        public List<ReplacedComponentItem> ReplacedComponents { get; set; } = new();

        // Recommendations
        public List<string> Recommendations { get; set; } = new();
    }

    public class SystemInventoryItem
    {
        public long Id { get; set; }
        public string MachineName { get; set; } = string.Empty;
        public string MachineId { get; set; } = string.Empty;
        public string OperatingSystem { get; set; } = "Not Available";
        public string CurrentUser { get; set; } = "Not Available";
        public string LastHeartbeat { get; set; } = "Not Available";
        public string LastInventoryUpdate { get; set; } = "Not Available";
        public string HealthStatus { get; set; } = "Not Available";

        // Hardware details
        public CpuDetail Cpu { get; set; } = new();
        public RamDetail Ram { get; set; } = new();
        public List<StorageDetail> Disks { get; set; } = new();
        public GpuDetail Gpu { get; set; } = new();
        public MotherboardDetail Motherboard { get; set; } = new();
        public BatteryDetail Battery { get; set; } = new();
        public List<NetworkAdapterDetail> NetworkAdapters { get; set; } = new();
        public PeripheralDetail Peripherals { get; set; } = new();
    }

    public class CpuDetail
    {
        public string Manufacturer { get; set; } = "Not Available";
        public string Model { get; set; } = "Not Available";
        public string Generation { get; set; } = "Not Available";
        public string ClockSpeed { get; set; } = "Not Available";
        public string CoreCount { get; set; } = "Not Available";
        public string LogicalProcessors { get; set; } = "Not Available";
    }

    public class RamDetail
    {
        public string Manufacturer { get; set; } = "Not Available";
        public string TotalCapacity { get; set; } = "Not Available";
        public string Speed { get; set; } = "Not Available";
        public string Type { get; set; } = "Not Available";
        public string SlotCount { get; set; } = "Not Available";
        public List<string> IndividualModules { get; set; } = new();
    }

    public class StorageDetail
    {
        public string DriveLetter { get; set; } = "Not Available";
        public string DriveType { get; set; } = "Not Available"; // e.g. SSD/HDD
        public string Manufacturer { get; set; } = "Not Available";
        public string Model { get; set; } = "Not Available";
        public string Capacity { get; set; } = "Not Available";
        public string SerialNumber { get; set; } = "Not Available";
        public string SmartStatus { get; set; } = "Not Available";
    }

    public class GpuDetail
    {
        public string GpuName { get; set; } = "Not Available";
        public string Manufacturer { get; set; } = "Not Available";
        public string Memory { get; set; } = "Not Available";
        public string DriverVersion { get; set; } = "Not Available";
    }

    public class MotherboardDetail
    {
        public string Manufacturer { get; set; } = "Not Available";
        public string Model { get; set; } = "Not Available";
        public string SerialNumber { get; set; } = "Not Available";
        public string BiosVersion { get; set; } = "Not Available";
        public string BiosDate { get; set; } = "Not Available";
    }

    public class BatteryDetail
    {
        public bool IsPresent { get; set; }
        public string Manufacturer { get; set; } = "Not Available";
        public string Health { get; set; } = "Not Available";
        public string Capacity { get; set; } = "Not Available";
        public string CycleCount { get; set; } = "Not Available";
        public string Status { get; set; } = "Not Available";
    }

    public class NetworkAdapterDetail
    {
        public string AdapterName { get; set; } = "Not Available";
        public string MacAddress { get; set; } = "Not Available";
        public string AdapterType { get; set; } = "Not Available"; // Ethernet, WiFi, Bluetooth, etc.
        public string DriverVersion { get; set; } = "Not Available";
    }

    public class PeripheralDetail
    {
        public List<string> Monitors { get; set; } = new();
        public List<string> Printers { get; set; } = new();
        public List<string> Keyboards { get; set; } = new();
        public List<string> Mouses { get; set; } = new();
        public List<string> Webcams { get; set; } = new();
        public List<string> Speakers { get; set; } = new();
        public List<string> Microphones { get; set; } = new();
        public List<string> UsbDevices { get; set; } = new();
    }

    public class MissingDeviceItem
    {
        public string MachineName { get; set; } = string.Empty;
        public string Detail { get; set; } = string.Empty; // e.g. "Missing Printer", "Missing Battery"
    }

    public class ReplacedComponentItem
    {
        public string MachineName { get; set; } = string.Empty;
        public string Detail { get; set; } = string.Empty; // e.g. "RAM Replaced", "SSD Replaced"
        public string DetectedAt { get; set; } = string.Empty;
    }
}
