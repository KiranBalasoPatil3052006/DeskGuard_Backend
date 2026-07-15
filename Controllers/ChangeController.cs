using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using DeskGuardBackend.DTOs.Common;
using DeskGuardBackend.Entities;
using DeskGuardBackend.Data;
using DeskGuardBackend.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using DeskGuardBackend.Services.Interfaces;

namespace DeskGuardBackend.Controllers
{
    [ApiController]
    [Route("api/v1")]
    public class ChangeController : ControllerBase
    {
        private readonly DeskGuardDbContext _dbContext;
        private readonly IConfiguration _configuration;
        private readonly ILogger<ChangeController> _logger;

        public ChangeController(DeskGuardDbContext dbContext, IConfiguration configuration, ILogger<ChangeController> logger)
        {
            _dbContext = dbContext;
            _configuration = configuration;
            _logger = logger;
        }

        private long GetCompanyId()
        {
            var compIdStr = User.FindFirst("CompanyId")?.Value;
            if (string.IsNullOrEmpty(compIdStr) || !long.TryParse(compIdStr, out var companyId))
            {
                return 1;
            }
            return companyId;
        }

        private long GetUserId()
        {
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return long.TryParse(userIdStr, out var userId) ? userId : 0;
        }

        [Authorize]
        [HttpGet("changes")]
        public async Task<IActionResult> Index(
            [FromQuery] string? category = null,
            [FromQuery] string? severity = null,
            [FromQuery] string? status = null,
            [FromQuery] long? machine_id = null,
            [FromQuery] int? days = null,
            [FromQuery] DateTime? date_from = null,
            [FromQuery] DateTime? date_to = null,
            [FromQuery] int page = 1,
            [FromQuery] int per_page = 50)
        {
            try
            {
                var companyId = GetCompanyId();
                var query = _dbContext.ChangeHistories
                    .Include(c => c.Machine)
                    .Where(c => c.CompanyId == companyId);

                if (!string.IsNullOrEmpty(category))
                    query = query.Where(c => c.Category == category);

                if (!string.IsNullOrEmpty(severity))
                    query = query.Where(c => c.Severity == severity);

                if (!string.IsNullOrEmpty(status))
                    query = query.Where(c => c.Status == status);

                if (machine_id.HasValue)
                    query = query.Where(c => c.MachineId == machine_id.Value);

                if (days.HasValue)
                {
                    var since = DateTime.UtcNow.AddDays(-days.Value);
                    query = query.Where(c => c.DetectedAt >= since);
                }

                if (date_from.HasValue)
                    query = query.Where(c => c.DetectedAt >= date_from.Value);

                if (date_to.HasValue)
                {
                    var toLimit = date_to.Value.Date.AddDays(1).AddTicks(-1);
                    query = query.Where(c => c.DetectedAt <= toLimit);
                }

                var total = await query.CountAsync();
                var items = await query
                    .OrderByDescending(c => c.DetectedAt)
                    .Skip((page - 1) * per_page)
                    .Take(per_page)
                    .Select(c => new
                    {
                        c.Id,
                        category = c.Category,
                        change_type = c.ChangeType,
                        severity = c.Severity,
                        status = c.Status,
                        item_label = c.ItemLabel,
                        item_identifier = c.ItemIdentifier,
                        description = c.Description,
                        previous_value = c.PreviousValue,
                        new_value = c.NewValue,
                        detected_at = c.DetectedAt,
                        metadata = c.Metadata,
                        machine = c.Machine != null ? new { hostname = c.Machine.Hostname, device_name = c.Machine.DeviceName } : null
                    })
                    .ToListAsync();

                return Ok(new
                {
                    data = items,
                    total,
                    current_page = page,
                    per_page,
                    last_page = (int)Math.Ceiling((double)total / per_page)
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get changes history");
                return StatusCode(500, ApiResponse.Fail("Failed to retrieve change history."));
            }
        }

        [Authorize]
        [HttpGet("machines/{id}/changes")]
        public async Task<IActionResult> MachineChanges(
            long id,
            [FromQuery] string? category = null,
            [FromQuery] string? severity = null,
            [FromQuery] string? status = null,
            [FromQuery] int? days = null,
            [FromQuery] DateTime? date_from = null,
            [FromQuery] DateTime? date_to = null,
            [FromQuery] int page = 1,
            [FromQuery] int per_page = 50)
        {
            try
            {
                var companyId = GetCompanyId();
                var machine = await _dbContext.Machines.FirstOrDefaultAsync(m => m.Id == id && m.CompanyId == companyId);
                if (machine == null) return NotFound(ApiResponse.Fail("Machine not found."));

                var query = _dbContext.ChangeHistories.Where(c => c.MachineId == machine.Id);

                if (!string.IsNullOrEmpty(category))
                    query = query.Where(c => c.Category == category);

                if (!string.IsNullOrEmpty(severity))
                    query = query.Where(c => c.Severity == severity);

                if (!string.IsNullOrEmpty(status))
                    query = query.Where(c => c.Status == status);

                if (days.HasValue)
                {
                    var since = DateTime.UtcNow.AddDays(-days.Value);
                    query = query.Where(c => c.DetectedAt >= since);
                }

                if (date_from.HasValue)
                    query = query.Where(c => c.DetectedAt >= date_from.Value);

                if (date_to.HasValue)
                {
                    var toLimit = date_to.Value.Date.AddDays(1).AddTicks(-1);
                    query = query.Where(c => c.DetectedAt <= toLimit);
                }

                var total = await query.CountAsync();
                var items = await query
                    .OrderByDescending(c => c.DetectedAt)
                    .Skip((page - 1) * per_page)
                    .Take(per_page)
                    .Select(c => new
                    {
                        c.Id,
                        category = c.Category,
                        change_type = c.ChangeType,
                        severity = c.Severity,
                        status = c.Status,
                        item_label = c.ItemLabel,
                        item_identifier = c.ItemIdentifier,
                        description = c.Description,
                        previous_value = c.PreviousValue,
                        new_value = c.NewValue,
                        detected_at = c.DetectedAt,
                        metadata = c.Metadata
                    })
                    .ToListAsync();

                return Ok(ApiResponse<object>.Ok(new
                {
                    data = items,
                    current_page = page,
                    per_page,
                    total,
                    last_page = (int)Math.Ceiling((double)total / per_page)
                }));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get machine changes");
                return StatusCode(500, ApiResponse.Fail("Failed to retrieve machine changes."));
            }
        }

        [Authorize]
        [HttpGet("changes/recent")]
        public async Task<IActionResult> RecentChanges([FromQuery] int days = 7, [FromQuery] int limit = 10)
        {
            try
            {
                var companyId = GetCompanyId();
                var since = DateTime.UtcNow.AddDays(-days);

                var changes = await _dbContext.ChangeHistories
                    .Include(c => c.Machine)
                    .Where(c => c.CompanyId == companyId && c.DetectedAt >= since)
                    .OrderByDescending(c => c.DetectedAt)
                    .Take(limit)
                    .ToListAsync();

                return Ok(ApiResponse<IEnumerable<ChangeHistory>>.Ok(changes, "Recent changes retrieved successfully."));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get recent changes");
                return StatusCode(500, ApiResponse.Fail("Failed to retrieve recent changes."));
            }
        }

        [Authorize]
        [HttpGet("changes/summary")]
        public async Task<IActionResult> Summary([FromQuery] int days = 7)
        {
            try
            {
                var companyId = GetCompanyId();
                var since = DateTime.UtcNow.AddDays(-days);

                var categoryCounts = await _dbContext.ChangeHistories
                    .Where(c => c.CompanyId == companyId && c.DetectedAt >= since)
                    .GroupBy(c => new { c.Category, c.ChangeType })
                    .Select(g => new
                    {
                        Category = g.Key.Category ?? "Unknown",
                        ChangeType = g.Key.ChangeType ?? "Unknown",
                        Count = g.Count()
                    })
                    .ToListAsync();

                var totalChanges = categoryCounts.Sum(x => x.Count);
                
                var byCategory = categoryCounts
                    .GroupBy(x => x.Category)
                    .ToDictionary(
                        cg => cg.Key,
                        cg => new
                        {
                            total = cg.Sum(x => x.Count),
                            by_type = cg.ToDictionary(x => x.ChangeType, x => x.Count)
                        }
                    );

                return Ok(ApiResponse<object>.Ok(new
                {
                    total_changes = totalChanges,
                    by_category = byCategory,
                    detail = categoryCounts
                }, "Change summary retrieved successfully."));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get change summary");
                return StatusCode(500, ApiResponse.Fail("Failed to retrieve change summary."));
            }
        }

        [Authorize]
        [HttpPut("changes/{id}/status")]
        public async Task<IActionResult> UpdateStatus(long id, [FromBody] JsonElement body)
        {
            try
            {
                if (!body.TryGetProperty("status", out var statusProp) || string.IsNullOrEmpty(statusProp.GetString()))
                {
                    return BadRequest(ApiResponse.Fail("status is required."));
                }

                var status = statusProp.GetString()!;
                var note = body.TryGetProperty("note", out var noteProp) ? noteProp.GetString() : null;

                var companyId = GetCompanyId();
                var change = await _dbContext.ChangeHistories
                    .FirstOrDefaultAsync(c => c.Id == id && c.CompanyId == companyId);

                if (change == null) return NotFound(ApiResponse.Fail("Change record not found."));

                // Deserialise existing metadata or init
                var metadata = string.IsNullOrEmpty(change.Metadata) 
                    ? new Dictionary<string, object>()
                    : JsonSerializer.Deserialize<Dictionary<string, object>>(change.Metadata) ?? new Dictionary<string, object>();

                var userId = GetUserId();
                metadata["status_updated_by"] = userId;
                metadata["status_updated_at"] = DateTime.UtcNow.ToString("o");
                if (!string.IsNullOrEmpty(note))
                {
                    metadata["status_note"] = note;
                }

                change.Status = status;
                change.Metadata = JsonSerializer.Serialize(metadata);

                await _dbContext.SaveChangesAsync();

                return Ok(ApiResponse<ChangeHistory>.Ok(change, "Change status updated successfully."));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update change status: {ChangeId}", id);
                return StatusCode(500, ApiResponse.Fail("Failed to update change status."));
            }
        }

        [HttpPost("agent/changes")]
        public async Task<IActionResult> SubmitAgentChanges([FromBody] JsonElement rawPayload)
        {
            try
            {
                var machineUid = ResolveMachineUid(rawPayload);
                if (string.IsNullOrEmpty(machineUid))
                    return UnprocessableEntity(ApiResponse.Fail("Machine identifier is required."));

                var machine = await _dbContext.Machines.FirstOrDefaultAsync(m => m.MachineUid == machineUid);
                if (machine == null)
                    return NotFound(ApiResponse.Fail("Machine not found. Send health payload first."));

                if (!machine.CompanyId.HasValue)
                    return UnprocessableEntity(ApiResponse.Fail("Machine has no company assigned."));

                var changesProp = rawPayload.GetPropertyOrNull("changes");
                if (changesProp == null || changesProp.Value.ValueKind != JsonValueKind.Array)
                    return BadRequest(ApiResponse.Fail("changes (array) is required."));

                var companyId = machine.CompanyId.Value;
                var incoming = new List<ChangeHistory>();

                foreach (var c in changesProp.Value.EnumerateArray())
                {
                    incoming.Add(new ChangeHistory
                    {
                        CompanyId = companyId,
                        MachineId = machine.Id,
                        Category = c.GetStringProperty("category") ?? "unknown",
                        ChangeType = c.GetStringProperty("change_type") ?? "unknown",
                        Severity = c.GetStringProperty("severity") ?? "information",
                        ItemIdentifier = c.GetStringProperty("item_identifier"),
                        ItemLabel = c.GetStringProperty("item_label"),
                        PreviousValue = c.GetStringProperty("previous_value"),
                        NewValue = c.GetStringProperty("new_value"),
                        Description = c.GetStringProperty("description"),
                        Status = "new",
                        DetectedAt = DateTimeOffset.TryParse(c.GetStringProperty("detected_at"), out var dto) ? dto.UtcDateTime : DateTime.UtcNow,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    });
                }

                _dbContext.ChangeHistories.AddRange(incoming);
                await _dbContext.SaveChangesAsync();

                _logger.LogInformation("Agent changes saved for machine {MachineId} ({Count} changes)", machine.Id, incoming.Count);
                return Ok(ApiResponse.Ok($"Changes saved ({incoming.Count} records)."));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process agent changes");
                return StatusCode(500, ApiResponse.Fail("Failed to process changes."));
            }
        }

        private string ResolveMachineUid(JsonElement payload)
        {
            var candidates = new[]
            {
                payload.GetStringProperty("machineId"),
                payload.GetStringProperty("machine_uid"),
                payload.GetStringProperty("machineUid"),
                payload.GetStringProperty("agentId"),
                Request.Headers["X-Agent-Id"].ToString()
            };

            foreach (var candidate in candidates)
            {
                if (!string.IsNullOrEmpty(candidate)) return candidate;
            }

            return string.Empty;
        }
    }
}
