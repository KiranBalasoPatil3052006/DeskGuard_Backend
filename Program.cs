using System;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Serilog;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using DeskGuardBackend.Data;
using DeskGuardBackend.Middleware;
using DeskGuardBackend.Services;
using DeskGuardBackend.Services.Interfaces;
using DeskGuardBackend.Services.PayloadProcessors;
using DeskGuardBackend.BackgroundJobs;
using DeskGuardBackend.SignalR;
using DeskGuardBackend.Entities;

var builder = WebApplication.CreateBuilder(args);

// Initialize QuestPDF license
QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;

// Configure Serilog structured logging
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .CreateLogger();

builder.Host.UseSerilog();

// Add Database context with PostgreSQL provider and snake_case naming conventions
string connectionString;

// Railway's DATABASE_URL takes priority (set automatically by Railway PostgreSQL plugin)
var databaseUrl = Environment.GetEnvironmentVariable("DATABASE_URL");
if (!string.IsNullOrWhiteSpace(databaseUrl))
{
    // Parse postgresql://user:password@host:port/database format
    var uri = new Uri(databaseUrl);
    var userInfo = uri.UserInfo?.Split(':');
    var username = userInfo?.Length > 0 ? userInfo[0] : "postgres";
    var password = userInfo?.Length > 1 ? userInfo[1] : "";
    var host = uri.Host;
    var port = uri.Port > 0 ? uri.Port : 5432;
    var database = uri.AbsolutePath.TrimStart('/');

    connectionString = $"Host={host};Port={port};Database={database};Username={username};Password={password};SSL Mode=Prefer;Trust Server Certificate=true";
    Log.Information("Using DATABASE_URL for PostgreSQL: {Host}:{Port}/{Database}", host, port, database);
}
else
{
    connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? "Host=localhost;Port=5432;Database=deskguard;Username=postgres;Password=postgres";
    Log.Information("Using DefaultConnection string for PostgreSQL");
}

builder.Services.AddDbContext<DeskGuardDbContext>(options =>
{
    options.UseNpgsql(connectionString, npgsqlOptions =>
    {
        npgsqlOptions.EnableRetryOnFailure(
            maxRetryCount: 3,
            maxRetryDelay: TimeSpan.FromSeconds(10),
            errorCodesToAdd: null);
    })
    .UseSnakeCaseNamingConvention();
});

// Configure JWT Authentication options
var jwtSettingsSection = builder.Configuration.GetSection("JwtSettings");
builder.Services.Configure<JwtSettings>(jwtSettingsSection);
var jwtSettings = jwtSettingsSection.Get<JwtSettings>();

if (jwtSettings == null || string.IsNullOrEmpty(jwtSettings.Secret))
{
    throw new InvalidOperationException("JWT settings are not configured properly in appsettings.json.");
}

var key = Encoding.UTF8.GetBytes(jwtSettings.Secret);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false; // Set to true in production
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ValidateIssuer = true,
        ValidIssuer = jwtSettings.Issuer,
        ValidateAudience = true,
        ValidAudience = jwtSettings.Audience,
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
    };
});

// Configure Memory Cache (L1)
builder.Services.AddMemoryCache();

// Configure Redis distributed cache (L2), optional — falls back to IMemoryCache only when unavailable
var redisConfig = builder.Configuration.GetSection("Redis");
if (redisConfig.GetValue<bool>("Enabled"))
{
    var redisConnectionString = redisConfig.GetValue<string>("ConnectionString") ?? "localhost:6379";
    var instanceName = redisConfig.GetValue<string>("InstanceName") ?? "DeskGuard";
    builder.Services.AddStackExchangeRedisCache(options =>
    {
        options.Configuration = redisConnectionString;
        options.InstanceName = instanceName;
    });
    Log.Information("Redis cache enabled: {ConnectionString}", redisConnectionString);
}
else
{
    builder.Services.AddDistributedMemoryCache(); // Safe no-op fallback (no Redis)
    Log.Information("Redis cache disabled, using local memory cache only");
}
builder.Services.AddSingleton<ICacheService, CacheService>();

// Register Scoped Services
builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();
builder.Services.AddScoped<IAuditLogService, AuditLogService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IOtpService, DevelopmentOtpService>();
builder.Services.AddScoped<IAgentRegistrationService, AgentRegistrationService>();
builder.Services.AddScoped<IMachineService, MachineService>();
builder.Services.AddScoped<IAlertService, AlertService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IPayloadProcessorService, PayloadProcessorService>();
builder.Services.AddScoped<ITelemetryService, TelemetryService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<ICompanyService, CompanyService>();
builder.Services.AddScoped<ISmtpEmailService, SmtpEmailService>();
builder.Services.AddSingleton<EmailQueueService>();
builder.Services.AddSingleton<IEmailQueueService>(sp => sp.GetRequiredService<EmailQueueService>());
builder.Services.AddHostedService(sp => sp.GetRequiredService<EmailQueueService>());
builder.Services.AddScoped<ISecurityService, SecurityService>();
builder.Services.AddScoped<IAccountService, AccountService>();
builder.Services.AddScoped<IAlertProfileService, AlertProfileService>();
builder.Services.AddScoped<IMachineStatusService, MachineStatusService>();
builder.Services.AddScoped<ICustomerService, CustomerService>();
builder.Services.AddScoped<DeskGuardBackend.Reports.Interfaces.IReportGenerationService, DeskGuardBackend.Reports.Services.ReportGenerationService>();
builder.Services.AddScoped<DeskGuardBackend.Reports.Interfaces.IMachineReportService, DeskGuardBackend.Reports.Services.MachineReportService>();

// Register Telemetry Payload Processors (Executed sequentially in transaction)
builder.Services.AddScoped<IPayloadProcessor, MachineProcessor>();
builder.Services.AddScoped<IPayloadProcessor, CpuProcessor>();
builder.Services.AddScoped<IPayloadProcessor, MemoryProcessor>();
builder.Services.AddScoped<IPayloadProcessor, DiskProcessor>();
builder.Services.AddScoped<IPayloadProcessor, BatteryProcessor>();
builder.Services.AddScoped<IPayloadProcessor, NetworkProcessor>();
builder.Services.AddScoped<IPayloadProcessor, AntivirusProcessor>();
builder.Services.AddScoped<IPayloadProcessor, FirewallProcessor>();
builder.Services.AddScoped<IPayloadProcessor, AlertProcessor>();
builder.Services.AddScoped<IPayloadProcessor, ProcessProcessor>();
builder.Services.AddScoped<IPayloadProcessor, ServiceProcessor>();
builder.Services.AddScoped<IPayloadProcessor, StartupProgramProcessor>();
builder.Services.AddScoped<IPayloadProcessor, EventLogProcessor>();
builder.Services.AddScoped<IPayloadProcessor, LoginActivityProcessor>();
builder.Services.AddScoped<IPayloadProcessor, UsbActivityProcessor>();
builder.Services.AddScoped<IPayloadProcessor, UpdateProcessor>();
builder.Services.AddScoped<IPayloadProcessor, DeviceProcessor>();

// Register Background Worker Jobs
builder.Services.AddHostedService<OfflineCheckJob>();
builder.Services.AddHostedService<ViewRefreshJob>();

builder.Services.AddControllers()
    .AddJsonOptions(o =>
    {
        o.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
        o.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower;
    });
builder.Services.AddSignalR();

// Swagger API Documentation
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "DeskGuard Backend Web API", Version = "v1" });
    
    // Add JWT authorization configuration
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// Configure CORS - allow all origins for testing
builder.Services.AddCors(options =>
{
    options.AddPolicy("CorsPolicy", policy =>
    {
        policy.SetIsOriginAllowed(_ => true)
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

var app = builder.Build();

// Enable Swagger UI (available in all environments for testing)
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "DeskGuard API V1");
    c.RoutePrefix = "swagger";
});

// Global exception handling middleware
app.UseMiddleware<GlobalExceptionMiddleware>();

// Request logging middleware
app.UseMiddleware<RequestLoggingMiddleware>();

app.UseCors("CorsPolicy");

app.UseRouting();

// Authentication & Authorization middlewares
app.UseAuthentication();
app.UseAuthorization();

// Multitenancy company scoping context middleware
app.UseMiddleware<CompanyScopeMiddleware>();

// Custom machine auth token middleware for agent telemetry
app.UseMiddleware<MachineAuthMiddleware>();

app.MapControllers();
app.MapHub<AlertHub>("/hubs/alerts");

// Auto-migration and user seeding block
try
{
    using (var scope = app.Services.CreateScope())
    {
        var dbContext = scope.ServiceProvider.GetRequiredService<DeskGuardDbContext>();
        
        Log.Information("Applying PostgreSQL database migrations and schema checks...");
        try
        {
            dbContext.Database.ExecuteSqlRaw("ALTER TABLE companies ADD COLUMN IF NOT EXISTS customer_id character varying(100);");
            dbContext.Database.ExecuteSqlRaw("ALTER TABLE users ADD COLUMN IF NOT EXISTS failed_login_attempts integer NOT NULL DEFAULT 0;");
            dbContext.Database.ExecuteSqlRaw("ALTER TABLE users ADD COLUMN IF NOT EXISTS lockout_end_at timestamp with time zone;");

            dbContext.Database.ExecuteSqlRaw(@"
                CREATE TABLE IF NOT EXISTS security_settings (
                    id bigint GENERATED BY DEFAULT AS IDENTITY PRIMARY KEY,
                    company_id bigint NULL,
                    min_password_length integer NOT NULL DEFAULT 6,
                    require_uppercase boolean NOT NULL DEFAULT true,
                    require_lowercase boolean NOT NULL DEFAULT true,
                    require_numbers boolean NOT NULL DEFAULT true,
                    require_special_chars boolean NOT NULL DEFAULT true,
                    idle_session_timeout_minutes integer NOT NULL DEFAULT 30,
                    max_failed_login_attempts integer NOT NULL DEFAULT 5,
                    account_lockout_duration_minutes integer NOT NULL DEFAULT 30,
                    created_at timestamp with time zone NOT NULL DEFAULT NOW(),
                    updated_at timestamp with time zone NOT NULL DEFAULT NOW()
                );
            ");

            dbContext.Database.ExecuteSqlRaw(@"
                CREATE TABLE IF NOT EXISTS user_login_histories (
                    id bigint GENERATED BY DEFAULT AS IDENTITY PRIMARY KEY,
                    user_id bigint NULL,
                    company_id bigint NULL,
                    email character varying(255) NOT NULL,
                    login_time timestamp with time zone NOT NULL DEFAULT NOW(),
                    logout_time timestamp with time zone NULL,
                    ip_address character varying(100) NULL,
                    user_agent text NULL,
                    browser character varying(100) NULL,
                    operating_system character varying(100) NULL,
                    status character varying(50) NOT NULL DEFAULT 'Success',
                    failure_reason character varying(255) NULL,
                    created_at timestamp with time zone NOT NULL DEFAULT NOW(),
                    updated_at timestamp with time zone NOT NULL DEFAULT NOW()
                );
            ");

            dbContext.Database.ExecuteSqlRaw("ALTER TABLE alerts ADD COLUMN IF NOT EXISTS alert_type character varying(100);");
            dbContext.Database.ExecuteSqlRaw("ALTER TABLE alerts ADD COLUMN IF NOT EXISTS resource character varying(100);");
            dbContext.Database.ExecuteSqlRaw("ALTER TABLE alerts ADD COLUMN IF NOT EXISTS current_value numeric;");
            dbContext.Database.ExecuteSqlRaw("ALTER TABLE alerts ADD COLUMN IF NOT EXISTS threshold_value numeric;");
            dbContext.Database.ExecuteSqlRaw("ALTER TABLE alerts ADD COLUMN IF NOT EXISTS max_recorded_value numeric;");
            dbContext.Database.ExecuteSqlRaw("ALTER TABLE alerts ADD COLUMN IF NOT EXISTS first_detected_at timestamp with time zone;");
            dbContext.Database.ExecuteSqlRaw("ALTER TABLE alerts ADD COLUMN IF NOT EXISTS last_detected_at timestamp with time zone;");
            dbContext.Database.ExecuteSqlRaw("ALTER TABLE alerts ADD COLUMN IF NOT EXISTS occurrence_count integer NOT NULL DEFAULT 1;");
            dbContext.Database.ExecuteSqlRaw("ALTER TABLE alerts ADD COLUMN IF NOT EXISTS duration_seconds integer;");
            dbContext.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS idx_alerts_machine_type_resource_status ON alerts (machine_id, alert_type, resource, status);");

            dbContext.Database.ExecuteSqlRaw("ALTER TABLE email_recipients ADD COLUMN IF NOT EXISTS department character varying(100);");

            dbContext.Database.ExecuteSqlRaw(@"
                CREATE TABLE IF NOT EXISTS smtp_configurations (
                    id bigint GENERATED BY DEFAULT AS IDENTITY PRIMARY KEY,
                    company_id bigint NULL,
                    host character varying(255) NOT NULL,
                    port integer NOT NULL DEFAULT 587,
                    username character varying(255) NOT NULL DEFAULT '',
                    encrypted_password text NOT NULL DEFAULT '',
                    enable_ssl boolean NOT NULL DEFAULT true,
                    encryption_type character varying(50) NOT NULL DEFAULT 'TLS',
                    from_email character varying(255) NOT NULL,
                    from_name character varying(255) NOT NULL DEFAULT 'DeskGuard Monitoring System',
                    timeout_seconds integer NOT NULL DEFAULT 15,
                    retry_count integer NOT NULL DEFAULT 3,
                    retry_delay_seconds integer NOT NULL DEFAULT 5,
                    created_at timestamp with time zone NOT NULL DEFAULT NOW(),
                    updated_at timestamp with time zone NOT NULL DEFAULT NOW()
                );
            ");

            dbContext.Database.ExecuteSqlRaw(@"
                CREATE TABLE IF NOT EXISTS notification_rules (
                    id bigint GENERATED BY DEFAULT AS IDENTITY PRIMARY KEY,
                    company_id bigint NULL,
                    category character varying(100) NOT NULL DEFAULT 'Critical Alerts',
                    event_type character varying(100) NOT NULL,
                    display_name character varying(255) NOT NULL,
                    send_email boolean NOT NULL DEFAULT true,
                    created_at timestamp with time zone NOT NULL DEFAULT NOW(),
                    updated_at timestamp with time zone NOT NULL DEFAULT NOW()
                );
            ");

            dbContext.Database.ExecuteSqlRaw(@"
                CREATE TABLE IF NOT EXISTS email_logs (
                    id bigint GENERATED BY DEFAULT AS IDENTITY PRIMARY KEY,
                    company_id bigint NULL,
                    alert_id bigint NULL,
                    machine_id bigint NULL,
                    recipient_email character varying(255) NOT NULL,
                    subject character varying(255) NOT NULL,
                    status character varying(50) NOT NULL DEFAULT 'queued',
                    sent_at timestamp with time zone NULL,
                    failure_reason text NULL,
                    retry_count integer NOT NULL DEFAULT 0,
                    smtp_response text NULL,
                    created_at timestamp with time zone NOT NULL DEFAULT NOW(),
                    updated_at timestamp with time zone NOT NULL DEFAULT NOW()
                );
            ");

            dbContext.Database.ExecuteSqlRaw(@"
                CREATE TABLE IF NOT EXISTS reports (
                    id bigint GENERATED BY DEFAULT AS IDENTITY PRIMARY KEY,
                    company_id bigint NULL,
                    generated_by bigint NULL,
                    title character varying(255) NOT NULL,
                    report_type character varying(100) NULL,
                    format character varying(50) NULL,
                    file_path text NULL,
                    status character varying(50) NULL DEFAULT 'completed',
                    parameters text NULL,
                    file_content bytea NULL,
                    generator_name character varying(255) NULL,
                    created_at timestamp with time zone NOT NULL DEFAULT NOW(),
                    updated_at timestamp with time zone NOT NULL DEFAULT NOW()
                );
            ");
            // Note: alerts columns and backfill already handled above (lines 316-325)
        }
        catch (Exception sqlEx)
        {
            Log.Warning(sqlEx, "Direct SQL schema preparation completed with notice");
        }
        dbContext.Database.Migrate();

        // Clean collected telemetry data & machine records to start fresh data collection
        try
        {
            var cleanupTables = new[]
            {
                "health_logs",
                "process_logs",
                "event_logs",
                "login_activities",
                "usb_activities",
                "windows_updates",
                "windows_services",
                "startup_programs",
                "machine_disks",
                "machine_network_adapters",
                "machine_connected_devices",
                "device_events",
                "change_histories",
                "alerts",
                "reports",
                "email_logs",
                "antivirus_statuses",
                "firewall_statuses",
                "hardware_inventories",
                "software_inventories",
                "machine_current_statuses",
                "machines",
                "customers"
            };

            foreach (var table in cleanupTables)
            {
                try
                {
                    await dbContext.Database.ExecuteSqlRawAsync("TRUNCATE TABLE " + table + " CASCADE;");
                }
                catch
                {
                    // Ignore if table does not exist or is empty
                }
            }
            Log.Information("Database telemetry and machine data cleaned successfully for fresh data collection.");
        }
        catch (Exception cleanEx)
        {
            Log.Warning(cleanEx, "Data cleanup notice during startup");
        }

        var email = "kiranbalasopatil33@gmail.com";
        var requestedPassword = "Kiranpatil@33";
        var passwordHash = BCrypt.Net.BCrypt.HashPassword(requestedPassword);

        var existingUser = await dbContext.Users.FirstOrDefaultAsync(u => u.Email == email);
        if (existingUser == null)
        {
            var company = await dbContext.Companies.FirstOrDefaultAsync();
            if (company == null)
            {
                company = new Company 
                { 
                    Name = "DeskGuard Default Company", 
                    CreatedAt = DateTime.UtcNow, 
                    UpdatedAt = DateTime.UtcNow 
                };
                await dbContext.Companies.AddAsync(company);
                await dbContext.SaveChangesAsync();
            }

            existingUser = new User
            {
                CompanyId = company.Id,
                Email = email,
                Password = passwordHash,
                Name = "Kiran Balaso Patil",
                Phone = "6846810210",
                EmployeeId = "EMP-0001",
                IsActive = true,
                IsVerified = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await dbContext.Users.AddAsync(existingUser);
            await dbContext.SaveChangesAsync();
            Log.Information("Successfully created Super Admin user: {Email}", email);
        }
        else
        {
            existingUser.Password = passwordHash;
            existingUser.IsActive = true;
            existingUser.IsVerified = true;
            existingUser.UpdatedAt = DateTime.UtcNow;
            await dbContext.SaveChangesAsync();
            Log.Information("Updated password and status for Super Admin user: {Email}", email);
        }

        string[] rolesToSeed = { "Super Admin", "Admin", "Customer", "User" };
        foreach (var roleName in rolesToSeed)
        {
            var rExists = await dbContext.Roles.AnyAsync(r => r.Name == roleName);
            if (!rExists)
            {
                await dbContext.Roles.AddAsync(new Role { Name = roleName, GuardName = "web", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow });
            }
        }
        await dbContext.SaveChangesAsync();

        var superAdminRole = await dbContext.Roles.FirstOrDefaultAsync(r => r.Name == "Super Admin");
        if (superAdminRole != null)
        {
            var hasRole = await dbContext.UserRoles.AnyAsync(ur => ur.UserId == existingUser.Id && ur.RoleId == superAdminRole.Id);
            if (!hasRole)
            {
                dbContext.UserRoles.Add(new UserRole
                {
                    RoleId = superAdminRole.Id,
                    UserId = existingUser.Id,
                    ModelType = "App\\Models\\User"
                });
                await dbContext.SaveChangesAsync();
                Log.Information("Assigned Super Admin role to user {Email}", email);
            }
        }
    }
}
catch (Exception ex)
{
    Log.Error(ex, "Failed to apply migrations or seed data during startup");
}

try
{
    Log.Information("Starting DeskGuard .NET Web API...");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application start-up failed");
}
finally
{
    Log.CloseAndFlush();
}
