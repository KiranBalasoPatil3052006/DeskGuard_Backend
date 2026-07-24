using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using DeskGuardBackend.DTOs.Common;
using DeskGuardBackend.Entities;
using DeskGuardBackend.Data;
using DeskGuardBackend.Reports.Interfaces;
using DeskGuardBackend.Reports.Models;
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
        private readonly IReportGenerationService _reportService;
        private readonly ILogger<ReportController> _logger;

        public ReportController(
            DeskGuardDbContext dbContext,
            IReportGenerationService reportService,
            ILogger<ReportController> logger)
        {
            _dbContext = dbContext;
            _reportService = reportService;
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

        private string GetUserName()
        {
            return User.FindFirst("Name")?.Value ?? User.FindFirst(ClaimTypes.Name)?.Value ?? "Administrator";
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            try
            {
                var companyId = GetCompanyId();
                var reports = await _dbContext.Reports
                    .AsNoTracking()
                    .Where(r => r.CompanyId == companyId)
                    .OrderByDescending(r => r.CreatedAt)
                    .Select(r => new ReportListItemDto
                    {
                        Id = r.Id,
                        Type = r.ReportType ?? "custom",
                        Format = r.Format ?? "pdf",
                        GeneratedBy = r.GeneratorName ?? "Administrator",
                        GeneratedAt = r.CreatedAt,
                        Status = r.Status ?? "completed",
                        Title = r.Title
                    })
                    .ToListAsync();

                return Ok(new { data = reports, total = reports.Count, last_page = 1 });
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

                var type = typeProp.GetString()!;
                var format = "pdf"; // Default to PDF
                if (body.TryGetProperty("format", out var formatProp) && !string.IsNullOrEmpty(formatProp.GetString()))
                {
                    format = formatProp.GetString()!;
                }

                var companyId = GetCompanyId();
                var userId = GetUserId();
                var userName = GetUserName();

                byte[]? pdfBytes = null;
                string reportTitle;

                // Generate real PDF content based on report type
                switch (type.ToLower())
                {
                    case "health":
                        var healthParams = new AmcHealthSummaryQueryParameters { CompanyId = companyId };
                        // Parse optional date filters from request body
                        if (body.TryGetProperty("filters", out var healthFilters) && healthFilters.ValueKind == JsonValueKind.Object)
                        {
                            if (healthFilters.TryGetProperty("date_from", out var hdf) && DateTime.TryParse(hdf.GetString(), out var hDateFrom))
                                healthParams.DateFrom = hDateFrom;
                            if (healthFilters.TryGetProperty("date_to", out var hdt) && DateTime.TryParse(hdt.GetString(), out var hDateTo))
                                healthParams.DateTo = hDateTo;
                            if (healthFilters.TryGetProperty("amc_plan", out var hPlan))
                                healthParams.AmcPlan = hPlan.GetString();
                        }
                        var healthData = await _reportService.GetAmcHealthSummaryDataAsync(healthParams);
                        healthData.GeneratedBy = userName;
                        pdfBytes = _reportService.GenerateAmcHealthSummaryPdf(healthData);
                        reportTitle = $"AMC Health Summary - {healthData.CompanyName} - {DateTime.UtcNow:yyyy-MM-dd}";
                        break;

                    case "inventory":
                        var invParams = new AssetInventoryQueryParameters { CompanyId = companyId };
                        if (body.TryGetProperty("filters", out var invFilters) && invFilters.ValueKind == JsonValueKind.Object)
                        {
                            if (invFilters.TryGetProperty("date_from", out var idf) && DateTime.TryParse(idf.GetString(), out var iDateFrom))
                                invParams.DateFrom = iDateFrom;
                            if (invFilters.TryGetProperty("date_to", out var idt) && DateTime.TryParse(idt.GetString(), out var iDateTo))
                                invParams.DateTo = iDateTo;
                            if (invFilters.TryGetProperty("machine_id", out var mid) && long.TryParse(mid.GetString(), out var machineId))
                                invParams.MachineId = machineId;
                            if (invFilters.TryGetProperty("amc_plan", out var iPlan))
                                invParams.AmcPlan = iPlan.GetString();
                        }
                        var invData = await _reportService.GetAssetInventoryDataAsync(invParams);
                        invData.GeneratedBy = userName;
                        pdfBytes = _reportService.GenerateAssetInventoryPdf(invData);
                        reportTitle = $"Asset Inventory - {invData.CompanyName} - {DateTime.UtcNow:yyyy-MM-dd}";
                        break;

                    case "security":
                        // Security report uses the health summary engine with a security-focused title
                        var secParams = new AmcHealthSummaryQueryParameters { CompanyId = companyId };
                        if (body.TryGetProperty("filters", out var secFilters) && secFilters.ValueKind == JsonValueKind.Object)
                        {
                            if (secFilters.TryGetProperty("date_from", out var sdf) && DateTime.TryParse(sdf.GetString(), out var sDateFrom))
                                secParams.DateFrom = sDateFrom;
                            if (secFilters.TryGetProperty("date_to", out var sdt) && DateTime.TryParse(sdt.GetString(), out var sDateTo))
                                secParams.DateTo = sDateTo;
                        }
                        var secData = await _reportService.GetAmcHealthSummaryDataAsync(secParams);
                        secData.GeneratedBy = userName;
                        pdfBytes = _reportService.GenerateAmcHealthSummaryPdf(secData);
                        reportTitle = $"Security Report - {secData.CompanyName} - {DateTime.UtcNow:yyyy-MM-dd}";
                        break;

                    default:
                        // Custom report type — use health summary as a general baseline
                        var customParams = new AmcHealthSummaryQueryParameters { CompanyId = companyId };
                        if (body.TryGetProperty("filters", out var customFilters) && customFilters.ValueKind == JsonValueKind.Object)
                        {
                            if (customFilters.TryGetProperty("date_from", out var cdf) && DateTime.TryParse(cdf.GetString(), out var cDateFrom))
                                customParams.DateFrom = cDateFrom;
                            if (customFilters.TryGetProperty("date_to", out var cdt) && DateTime.TryParse(cdt.GetString(), out var cDateTo))
                                customParams.DateTo = cDateTo;
                        }
                        var customData = await _reportService.GetAmcHealthSummaryDataAsync(customParams);
                        customData.GeneratedBy = userName;
                        pdfBytes = _reportService.GenerateAmcHealthSummaryPdf(customData);
                        reportTitle = $"{type.ToUpper()} Report - {customData.CompanyName} - {DateTime.UtcNow:yyyy-MM-dd}";
                        break;
                }

                var report = new Report
                {
                    CompanyId = companyId,
                    GeneratedBy = userId,
                    GeneratorName = userName,
                    Title = reportTitle,
                    ReportType = type,
                    Format = format,
                    Status = "completed",
                    FilePath = $"Reports/{type}_{Guid.NewGuid():N}.pdf",
                    FileContent = pdfBytes,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                await _dbContext.Reports.AddAsync(report);
                await _dbContext.SaveChangesAsync();

                var responseDto = new ReportListItemDto
                {
                    Id = report.Id,
                    Type = report.ReportType ?? "custom",
                    Format = report.Format ?? "pdf",
                    GeneratedBy = report.GeneratorName ?? "Administrator",
                    GeneratedAt = report.CreatedAt,
                    Status = report.Status ?? "completed",
                    Title = report.Title
                };

                return StatusCode(201, ApiResponse<ReportListItemDto>.Ok(responseDto, "Report generated successfully."));
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ApiResponse.Fail(ex.Message));
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

                // Serve real PDF content if available
                if (report.FileContent != null && report.FileContent.Length > 0)
                {
                    var sanitizedTitle = SanitizeFileName(report.Title ?? "Report");
                    var fileName = $"{sanitizedTitle}.pdf";
                    return File(report.FileContent, "application/pdf", fileName);
                }

                // Fallback: regenerate the report on-the-fly
                byte[]? regeneratedBytes = null;
                var userName = GetUserName();

                try
                {
                    switch (report.ReportType?.ToLower())
                    {
                        case "health":
                            var healthParams = new AmcHealthSummaryQueryParameters { CompanyId = companyId };
                            var healthData = await _reportService.GetAmcHealthSummaryDataAsync(healthParams);
                            healthData.GeneratedBy = userName;
                            regeneratedBytes = _reportService.GenerateAmcHealthSummaryPdf(healthData);
                            break;
                        case "inventory":
                            var invParams = new AssetInventoryQueryParameters { CompanyId = companyId };
                            var invData = await _reportService.GetAssetInventoryDataAsync(invParams);
                            invData.GeneratedBy = userName;
                            regeneratedBytes = _reportService.GenerateAssetInventoryPdf(invData);
                            break;
                        case "security":
                        case "custom":
                        default:
                            var defaultParams = new AmcHealthSummaryQueryParameters { CompanyId = companyId };
                            var defaultData = await _reportService.GetAmcHealthSummaryDataAsync(defaultParams);
                            defaultData.GeneratedBy = userName;
                            regeneratedBytes = _reportService.GenerateAmcHealthSummaryPdf(defaultData);
                            break;
                    }
                }
                catch (Exception regenEx)
                {
                    _logger.LogWarning(regenEx, "Failed to regenerate report {ReportId}, returning placeholder", id);
                }

                if (regeneratedBytes != null && regeneratedBytes.Length > 0)
                {
                    // Cache the regenerated content for future downloads
                    report.FileContent = regeneratedBytes;
                    report.UpdatedAt = DateTime.UtcNow;
                    await _dbContext.SaveChangesAsync();

                    var sanitizedTitle = SanitizeFileName(report.Title ?? "Report");
                    return File(regeneratedBytes, "application/pdf", $"{sanitizedTitle}.pdf");
                }

                // Ultimate fallback: return a basic text placeholder
                var stream = new MemoryStream();
                var writer = new StreamWriter(stream);
                writer.WriteLine("DeskGuard Report");
                writer.WriteLine($"Report Name: {report.Title}");
                writer.WriteLine($"Type: {report.ReportType}");
                writer.WriteLine($"Format: {report.Format}");
                writer.WriteLine($"Generated At: {report.CreatedAt}");
                writer.Flush();
                stream.Position = 0;

                return File(stream, "application/octet-stream", $"{report.Title}.txt");
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

        private string SanitizeFileName(string name)
        {
            var invalidChars = Path.GetInvalidFileNameChars();
            var clean = new string(name.Where(c => !invalidChars.Contains(c)).ToArray());
            return clean.Replace(" ", "_");
        }
    }

    /// <summary>
    /// Response DTO for the reports list, using snake_case-friendly naming
    /// that the React frontend expects.
    /// </summary>
    public class ReportListItemDto
    {
        [System.Text.Json.Serialization.JsonPropertyName("id")]
        public long Id { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("type")]
        public string Type { get; set; } = "custom";

        [System.Text.Json.Serialization.JsonPropertyName("format")]
        public string Format { get; set; } = "pdf";

        [System.Text.Json.Serialization.JsonPropertyName("generated_by")]
        public string GeneratedBy { get; set; } = "Administrator";

        [System.Text.Json.Serialization.JsonPropertyName("generated_at")]
        public DateTime GeneratedAt { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("status")]
        public string Status { get; set; } = "completed";

        [System.Text.Json.Serialization.JsonPropertyName("title")]
        public string Title { get; set; } = string.Empty;
    }
}
