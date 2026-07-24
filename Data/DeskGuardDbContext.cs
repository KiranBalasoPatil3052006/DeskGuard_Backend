using DeskGuardBackend.Entities;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// DeskGuard database context for Entity Framework Core.
/// Configures all entity mappings, relationships, indexes, and constraints
/// for the PostgreSQL 'deskguard' database.
///
/// Architecture: Uses Fluent API configuration exclusively (no data annotations).
/// Naming: Uses snake_case naming convention (EFCore.NamingConventions) to match
/// the existing Laravel/MySQL column naming convention seamlessly.
/// </summary>
namespace DeskGuardBackend.Data
{
    public class DeskGuardDbContext : DbContext
    {
        public DeskGuardDbContext(DbContextOptions<DeskGuardDbContext> options) : base(options) { }

        // Core
        public DbSet<Company> Companies => Set<Company>();
        public DbSet<Customer> Customers => Set<Customer>();
        public DbSet<User> Users => Set<User>();
        public DbSet<Machine> Machines => Set<Machine>();
        public DbSet<MachineToken> MachineTokens => Set<MachineToken>();
        public DbSet<MachineCurrentStatus> MachineCurrentStatuses => Set<MachineCurrentStatus>();
        public DbSet<HealthLog> HealthLogs => Set<HealthLog>();

        // Inventory
        public DbSet<HardwareInventory> HardwareInventories => Set<HardwareInventory>();
        public DbSet<SoftwareInventory> SoftwareInventories => Set<SoftwareInventory>();

        // Security
        public DbSet<AntivirusStatus> AntivirusStatuses => Set<AntivirusStatus>();
        public DbSet<FirewallStatus> FirewallStatuses => Set<FirewallStatus>();
        public DbSet<LoginActivity> LoginActivities => Set<LoginActivity>();
        public DbSet<UsbActivity> UsbActivities => Set<UsbActivity>();
        public DbSet<SecuritySetting> SecuritySettings => Set<SecuritySetting>();
        public DbSet<UserLoginHistory> UserLoginHistories => Set<UserLoginHistory>();

        // Notifications & SMTP
        public DbSet<SmtpConfiguration> SmtpConfigurations => Set<SmtpConfiguration>();
        public DbSet<NotificationRule> NotificationRules => Set<NotificationRule>();
        public DbSet<EmailLog> EmailLogs => Set<EmailLog>();

        // Monitoring
        public DbSet<WindowsService> WindowsServices => Set<WindowsService>();
        public DbSet<WindowsUpdate> WindowsUpdates => Set<WindowsUpdate>();
        public DbSet<EventLog> EventLogs => Set<EventLog>();
        public DbSet<StartupProgram> StartupPrograms => Set<StartupProgram>();
        public DbSet<ProcessLog> ProcessLogs => Set<ProcessLog>();
        public DbSet<MachineConnectedDevice> MachineConnectedDevices => Set<MachineConnectedDevice>();
        public DbSet<DeviceEvent> DeviceEvents => Set<DeviceEvent>();
        public DbSet<MachineNetworkAdapter> MachineNetworkAdapters => Set<MachineNetworkAdapter>();
        public DbSet<MachineDisk> MachineDisks => Set<MachineDisk>();

        // Alerts & Changes
        public DbSet<AlertRule> AlertRules => Set<AlertRule>();
        public DbSet<Alert> Alerts => Set<Alert>();
        public DbSet<ChangeHistory> ChangeHistories => Set<ChangeHistory>();
        public DbSet<AlertProfile> AlertProfiles => Set<AlertProfile>();
        public DbSet<AlertThreshold> AlertThresholds => Set<AlertThreshold>();
        public DbSet<HardwareBaseline> HardwareBaselines => Set<HardwareBaseline>();
        public DbSet<SoftwareBaseline> SoftwareBaselines => Set<SoftwareBaseline>();
        public DbSet<SecurityBaseline> SecurityBaselines => Set<SecurityBaseline>();
        public DbSet<ConfigurationBaseline> ConfigurationBaselines => Set<ConfigurationBaseline>();

        // System
        public DbSet<Report> Reports => Set<Report>();
        public DbSet<Notification> Notifications => Set<Notification>();
        public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
        public DbSet<EmailRecipient> EmailRecipients => Set<EmailRecipient>();
        public DbSet<OtpCode> OtpCodes => Set<OtpCode>();
        public DbSet<RawPayloadLog> RawPayloadLogs => Set<RawPayloadLog>();
        public DbSet<PersonalAccessToken> PersonalAccessTokens => Set<PersonalAccessToken>();

        // RBAC
        public DbSet<Role> Roles => Set<Role>();
        public DbSet<Permission> Permissions => Set<Permission>();
        public DbSet<UserRole> UserRoles => Set<UserRole>();
        public DbSet<RolePermission> RolePermissions => Set<RolePermission>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // ========== COMPANIES ==========
            modelBuilder.Entity<Company>(e =>
            {
                e.ToTable("companies");
                e.HasIndex(c => c.IsActive);
                e.HasIndex(c => c.Email).IsUnique();
                e.Property(c => c.Name).HasMaxLength(255).IsRequired();
                e.Property(c => c.CustomerId).HasMaxLength(100);
                e.Property(c => c.Email).HasMaxLength(255);
                e.Property(c => c.Phone).HasMaxLength(50);
                e.Property(c => c.Website).HasMaxLength(255);
                e.Property(c => c.AmcPlan).HasMaxLength(100);
                e.Property(c => c.AmcStartDate);
                e.Property(c => c.AmcEndDate);

                e.HasOne(c => c.AlertProfile)
                    .WithMany(p => p.AssignedCompanies)
                    .HasForeignKey(c => c.AlertProfileId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            // ========== USERS ==========
            modelBuilder.Entity<User>(e =>
            {
                e.ToTable("users");
                e.HasIndex(u => u.Email).IsUnique();
                e.HasIndex(u => u.EmployeeId).IsUnique();
                e.HasIndex(u => u.CompanyId);
                e.HasIndex(u => u.IsActive);
                e.HasIndex(u => u.MobileNumber);
                e.HasQueryFilter(u => u.DeletedAt == null); // Soft delete global filter
                e.Property(u => u.Name).HasMaxLength(255);
                e.Property(u => u.Email).HasMaxLength(255);
                e.Property(u => u.Password).HasMaxLength(255);
                e.Property(u => u.Phone).HasMaxLength(50);
                e.Property(u => u.Avatar).HasMaxLength(255);
                e.Property(u => u.MobileNumber).HasMaxLength(20);
                e.Property(u => u.EmployeeId).HasMaxLength(20);

                e.HasOne(u => u.Company)
                    .WithMany(c => c.Users)
                    .HasForeignKey(u => u.CompanyId)
                    .OnDelete(DeleteBehavior.Cascade);

                e.HasOne(u => u.CreatedBy)
                    .WithMany()
                    .HasForeignKey(u => u.CreatedByUserId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            // ========== CUSTOMERS ==========
            modelBuilder.Entity<Customer>(e =>
            {
                e.ToTable("customers");
                e.HasIndex(c => new { c.CompanyName, c.MobileNumber });
                e.Property(c => c.CustomerCode).HasMaxLength(100).IsRequired();
                e.Property(c => c.CompanyName).HasMaxLength(255).IsRequired();
                e.Property(c => c.CustomerName).HasMaxLength(255).IsRequired();
                e.Property(c => c.MobileNumber).HasMaxLength(50).IsRequired();
                e.Property(c => c.Email).HasMaxLength(255);
                e.Property(c => c.Status).HasMaxLength(50).HasDefaultValue("Active");
                e.Property(c => c.Remarks).HasMaxLength(1000);
            });

            // ========== MACHINES ==========
            modelBuilder.Entity<Machine>(e =>
            {
                e.ToTable("machines");
                e.HasIndex(m => m.MachineUid).IsUnique();
                e.HasIndex(m => m.CompanyId);
                e.HasIndex(m => m.CustomerId);
                e.HasIndex(m => m.UserId);
                e.HasIndex(m => m.IsOnline);
                e.HasIndex(m => new { m.CompanyId, m.IsOnline }); // Dashboard filter
                e.Property(m => m.MachineUid).HasMaxLength(255).IsRequired();
                e.Property(m => m.Hostname).HasMaxLength(255);
                e.Property(m => m.DeviceName).HasMaxLength(255);
                e.Property(m => m.OperatingSystem).HasMaxLength(255);
                e.Property(m => m.OsVersion).HasMaxLength(255);
                e.Property(m => m.Manufacturer).HasMaxLength(255);
                e.Property(m => m.Model).HasMaxLength(255);
                e.Property(m => m.SerialNumber).HasMaxLength(255);
                e.Property(m => m.BiosVersion).HasMaxLength(255);
                e.Property(m => m.Processor).HasMaxLength(255);
                e.Property(m => m.ActivationToken).HasMaxLength(255);
                e.Property(m => m.EmployeeMobileNumber).HasMaxLength(20);
                e.Ignore(m => m.ApiToken); // Transient — not stored in DB

                e.HasOne(m => m.Company)
                    .WithMany(c => c.Machines)
                    .HasForeignKey(m => m.CompanyId)
                    .OnDelete(DeleteBehavior.Cascade);

                e.HasOne(m => m.Customer)
                    .WithMany(c => c.Machines)
                    .HasForeignKey(m => m.CustomerId)
                    .OnDelete(DeleteBehavior.SetNull);

                e.HasOne(m => m.AssignedUser)
                    .WithMany(u => u.Machines)
                    .HasForeignKey(m => m.UserId)
                    .OnDelete(DeleteBehavior.SetNull);

                e.HasOne(m => m.CustomAlertProfile)
                    .WithMany(p => p.CustomAssignedMachines)
                    .HasForeignKey(m => m.CustomAlertProfileId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            // ========== MACHINE TOKENS ==========
            modelBuilder.Entity<MachineToken>(e =>
            {
                e.ToTable("machine_tokens");
                e.HasIndex(t => t.Token);

                e.HasOne(t => t.Machine)
                    .WithMany(m => m.MachineTokens)
                    .HasForeignKey(t => t.MachineId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // ========== MACHINE CURRENT STATUS ==========
            modelBuilder.Entity<MachineCurrentStatus>(e =>
            {
                e.ToTable("machine_current_status");
                e.HasIndex(s => s.MachineId).IsUnique();
                e.Property(s => s.CpuPercentage).HasPrecision(5, 2);
                e.Property(s => s.CpuTemperature).HasPrecision(5, 2);
                e.Property(s => s.CpuClockSpeed).HasPrecision(10, 2);
                e.Property(s => s.RamPercentage).HasPrecision(5, 2);
                e.Property(s => s.DiskPercentage).HasPrecision(5, 2);
                e.Property(s => s.BatteryPercentage).HasPrecision(5, 2);
                e.Property(s => s.BatteryWearLevel).HasPrecision(5, 2);

                e.HasOne(s => s.Machine)
                    .WithOne(m => m.CurrentStatus)
                    .HasForeignKey<MachineCurrentStatus>(s => s.MachineId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // ========== HEALTH LOGS ==========
            modelBuilder.Entity<HealthLog>(e =>
            {
                e.ToTable("health_logs");
                e.HasIndex(h => h.MachineId);
                e.HasIndex(h => h.CompanyId);
                e.HasIndex(h => h.CollectedAt);
                e.HasIndex(h => new { h.MachineId, h.CollectedAt }); // Performance chart queries
                e.HasIndex(h => new { h.CompanyId, h.CollectedAt }); // Dashboard aggregation queries
                e.Property(h => h.CpuPercentage).HasPrecision(5, 2);
                e.Property(h => h.CpuTemperature).HasPrecision(5, 2);
                e.Property(h => h.CpuClockSpeed).HasPrecision(10, 2);
                e.Property(h => h.RamPercentage).HasPrecision(5, 2);
                e.Property(h => h.DiskPercentage).HasPrecision(5, 2);
                e.Property(h => h.BatteryPercentage).HasPrecision(5, 2);

                e.HasOne(h => h.Machine)
                    .WithMany(m => m.HealthLogs)
                    .HasForeignKey(h => h.MachineId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // ========== HARDWARE INVENTORY ==========
            modelBuilder.Entity<HardwareInventory>(e =>
            {
                e.ToTable("hardware_inventory");
                e.HasIndex(h => h.MachineId);
                e.HasOne(h => h.Machine).WithMany(m => m.HardwareInventories)
                    .HasForeignKey(h => h.MachineId).OnDelete(DeleteBehavior.Cascade);
            });

            // ========== SOFTWARE INVENTORY ==========
            modelBuilder.Entity<SoftwareInventory>(e =>
            {
                e.ToTable("software_inventory");
                e.HasIndex(s => s.MachineId);
                e.HasIndex(s => new { s.MachineId, s.Name }); // Dedupe lookups
                e.HasOne(s => s.Machine).WithMany(m => m.SoftwareInventories)
                    .HasForeignKey(s => s.MachineId).OnDelete(DeleteBehavior.Cascade);
            });

            // ========== ANTIVIRUS STATUS ==========
            modelBuilder.Entity<AntivirusStatus>(e =>
            {
                e.ToTable("antivirus_status");
                e.HasIndex(a => a.MachineId);
                e.HasOne(a => a.Machine).WithMany()
                    .HasForeignKey(a => a.MachineId).OnDelete(DeleteBehavior.Cascade);
            });

            // ========== FIREWALL STATUS ==========
            modelBuilder.Entity<FirewallStatus>(e =>
            {
                e.ToTable("firewall_status");
                e.HasIndex(f => f.MachineId);
                e.HasOne(f => f.Machine).WithMany()
                    .HasForeignKey(f => f.MachineId).OnDelete(DeleteBehavior.Cascade);
            });

            // ========== LOGIN ACTIVITIES ==========
            modelBuilder.Entity<LoginActivity>(e =>
            {
                e.ToTable("login_activities");
                e.HasIndex(l => l.MachineId);
                e.HasIndex(l => l.CompanyId);
                e.HasIndex(l => l.EventTime);
                e.HasOne(l => l.Machine).WithMany(m => m.LoginActivities)
                    .HasForeignKey(l => l.MachineId).OnDelete(DeleteBehavior.Cascade);
            });

            // ========== USB ACTIVITIES ==========
            modelBuilder.Entity<UsbActivity>(e =>
            {
                e.ToTable("usb_activities");
                e.HasIndex(u => u.MachineId);
                e.HasIndex(u => u.CompanyId);
                e.HasIndex(u => u.EventTime);
                e.HasOne(u => u.Machine).WithMany(m => m.UsbActivities)
                    .HasForeignKey(u => u.MachineId).OnDelete(DeleteBehavior.Cascade);
            });

            // ========== WINDOWS SERVICES ==========
            modelBuilder.Entity<WindowsService>(e =>
            {
                e.ToTable("windows_services");
                e.HasIndex(s => s.MachineId);
                e.HasIndex(s => new { s.MachineId, s.ServiceName }); // Service queries by machine
                e.HasOne(s => s.Machine).WithMany(m => m.WindowsServices)
                    .HasForeignKey(s => s.MachineId).OnDelete(DeleteBehavior.Cascade);
            });

            // ========== WINDOWS UPDATES ==========
            modelBuilder.Entity<WindowsUpdate>(e =>
            {
                e.ToTable("windows_updates");
                e.HasIndex(u => u.MachineId);
                e.HasOne(u => u.Machine).WithMany(m => m.WindowsUpdates)
                    .HasForeignKey(u => u.MachineId).OnDelete(DeleteBehavior.Cascade);
            });

            // ========== EVENT LOGS ==========
            modelBuilder.Entity<EventLog>(e =>
            {
                e.ToTable("event_logs");
                e.HasIndex(l => l.MachineId);
                e.HasIndex(l => l.TimeGenerated);
                e.HasIndex(l => new { l.MachineId, l.TimeGenerated }); // Machine event log queries
                e.HasOne(l => l.Machine).WithMany(m => m.EventLogs)
                    .HasForeignKey(l => l.MachineId).OnDelete(DeleteBehavior.Cascade);
            });

            // ========== STARTUP PROGRAMS ==========
            modelBuilder.Entity<StartupProgram>(e =>
            {
                e.ToTable("startup_programs");
                e.HasIndex(s => s.MachineId);
                e.HasOne(s => s.Machine).WithMany(m => m.StartupPrograms)
                    .HasForeignKey(s => s.MachineId).OnDelete(DeleteBehavior.Cascade);
            });

            // ========== PROCESS LOGS ==========
            modelBuilder.Entity<ProcessLog>(e =>
            {
                e.ToTable("process_logs");
                e.HasIndex(p => p.MachineId);
                e.HasIndex(p => new { p.MachineId, p.CpuUsagePercentage }); // Top processes queries
                e.Property(p => p.CpuUsagePercentage).HasPrecision(10, 4);
                e.Property(p => p.MemoryUsageMb).HasPrecision(10, 2);
                e.HasOne(p => p.Machine).WithMany(m => m.ProcessLogs)
                    .HasForeignKey(p => p.MachineId).OnDelete(DeleteBehavior.Cascade);
            });

            // ========== MACHINE CONNECTED DEVICES ==========
            modelBuilder.Entity<MachineConnectedDevice>(e =>
            {
                e.ToTable("machine_connected_devices");
                e.HasIndex(d => d.MachineId);
                e.HasOne(d => d.Machine).WithMany(m => m.ConnectedDevices)
                    .HasForeignKey(d => d.MachineId).OnDelete(DeleteBehavior.Cascade);
            });

            // ========== DEVICE EVENTS ==========
            modelBuilder.Entity<DeviceEvent>(e =>
            {
                e.ToTable("device_events");
                e.HasIndex(d => d.MachineId);
                e.HasOne(d => d.Machine).WithMany(m => m.DeviceEvents)
                    .HasForeignKey(d => d.MachineId).OnDelete(DeleteBehavior.Cascade);
            });

            // ========== MACHINE NETWORK ADAPTERS ==========
            modelBuilder.Entity<MachineNetworkAdapter>(e =>
            {
                e.ToTable("machine_network_adapters");
                e.HasIndex(n => n.MachineId);
                e.HasIndex(n => new { n.MachineId, n.AdapterName }); // Network adapter queries by machine
                e.HasOne(n => n.Machine).WithMany(m => m.NetworkAdapters)
                    .HasForeignKey(n => n.MachineId).OnDelete(DeleteBehavior.Cascade);
            });

            // ========== MACHINE DISKS ==========
            modelBuilder.Entity<MachineDisk>(e =>
            {
                e.ToTable("machine_disks");
                e.HasIndex(d => d.MachineId);
                e.Property(d => d.TotalGb).HasPrecision(10, 2);
                e.Property(d => d.UsedGb).HasPrecision(10, 2);
                e.Property(d => d.FreeGb).HasPrecision(10, 2);
                e.HasOne(d => d.Machine).WithMany(m => m.Disks)
                    .HasForeignKey(d => d.MachineId).OnDelete(DeleteBehavior.Cascade);
            });

            // ========== ALERT RULES ==========
            modelBuilder.Entity<AlertRule>(e =>
            {
                e.ToTable("alert_rules");
                e.HasIndex(r => r.CompanyId);
                e.Property(r => r.ThresholdValue).HasPrecision(10, 2);
                e.HasOne(r => r.Company).WithMany()
                    .HasForeignKey(r => r.CompanyId).OnDelete(DeleteBehavior.Cascade);
            });

            // ========== ALERTS ==========
            modelBuilder.Entity<Alert>(e =>
            {
                e.ToTable("alerts");
                e.HasIndex(a => a.CompanyId);
                e.HasIndex(a => a.MachineId);
                e.HasIndex(a => a.Severity);
                e.HasIndex(a => a.Status);
                e.HasIndex(a => a.CreatedAt);
                e.HasIndex(a => new { a.CompanyId, a.Status, a.CreatedAt }); // Dashboard alert queries
                e.HasIndex(a => new { a.MachineId, a.CreatedAt }); // Machine alerts paginated queries
                e.Property(a => a.Title).HasMaxLength(255).IsRequired();
                e.Property(a => a.Severity).HasMaxLength(255);
                e.Property(a => a.Status).HasMaxLength(255);
                e.Property(a => a.Metadata).HasColumnType("jsonb");

                e.HasOne(a => a.Company).WithMany(c => c.Alerts)
                    .HasForeignKey(a => a.CompanyId).OnDelete(DeleteBehavior.Cascade);
                e.HasOne(a => a.Machine).WithMany(m => m.Alerts)
                    .HasForeignKey(a => a.MachineId).OnDelete(DeleteBehavior.Cascade);
                e.HasOne(a => a.AlertRule).WithMany(r => r.Alerts)
                    .HasForeignKey(a => a.AlertRuleId).OnDelete(DeleteBehavior.SetNull);
                e.HasOne(a => a.Acknowledger).WithMany()
                    .HasForeignKey(a => a.AcknowledgedBy).OnDelete(DeleteBehavior.SetNull);
                e.HasOne(a => a.Resolver).WithMany()
                    .HasForeignKey(a => a.ResolvedBy).OnDelete(DeleteBehavior.SetNull);
            });

            // ========== CHANGE HISTORY ==========
            modelBuilder.Entity<ChangeHistory>(e =>
            {
                e.ToTable("change_history");
                e.HasIndex(c => c.CompanyId);
                e.HasIndex(c => c.Category);
                e.HasIndex(c => c.ChangeType);
                e.HasIndex(c => new { c.MachineId, c.DetectedAt });
                e.Property(c => c.Metadata).HasColumnType("jsonb");

                e.HasOne(c => c.Company).WithMany()
                    .HasForeignKey(c => c.CompanyId).OnDelete(DeleteBehavior.Cascade);
                e.HasOne(c => c.Machine).WithMany(m => m.ChangeHistories)
                    .HasForeignKey(c => c.MachineId).OnDelete(DeleteBehavior.Cascade);
            });

            // ========== BASELINES ==========
            modelBuilder.Entity<HardwareBaseline>(e =>
            {
                e.ToTable("hardware_baselines");
                e.HasIndex(b => b.MachineId);
                e.HasIndex(b => new { b.MachineId, b.ComponentType, b.Identifier });
                e.HasOne(b => b.Machine).WithMany().HasForeignKey(b => b.MachineId).OnDelete(DeleteBehavior.Cascade);
            });
            modelBuilder.Entity<SoftwareBaseline>(e =>
            {
                e.ToTable("software_baselines");
                e.HasIndex(b => b.MachineId);
                e.HasIndex(b => new { b.MachineId, b.Name });
                e.HasOne(b => b.Machine).WithMany().HasForeignKey(b => b.MachineId).OnDelete(DeleteBehavior.Cascade);
            });
            modelBuilder.Entity<SecurityBaseline>(e =>
            {
                e.ToTable("security_baselines");
                e.HasIndex(b => b.MachineId);
                e.HasOne(b => b.Machine).WithMany().HasForeignKey(b => b.MachineId).OnDelete(DeleteBehavior.Cascade);
            });
            modelBuilder.Entity<ConfigurationBaseline>(e =>
            {
                e.ToTable("configuration_baselines");
                e.HasIndex(b => b.MachineId);
                e.HasOne(b => b.Machine).WithMany().HasForeignKey(b => b.MachineId).OnDelete(DeleteBehavior.Cascade);
            });

            // ========== ALERT PROFILES ==========
            modelBuilder.Entity<AlertProfile>(e =>
            {
                e.ToTable("alert_profiles");
                e.HasIndex(p => p.Name);
                e.Property(p => p.Name).HasMaxLength(255).IsRequired();

                e.HasOne(p => p.Threshold)
                    .WithOne(t => t.Profile)
                    .HasForeignKey<AlertThreshold>(t => t.ProfileId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // ========== ALERT THRESHOLDS ==========
            modelBuilder.Entity<AlertThreshold>(e =>
            {
                e.ToTable("alert_thresholds");
                e.HasIndex(t => t.ProfileId).IsUnique();

                e.Property(t => t.CpuWarningPercent).HasPrecision(5, 2);
                e.Property(t => t.CpuCriticalPercent).HasPrecision(5, 2);
                e.Property(t => t.RamWarningPercent).HasPrecision(5, 2);
                e.Property(t => t.RamCriticalPercent).HasPrecision(5, 2);
                e.Property(t => t.CpuTempWarning).HasPrecision(5, 2);
                e.Property(t => t.CpuTempCritical).HasPrecision(5, 2);
                e.Property(t => t.DiskWarningPercent).HasPrecision(5, 2);
                e.Property(t => t.DiskCriticalPercent).HasPrecision(5, 2);
            });

            // ========== REPORTS ==========
            modelBuilder.Entity<Report>(e =>
            {
                e.ToTable("reports");
                e.Property(r => r.FileContent).HasColumnName("file_content");
                e.Property(r => r.GeneratorName).HasColumnName("generator_name").HasMaxLength(255);
                e.HasOne(r => r.Generator).WithMany(u => u.Reports)
                    .HasForeignKey(r => r.GeneratedBy).OnDelete(DeleteBehavior.SetNull);
            });

            // ========== NOTIFICATIONS ==========
            modelBuilder.Entity<Notification>(e =>
            {
                e.ToTable("notifications");
                e.HasIndex(n => new { n.UserId, n.IsRead });
                e.HasOne(n => n.User).WithMany()
                    .HasForeignKey(n => n.UserId).OnDelete(DeleteBehavior.SetNull);
            });

            // ========== AUDIT LOGS ==========
            modelBuilder.Entity<AuditLog>(e =>
            {
                e.ToTable("audit_logs");
                e.HasIndex(a => a.UserId);
                e.HasIndex(a => a.CompanyId);
                e.HasIndex(a => a.EventType);
                e.HasIndex(a => a.CreatedAt);
                e.Property(a => a.OldValues).HasColumnType("jsonb");
                e.Property(a => a.NewValues).HasColumnType("jsonb");
                e.HasOne(a => a.User).WithMany(u => u.AuditLogs)
                    .HasForeignKey(a => a.UserId).OnDelete(DeleteBehavior.SetNull);
            });

            // ========== EMAIL RECIPIENTS ==========
            modelBuilder.Entity<EmailRecipient>(e =>
            {
                e.ToTable("email_recipients");
                e.HasIndex(r => r.CompanyId);
                e.HasOne(r => r.Company).WithMany()
                    .HasForeignKey(r => r.CompanyId).OnDelete(DeleteBehavior.Cascade);
            });

            // ========== OTP CODES ==========
            modelBuilder.Entity<OtpCode>(e =>
            {
                e.ToTable("otp_codes");
                e.HasIndex(o => o.MobileNumber);
                e.HasIndex(o => new { o.MobileNumber, o.IsUsed, o.ExpiresAt });
            });

            // ========== RAW PAYLOAD LOGS ==========
            modelBuilder.Entity<RawPayloadLog>(e =>
            {
                e.ToTable("raw_payload_logs");
                e.HasIndex(r => r.MachineId);
                e.Property(r => r.Payload).HasColumnType("jsonb");
            });

            // ========== PERSONAL ACCESS TOKENS ==========
            modelBuilder.Entity<PersonalAccessToken>(e =>
            {
                e.ToTable("personal_access_tokens");
                e.HasIndex(t => t.Token).IsUnique();
                e.HasIndex(t => new { t.TokenableType, t.TokenableId });
            });

            // ========== RBAC: ROLES ==========
            modelBuilder.Entity<Role>(e =>
            {
                e.ToTable("roles");
                e.HasIndex(r => new { r.Name, r.GuardName }).IsUnique();
            });

            // ========== RBAC: PERMISSIONS ==========
            modelBuilder.Entity<Permission>(e =>
            {
                e.ToTable("permissions");
                e.HasIndex(p => new { p.Name, p.GuardName }).IsUnique();
            });

            // ========== RBAC: USER ↔ ROLE (Spatie-compatible) ==========
            modelBuilder.Entity<UserRole>(e =>
            {
                e.ToTable("model_has_roles");
                e.HasKey(ur => new { ur.RoleId, ur.UserId, ur.ModelType });
                e.Property(ur => ur.UserId).HasColumnName("model_id");
                e.HasQueryFilter(ur => ur.User!.DeletedAt == null); // Match User soft-delete filter

                e.HasOne(ur => ur.Role).WithMany(r => r.UserRoles)
                    .HasForeignKey(ur => ur.RoleId).OnDelete(DeleteBehavior.Cascade);
                e.HasOne(ur => ur.User).WithMany(u => u.UserRoles)
                    .HasForeignKey(ur => ur.UserId).OnDelete(DeleteBehavior.Cascade);
            });

            // ========== RBAC: ROLE ↔ PERMISSION ==========
            modelBuilder.Entity<RolePermission>(e =>
            {
                e.ToTable("role_has_permissions");
                e.HasKey(rp => new { rp.PermissionId, rp.RoleId });

                e.HasOne(rp => rp.Permission).WithMany(p => p.RolePermissions)
                    .HasForeignKey(rp => rp.PermissionId).OnDelete(DeleteBehavior.Cascade);
                e.HasOne(rp => rp.Role).WithMany(r => r.RolePermissions)
                    .HasForeignKey(rp => rp.RoleId).OnDelete(DeleteBehavior.Cascade);
            });

            // ========== SEED DATA ==========
            SeedData(modelBuilder);
        }

        /// <summary>
        /// Seeds initial data: default roles and default profiles.
        /// </summary>
        private static void SeedData(ModelBuilder modelBuilder)
        {
            // Default roles (matching Spatie Permission setup)
            modelBuilder.Entity<Role>().HasData(
                new Role { Id = 1, Name = "Super Admin", GuardName = "web" },
                new Role { Id = 2, Name = "Company Admin", GuardName = "web" },
                new Role { Id = 3, Name = "Support Technician", GuardName = "web" },
                new Role { Id = 4, Name = "Admin", GuardName = "web" }
            );

            var now = new DateTime(2026, 7, 15, 0, 0, 0, DateTimeKind.Utc);

            modelBuilder.Entity<AlertProfile>().HasData(
                new AlertProfile { Id = 1, Name = "Default Profile", Description = "Standard monitoring thresholds for general office workstations.", IsDefault = true, CreatedAt = now, UpdatedAt = now },
                new AlertProfile { Id = 2, Name = "Office Workstations", Description = "Optimized for typical office productivity workloads.", IsDefault = false, CreatedAt = now, UpdatedAt = now },
                new AlertProfile { Id = 3, Name = "Development Machines", Description = "Relaxed thresholds for developer machines with high resource usage.", IsDefault = false, CreatedAt = now, UpdatedAt = now }
            );

            modelBuilder.Entity<AlertThreshold>().HasData(
                // Default Profile thresholds (id=1)
                new AlertThreshold { Id = 1, ProfileId = 1, CpuWarningPercent = 80, CpuCriticalPercent = 95, CpuWarningDurationMinutes = 5, RamWarningPercent = 80, RamCriticalPercent = 95, RamWarningDurationMinutes = 5, CpuTempWarning = 80, CpuTempCritical = 90, DiskWarningPercent = 85, DiskCriticalPercent = 95, DiskSmartWarningEnabled = true, DiskSmartCriticalEnabled = true, OfflineWarningMinutes = 10, OfflineCriticalMinutes = 30, FailedLoginWarningCount = 5, FailedLoginCriticalCount = 15, NetworkDisconnectWarningCount = 3, CreatedAt = now, UpdatedAt = now },
                // Office Workstations thresholds (id=2)
                new AlertThreshold { Id = 2, ProfileId = 2, CpuWarningPercent = 75, CpuCriticalPercent = 90, CpuWarningDurationMinutes = 5, RamWarningPercent = 75, RamCriticalPercent = 90, RamWarningDurationMinutes = 5, CpuTempWarning = 75, CpuTempCritical = 85, DiskWarningPercent = 85, DiskCriticalPercent = 95, DiskSmartWarningEnabled = true, DiskSmartCriticalEnabled = true, OfflineWarningMinutes = 15, OfflineCriticalMinutes = 45, FailedLoginWarningCount = 5, FailedLoginCriticalCount = 10, NetworkDisconnectWarningCount = 3, CreatedAt = now, UpdatedAt = now },
                // Development Machines thresholds (id=3)
                new AlertThreshold { Id = 3, ProfileId = 3, CpuWarningPercent = 90, CpuCriticalPercent = 98, CpuWarningDurationMinutes = 10, RamWarningPercent = 85, RamCriticalPercent = 97, RamWarningDurationMinutes = 10, CpuTempWarning = 85, CpuTempCritical = 95, DiskWarningPercent = 90, DiskCriticalPercent = 97, DiskSmartWarningEnabled = true, DiskSmartCriticalEnabled = true, OfflineWarningMinutes = 10, OfflineCriticalMinutes = 30, FailedLoginWarningCount = 3, FailedLoginCriticalCount = 10, NetworkDisconnectWarningCount = 5, CreatedAt = now, UpdatedAt = now }
            );
        }

        /// <summary>
        /// Automatically sets CreatedAt and UpdatedAt timestamps on SaveChanges.
        /// Mimics Laravel's automatic timestamp behavior.
        /// </summary>
        public override int SaveChanges()
        {
            UpdateTimestamps();
            return base.SaveChanges();
        }

        /// <summary>
        /// Async version of SaveChanges with automatic timestamp updates.
        /// </summary>
        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            UpdateTimestamps();
            return await base.SaveChangesAsync(cancellationToken);
        }

        private void UpdateTimestamps()
        {
            var entries = ChangeTracker.Entries()
                .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified);

            foreach (var entry in entries)
            {
                var updatedAtProp = entry.Properties.FirstOrDefault(p => p.Metadata.Name == "UpdatedAt");
                if (updatedAtProp != null)
                    updatedAtProp.CurrentValue = DateTime.UtcNow;

                if (entry.State == EntityState.Added)
                {
                    var createdAtProp = entry.Properties.FirstOrDefault(p => p.Metadata.Name == "CreatedAt");
                    if (createdAtProp != null && (createdAtProp.CurrentValue == null ||
                        (createdAtProp.CurrentValue is DateTime dt && dt == default)))
                        createdAtProp.CurrentValue = DateTime.UtcNow;
                }

                // Automatically normalize all DateTime properties to UTC to prevent Npgsql DateTimeKind errors
                foreach (var prop in entry.Properties)
                {
                    if (prop.CurrentValue is DateTime dtVal && dtVal.Kind != DateTimeKind.Utc)
                    {
                        prop.CurrentValue = dtVal.Kind == DateTimeKind.Local
                            ? dtVal.ToUniversalTime()
                            : DateTime.SpecifyKind(dtVal, DateTimeKind.Utc);
                    }
                }
            }
        }
    }
}
