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

// Configure Serilog structured logging
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .CreateLogger();

builder.Host.UseSerilog();

// Add Database context with PostgreSQL provider and snake_case naming conventions
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
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
builder.Services.AddScoped<IOtpService, OtpService>();
builder.Services.AddScoped<IAgentRegistrationService, AgentRegistrationService>();
builder.Services.AddScoped<IMachineService, MachineService>();
builder.Services.AddScoped<IAlertService, AlertService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IPayloadProcessorService, PayloadProcessorService>();
builder.Services.AddScoped<ITelemetryService, TelemetryService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<ICompanyService, CompanyService>();
builder.Services.AddScoped<IAccountService, AccountService>();
builder.Services.AddScoped<IAlertProfileService, AlertProfileService>();

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
        
        Log.Information("Applying PostgreSQL database migrations...");
        dbContext.Database.Migrate();

        var email = "kiranbalasopatil33@gmail.com";
        var userExists = await dbContext.Users.AnyAsync(u => u.Email == email);
        if (!userExists)
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

            var passwordHash = BCrypt.Net.BCrypt.HashPassword("kiranpatil33");
            var newUser = new User
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

            await dbContext.Users.AddAsync(newUser);
            await dbContext.SaveChangesAsync();
            
            Log.Information("Successfully seeded user: {Email}", email);

            var superAdminRole = await dbContext.Roles.FirstOrDefaultAsync(r => r.Name == "Super Admin");
            if (superAdminRole != null)
            {
                var hasRole = await dbContext.UserRoles.AnyAsync(ur => ur.UserId == newUser.Id && ur.RoleId == superAdminRole.Id);
                if (!hasRole)
                {
                    dbContext.UserRoles.Add(new UserRole
                    {
                        RoleId = superAdminRole.Id,
                        UserId = newUser.Id,
                        ModelType = "App\\Models\\User"
                    });
                    await dbContext.SaveChangesAsync();
                    Log.Information("Assigned Super Admin role to seeded user");
                }
            }
        }
        else
        {
            var existingUser = await dbContext.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (existingUser != null)
            {
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
                        Log.Information("Assigned Super Admin role to existing seed user");
                    }
                }
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
