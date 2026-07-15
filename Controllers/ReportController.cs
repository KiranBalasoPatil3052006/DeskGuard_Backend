using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using DeskGuardBackend.DTOs.Common;
using DeskGuardBackend.Entities;
using DeskGuardBackend.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.IO;

namespace DeskGuardBackend.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/v1/reports")]
    public class ReportController : ControllerBase
    {
        private readonly DeskGuardDbContext _dbContext;
        private readonly ILogger<ReportController> _logger;

        public ReportController(DeskGuardDbContext dbContext, ILogger<ReportController> logger)
        {
            _dbContext = dbContext;
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

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            try
            {
                var companyId = GetCompanyId();
                var reports = await _dbContext.Reports
                    .Where(r => r.CompanyId == companyId)
                    .ToListAsync();
                return Ok(ApiResponse<IEnumerable<Report>>.Ok(reports, "Reports retrieved successfully."));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get reports");
                return StatusCode(500, ApiResponse.Fail("Failed to retrieve reports."));
            }
        }

        [HttpPost("generate")]
        public async Task<IActionResult> Generate([FromBody] JsonElement body)
        {
            try
            {
                if (!body.TryGetProperty("type", out var typeProp) || string.IsNullOrEmpty(typeProp.GetString()))
                {
                    return BadRequest(ApiResponse.Fail("type is required."));
                }
                if (!body.TryGetProperty("format", out var formatProp) || string.IsNullOrEmpty(formatProp.GetString()))
                {
                    return BadRequest(ApiResponse.Fail("format is required."));
                }

                var type = typeProp.GetString()!;
                var format = formatProp.GetString()!;
                
                var companyId = GetCompanyId();
                var userId = GetUserId();

                var report = new Report
                {
                    CompanyId = companyId,
                    GeneratedBy = userId,
                    Title = $"{type.ToUpper()} Report - {DateTime.UtcNow:yyyyMMdd_HHmmss}",
                    ReportType = type,
                    Format = format,
                    Status = "completed",
                    FilePath = $"Reports/mock_{Guid.NewGuid()}.{format.ToLower()}",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                await _dbContext.Reports.AddAsync(report);
                await _dbContext.SaveChangesAsync();

                return StatusCode(201, ApiResponse<Report>.Ok(report, "Report generated successfully."));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to generate report");
                return StatusCode(500, ApiResponse.Fail("Failed to generate report."));
            }
        }

        [HttpGet("{id}/download")]
        public async Task<IActionResult> Download(long id)
        {
            try
            {
                var companyId = GetCompanyId();
                var report = await _dbContext.Reports
                    .FirstOrDefaultAsync(r => r.Id == id && r.CompanyId == companyId);

                if (report == null) return NotFound(ApiResponse.Fail("Report not found."));

                // We return a mock file stream to satisfy the download request
                var stream = new MemoryStream();
                var writer = new StreamWriter(stream);
                writer.WriteLine("DeskGuard Mock Generated Report");
                writer.WriteLine($"Report Name: {report.Title}");
                writer.WriteLine($"Type: {report.ReportType}");
                writer.WriteLine($"Format: {report.Format}");
                writer.WriteLine($"Generated At: {report.CreatedAt}");
                writer.Flush();
                stream.Position = 0;

                var formatStr = report.Format?.ToLower() ?? "pdf";
                var contentType = formatStr switch
                {
                    "pdf" => "application/pdf",
                    "excel" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    "csv" => "text/csv",
                    _ => "application/octet-stream"
                };

                return File(stream, contentType, $"{report.Title}.{formatStr}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to download report {ReportId}", id);
                return StatusCode(500, ApiResponse.Fail("Failed to download report."));
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Destroy(long id)
        {
            try
            {
                var companyId = GetCompanyId();
                var report = await _dbContext.Reports
                    .FirstOrDefaultAsync(r => r.Id == id && r.CompanyId == companyId);

                if (report == null) return NotFound(ApiResponse.Fail("Report not found."));

                _dbContext.Reports.Remove(report);
                await _dbContext.SaveChangesAsync();

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete report {ReportId}", id);
                return StatusCode(500, ApiResponse.Fail("Failed to delete report."));
            }
        }
    }
}
