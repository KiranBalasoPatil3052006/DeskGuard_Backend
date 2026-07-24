using System;
using System.Collections.Generic;

namespace DeskGuardBackend.Reports.Models
{
    // ========================================================================
    // QUERY PARAMETERS
    // ========================================================================

    /// <summary>
    /// Query parameters for single-machine report generation.
    /// </summary>
    public class MachineReportQueryParameters
    {
        public long MachineId { get; set; }
        public string ReportType { get; set; } = "health";
        public DateTime? DateFrom { get; set; }
        public DateTime? DateTo { get; set; }
    }

    // ========================================================================
    // SHARED BASE
    // ========================================================================

    /// <summary>
    /// Common metadata present in every machine report header.
    /// </summary>
    public class MachineReportMetadata
    {
        public string ReportId { get; set; } = string.Empty;
        public string ReportTitle { get; set; } = string.Empty;
        public string MachineName { get; set; } = string.Empty;
        public string MachineUid { get; set; } = string.Empty;
        public string OperatingSystem { get; set; } = "No Data Available";
        public string CompanyName { get; set; } = "No Data Available";
        public string GeneratedBy { get; set; } = "Administrator";
        public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
        public string ReportPeriod { get; set; } = string.Empty;
    }

    // ========================================================================
    // 1. MACHINE HEALTH REPORT
    // ========================================================================

    public class MachineHealthReportData
    {
        public MachineReportMetadata Metadata { get; set; } = new();

        // Machine Information
        public string Hostname { get; set; } = "No Data Available";
        public string DeviceName { get; set; } = "No Data Available";
        public string OsVersion { get; set; } = "No Data Available";
        public string Manufacturer { get; set; } = "No Data Available";
        public string Model { get; set; } = "No Data Available";
        public string SerialNumber { get; set; } = "No Data Available";
        public string Processor { get; set; } = "No Data Available";
        public string RamGb { get; set; } = "No Data Available";
        public string RegistrationDate { get; set; } = "No Data Available";
        public string LastHeartbeat { get; set; } = "No Data Available";
        public string LastCollection { get; set; } = "No Data Available";
        public bool IsOnline { get; set; }

        // Health Scores
        public double HealthScore { get; set; }
        public string HealthLabel { get; set; } = "No Data Available";
        public string CpuStatus { get; set; } = "No Data Available";
        public string RamStatus { get; set; } = "No Data Available";
        public string DiskStatus { get; set; } = "No Data Available";
        public string NetworkStatus { get; set; } = "No Data Available";

        // Security
        public string AntivirusName { get; set; } = "No Data Available";
        public bool AntivirusEnabled { get; set; }
        public bool FirewallEnabled { get; set; }
        public string SecurityScore { get; set; } = "No Data Available";

        // Alerts & Changes Summary
        public int OpenAlerts { get; set; }
        public int ResolvedAlerts { get; set; }
        public int TotalChanges { get; set; }
        public List<MachineAlertSummaryRow> RecentAlerts { get; set; } = new();
        public List<MachineChangeSummaryRow> RecentChanges { get; set; } = new();
    }

    public class MachineAlertSummaryRow
    {
        public string Title { get; set; } = string.Empty;
        public string Severity { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string CreatedAt { get; set; } = string.Empty;
    }

    public class MachineChangeSummaryRow
    {
        public string Category { get; set; } = string.Empty;
        public string ChangeType { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string DetectedAt { get; set; } = string.Empty;
    }

    // ========================================================================
    // 2. HARDWARE REPORT
    // ========================================================================

    public class MachineHardwareReportData
    {
        public MachineReportMetadata Metadata { get; set; } = new();

        // CPU
        public string CpuModel { get; set; } = "No Data Available";
        public string CpuCores { get; set; } = "No Data Available";
        public string CpuThreads { get; set; } = "No Data Available";
        public string CpuMaxClockSpeed { get; set; } = "No Data Available";
        public string CpuArchitecture { get; set; } = "No Data Available";

        // RAM
        public string RamTotal { get; set; } = "No Data Available";
        public string RamSlots { get; set; } = "No Data Available";
        public string RamType { get; set; } = "No Data Available";
        public string RamSpeed { get; set; } = "No Data Available";

        // Motherboard
        public string MotherboardManufacturer { get; set; } = "No Data Available";
        public string MotherboardModel { get; set; } = "No Data Available";
        public string BiosVersion { get; set; } = "No Data Available";
        public string MachineSerialNumber { get; set; } = "No Data Available";

        // GPU
        public string GpuName { get; set; } = "No Data Available";
        public string GpuDriverVersion { get; set; } = "No Data Available";
        public string GpuMemory { get; set; } = "No Data Available";

        // Battery
        public bool BatteryPresent { get; set; }
        public string BatteryPercentage { get; set; } = "No Data Available";
        public string BatteryWearLevel { get; set; } = "No Data Available";
        public string BatteryDesignCapacity { get; set; } = "No Data Available";
        public string BatteryFullChargeCapacity { get; set; } = "No Data Available";
        public string BatteryChargingStatus { get; set; } = "No Data Available";

        // Storage
        public List<DiskReportRow> Disks { get; set; } = new();

        // Network Adapters
        public List<NetworkAdapterReportRow> NetworkAdapters { get; set; } = new();

        // Connected Devices (peripherals)
        public List<ConnectedDeviceReportRow> ConnectedDevices { get; set; } = new();
    }

    public class DiskReportRow
    {
        public string DriveLetter { get; set; } = "—";
        public string VolumeLabel { get; set; } = "—";
        public string FileSystem { get; set; } = "—";
        public string DriveType { get; set; } = "—";
        public string TotalGb { get; set; } = "—";
        public string UsedGb { get; set; } = "—";
        public string FreeGb { get; set; } = "—";
        public string HealthStatus { get; set; } = "Unknown";
    }

    public class NetworkAdapterReportRow
    {
        public string AdapterName { get; set; } = "—";
        public string IpAddress { get; set; } = "—";
        public string MacAddress { get; set; } = "—";
        public string AdapterType { get; set; } = "—";
        public string Speed { get; set; } = "—";
        public string Status { get; set; } = "—";
    }

    public class ConnectedDeviceReportRow
    {
        public string DeviceName { get; set; } = "—";
        public string DeviceType { get; set; } = "—";
        public string ConnectionType { get; set; } = "—";
        public string Manufacturer { get; set; } = "—";
        public string DriverVersion { get; set; } = "—";
        public string Status { get; set; } = "—";
    }

    // ========================================================================
    // 3. PERFORMANCE REPORT
    // ========================================================================

    public class MachinePerformanceReportData
    {
        public MachineReportMetadata Metadata { get; set; } = new();

        // Current Status
        public string CurrentCpu { get; set; } = "No Data Available";
        public string CurrentRam { get; set; } = "No Data Available";
        public string CurrentDisk { get; set; } = "No Data Available";
        public string CurrentCpuTemp { get; set; } = "No Data Available";

        // Averages
        public string AvgCpu { get; set; } = "No Data Available";
        public string AvgRam { get; set; } = "No Data Available";
        public string AvgDisk { get; set; } = "No Data Available";

        // Peaks
        public string PeakCpu { get; set; } = "No Data Available";
        public string PeakRam { get; set; } = "No Data Available";
        public string PeakDisk { get; set; } = "No Data Available";

        // Network totals
        public string TotalNetworkReceived { get; set; } = "No Data Available";
        public string TotalNetworkSent { get; set; } = "No Data Available";

        // Timeline rows
        public List<PerformanceTimelineRow> Timeline { get; set; } = new();
        public int TotalDataPoints { get; set; }
    }

    public class PerformanceTimelineRow
    {
        public string CollectedAt { get; set; } = "—";
        public string CpuPercent { get; set; } = "—";
        public string RamPercent { get; set; } = "—";
        public string DiskPercent { get; set; } = "—";
        public string CpuTemp { get; set; } = "—";
        public string NetworkReceived { get; set; } = "—";
        public string NetworkSent { get; set; } = "—";
    }

    // ========================================================================
    // 4. CHANGE TIMELINE REPORT
    // ========================================================================

    public class MachineChangeTimelineReportData
    {
        public MachineReportMetadata Metadata { get; set; } = new();

        // Summary
        public int TotalChanges { get; set; }
        public int HardwareChanges { get; set; }
        public int SoftwareChanges { get; set; }
        public int SecurityChanges { get; set; }
        public int NetworkChanges { get; set; }
        public int ConfigurationChanges { get; set; }
        public int UsbChanges { get; set; }

        // Detail rows
        public List<ChangeDetailRow> Changes { get; set; } = new();
    }

    public class ChangeDetailRow
    {
        public string Category { get; set; } = "—";
        public string ChangeType { get; set; } = "—";
        public string ItemLabel { get; set; } = "—";
        public string PreviousValue { get; set; } = "—";
        public string NewValue { get; set; } = "—";
        public string Severity { get; set; } = "—";
        public string Status { get; set; } = "—";
        public string DetectedAt { get; set; } = "—";
        public string Description { get; set; } = "—";
    }

    // ========================================================================
    // 5. ALERT HISTORY REPORT
    // ========================================================================

    public class MachineAlertHistoryReportData
    {
        public MachineReportMetadata Metadata { get; set; } = new();

        // Summary
        public int TotalAlerts { get; set; }
        public int OpenAlerts { get; set; }
        public int AcknowledgedAlerts { get; set; }
        public int ResolvedAlerts { get; set; }
        public int CriticalAlerts { get; set; }
        public int WarningAlerts { get; set; }
        public int InfoAlerts { get; set; }

        // Detail rows
        public List<AlertDetailRow> Alerts { get; set; } = new();
    }

    public class AlertDetailRow
    {
        public string Title { get; set; } = "—";
        public string Severity { get; set; } = "—";
        public string Status { get; set; } = "—";
        public string CreatedAt { get; set; } = "—";
        public string AcknowledgedAt { get; set; } = "—";
        public string ResolvedAt { get; set; } = "—";
        public string ResolutionNote { get; set; } = "—";
        public string Description { get; set; } = "—";
    }

    // ========================================================================
    // 6. ACTIVITY REPORT
    // ========================================================================

    public class MachineActivityReportData
    {
        public MachineReportMetadata Metadata { get; set; } = new();

        // Summary
        public int TotalLogins { get; set; }
        public int SuccessfulLogins { get; set; }
        public int FailedLogins { get; set; }
        public int TotalUsbEvents { get; set; }

        // Login activity rows
        public List<LoginActivityRow> LoginActivities { get; set; } = new();

        // USB activity rows
        public List<UsbActivityRow> UsbActivities { get; set; } = new();
    }

    public class LoginActivityRow
    {
        public string Username { get; set; } = "—";
        public string EventType { get; set; } = "—";
        public string LogonType { get; set; } = "—";
        public string SourceIp { get; set; } = "—";
        public string IsSuccess { get; set; } = "—";
        public string EventTime { get; set; } = "—";
    }

    public class UsbActivityRow
    {
        public string DeviceName { get; set; } = "—";
        public string DeviceSerial { get; set; } = "—";
        public string EventType { get; set; } = "—";
        public string EventTime { get; set; } = "—";
    }

    // ========================================================================
    // 7. SYSTEM LOG REPORT
    // ========================================================================

    public class MachineSystemLogReportData
    {
        public MachineReportMetadata Metadata { get; set; } = new();

        // Summary
        public int TotalEvents { get; set; }
        public int CriticalEvents { get; set; }
        public int ErrorEvents { get; set; }
        public int WarningEvents { get; set; }
        public int InformationEvents { get; set; }

        // Service status
        public int TotalServices { get; set; }
        public int RunningServices { get; set; }
        public int StoppedServices { get; set; }

        // Startup programs
        public int TotalStartupPrograms { get; set; }

        // Event log rows
        public List<EventLogRow> EventLogs { get; set; } = new();

        // Service rows
        public List<ServiceReportRow> Services { get; set; } = new();

        // Startup program rows
        public List<StartupProgramRow> StartupPrograms { get; set; } = new();
    }

    public class EventLogRow
    {
        public string LogName { get; set; } = "—";
        public string Source { get; set; } = "—";
        public string EventId { get; set; } = "—";
        public string Level { get; set; } = "—";
        public string Message { get; set; } = "—";
        public string TimeGenerated { get; set; } = "—";
    }

    public class ServiceReportRow
    {
        public string ServiceName { get; set; } = "—";
        public string DisplayName { get; set; } = "—";
        public string Status { get; set; } = "—";
        public string StartType { get; set; } = "—";
    }

    public class StartupProgramRow
    {
        public string Name { get; set; } = "—";
        public string Command { get; set; } = "—";
        public string Location { get; set; } = "—";
        public string Status { get; set; } = "—";
    }
}
