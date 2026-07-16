using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using DeskGuardBackend.Data;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace DeskGuardBackend.Middleware
{
    /// <summary>
    /// Middleware for agent (machine) authentication.
    /// Replicates the Laravel 'machine.auth' middleware.
    /// Authenticates requests using a Bearer token hashed with SHA-256 and looks it up in machine_tokens table.
    /// Stores the authenticated machine entity in HttpContext.Items for downstream processing.
    /// </summary>
    public class MachineAuthMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<MachineAuthMiddleware> _logger;

        public MachineAuthMiddleware(RequestDelegate next, ILogger<MachineAuthMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var path = context.Request.Path.Value ?? string.Empty;

            // Only enforce this middleware for routes requiring machine authentication.
            // Under Laravel: Route::middleware('machine.auth')->prefix('v1/agent')
            // Except public agent routes like /v1/agent/register, /v1/agent/request-otp, /v1/agent/verify-otp, etc.
            bool requiresMachineAuth = path.StartsWith("/api/v1/agent/heartbeat", StringComparison.OrdinalIgnoreCase) ||
                                       path.StartsWith("/api/v1/agent/health", StringComparison.OrdinalIgnoreCase) ||
                                       path.StartsWith("/api/v1/agent/inventory", StringComparison.OrdinalIgnoreCase) ||
                                       path.StartsWith("/api/v1/agent/security", StringComparison.OrdinalIgnoreCase) ||
                                       path.StartsWith("/api/v1/agent/telemetry", StringComparison.OrdinalIgnoreCase) ||
                                       path.StartsWith("/api/v1/agent/device-sync", StringComparison.OrdinalIgnoreCase) ||
                                       path.StartsWith("/api/v1/agent/device-events", StringComparison.OrdinalIgnoreCase) ||
                                         path.StartsWith("/api/v1/agent/changes", StringComparison.OrdinalIgnoreCase) ||
                                         path.StartsWith("/api/v1/agent/shutdown", StringComparison.OrdinalIgnoreCase) ||
                                         path.StartsWith("/api/v1/inventory/", StringComparison.OrdinalIgnoreCase);

            if (!requiresMachineAuth)
            {
                await _next(context);
                return;
            }

            var authHeader = context.Request.Headers["Authorization"].ToString();
            if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning("MachineAuthMiddleware - Missing or invalid Authorization header from IP {Ip}", context.Connection.RemoteIpAddress);
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsJsonAsync(new { success = false, message = "Authentication token is missing." });
                return;
            }

            var token = authHeader.Substring("Bearer ".Length).Trim();
            var hashedToken = HashToken(token);

            // Access dbContext via request services (scoped)
            var dbContext = context.RequestServices.GetRequiredService<DeskGuardDbContext>();

            var machineToken = await dbContext.MachineTokens
                .Include(t => t.Machine)
                .FirstOrDefaultAsync(t => t.Token == hashedToken);

            if (machineToken == null)
            {
                _logger.LogWarning("MachineAuthMiddleware - Invalid machine token from IP {Ip}", context.Connection.RemoteIpAddress);
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsJsonAsync(new { success = false, message = "Invalid or expired machine token." });
                return;
            }

            if (machineToken.ExpiresAt.HasValue && machineToken.ExpiresAt.Value < DateTime.UtcNow)
            {
                _logger.LogWarning("MachineAuthMiddleware - Expired machine token for machine {MachineId}", machineToken.MachineId);
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsJsonAsync(new { success = false, message = "Machine token has expired. Please re-register." });
                return;
            }

            if (machineToken.Machine == null || !machineToken.Machine.IsActive)
            {
                _logger.LogWarning("MachineAuthMiddleware - Machine {MachineId} not found or inactive", machineToken.MachineId);
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsJsonAsync(new { success = false, message = "Machine is not registered or is inactive." });
                return;
            }

            // Update last used at timestamp
            machineToken.LastUsedAt = DateTime.UtcNow;
            await dbContext.SaveChangesAsync();

            // Set context items for downstream controllers
            context.Items["Machine"] = machineToken.Machine;
            context.Items["MachineId"] = machineToken.Machine.Id;
            context.Items["CompanyId"] = machineToken.Machine.CompanyId;

            await _next(context);
        }

        private static string HashToken(string token)
        {
            using var sha256 = SHA256.Create();
            var bytes = Encoding.UTF8.GetBytes(token);
            var hashBytes = sha256.ComputeHash(bytes);
            return Convert.ToHexString(hashBytes).ToLowerInvariant();
        }
    }
}
