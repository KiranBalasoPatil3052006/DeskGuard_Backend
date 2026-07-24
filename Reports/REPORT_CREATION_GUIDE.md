# DeskGuard — How to Create a New Report Type

This guide walks through every step needed to add a new report format to DeskGuard. By following this structure, you can introduce any new PDF report (e.g. "Software Audit Report", "Login Activity Report", "Change History Report") in a consistent, maintainable way.

---

## Architecture Overview

Each report type in DeskGuard follows a 4-layer architecture:

```
Frontend (React)                    Backend (ASP.NET Core)
┌──────────────────┐               ┌─────────────────────────────┐
│ ReportsList.jsx  │──HTTP GET────▶│ ReportApiController.cs      │
│ (modal + button) │               │ (route + auth + streaming)  │
│                  │◀──PDF blob────│                             │
└──────────────────┘               └──────────┬──────────────────┘
                                              │
                                   ┌──────────▼──────────────────┐
                                   │ ReportGenerationService.cs  │
                                   │ (DB queries + data mapping) │
                                   └──────────┬──────────────────┘
                                              │
                                   ┌──────────▼──────────────────┐
                                   │ YourReportDocument.cs       │
                                   │ (QuestPDF layout)           │
                                   └─────────────────────────────┘
```

The key files you will touch are:

| Layer | File | What You Add |
|---|---|---|
| Models | `Reports/Models/ReportModels.cs` | Query parameters class + report data class |
| Service Interface | `Reports/Interfaces/IReportGenerationService.cs` | Two new method signatures |
| Service Implementation | `Reports/Services/ReportGenerationService.cs` | DB queries + data mapping logic |
| PDF Layout | `Reports/PDF/YourReportDocument.cs` | QuestPDF layout definition |
| API Controller | `Reports/Controllers/ReportApiController.cs` | New HTTP endpoint |
| Generate Flow | `Controllers/ReportController.cs` | New case in the Generate switch |
| Frontend Service | `src/services/reports.js` | New API download function |
| Frontend Page | `src/pages/reports/ReportsList.jsx` | New button + modal |

---

## Step-by-Step Guide

### Step 1: Define the Query Parameters and Data Model

Open `Reports/Models/ReportModels.cs` and add two new classes. The first is the query parameters class that captures what the user passes in from the modal (Company ID, date range, etc). The second is the data class that holds all the information your PDF will render.

```csharp
// Query Parameters — what the API endpoint accepts
public class SoftwareAuditQueryParameters
{
    public long? CompanyId { get; set; }
    public long? CustomerId { get; set; }     // Alias for CompanyId
    public long? MachineId { get; set; }       // Optional single-machine filter
    public DateTime? DateFrom { get; set; }
    public DateTime? DateTo { get; set; }
    public DateTime? StartDate { get; set; }   // Alias for DateFrom
    public DateTime? EndDate { get; set; }     // Alias for DateTo
}

// Report Data — populated by the service, consumed by the PDF
public class SoftwareAuditReportData
{
    public string ReportId { get; set; } = string.Empty;
    public string CompanyName { get; set; } = string.Empty;
    public string ReportPeriodStr { get; set; } = string.Empty;
    public string GeneratedBy { get; set; } = string.Empty;
    public DateTime GeneratedAt { get; set; }

    // Add your report-specific data fields here
    public int TotalSystems { get; set; }
    public int TotalSoftwareInstalled { get; set; }
    public List<SoftwareAuditItem> Systems { get; set; } = new();
    public List<string> Recommendations { get; set; } = new();
}

// Row-level detail class
public class SoftwareAuditItem
{
    public string MachineName { get; set; } = string.Empty;
    public string MachineId { get; set; } = string.Empty;
    public string OperatingSystem { get; set; } = "Not Available";
    public List<SoftwareEntry> InstalledSoftware { get; set; } = new();
}

public class SoftwareEntry
{
    public string Name { get; set; } = string.Empty;
    public string Version { get; set; } = "Not Available";
    public string Publisher { get; set; } = "Not Available";
    public string InstalledDate { get; set; } = "Not Available";
}
```

**Design Tips:**
- Always include `CompanyId` and alias fields (`CustomerId`, `StartDate`/`EndDate`) for flexible input.
- Default every string field to `"Not Available"` or `string.Empty` — never return null in PDF render data.
- Use safe list initializers (`= new()`) to prevent null reference errors in the PDF layout.

---

### Step 2: Add Method Signatures to the Service Interface

Open `Reports/Interfaces/IReportGenerationService.cs` and add two methods — one to fetch the data from the database, and one to render the PDF:

```csharp
public interface IReportGenerationService
{
    // ... existing methods ...

    Task<SoftwareAuditReportData> GetSoftwareAuditDataAsync(SoftwareAuditQueryParameters queryParams);
    byte[] GenerateSoftwareAuditPdf(SoftwareAuditReportData data);
}
```

---

### Step 3: Implement the Data Query Service

Open `Reports/Services/ReportGenerationService.cs` and add the implementation. Follow these patterns from the existing code:

```csharp
public async Task<SoftwareAuditReportData> GetSoftwareAuditDataAsync(SoftwareAuditQueryParameters queryParams)
{
    // 1. Resolve parameters with fallback defaults
    var companyId = queryParams.CompanyId ?? queryParams.CustomerId ?? 1;
    var dateFrom = queryParams.DateFrom ?? queryParams.StartDate ?? DateTime.UtcNow.AddDays(-30);
    var dateTo = queryParams.DateTo ?? queryParams.EndDate ?? DateTime.UtcNow;

    // 2. Fetch company (throws KeyNotFoundException if not found)
    var company = await _dbContext.Companies
        .AsNoTracking()
        .FirstOrDefaultAsync(c => c.Id == companyId);

    if (company == null)
    {
        throw new KeyNotFoundException($"Company with ID {companyId} not found.");
    }

    // 3. Initialize the report data with metadata
    var reportData = new SoftwareAuditReportData
    {
        ReportId = Guid.NewGuid().ToString().Substring(0, 8).ToUpper(),
        CompanyName = company.Name,
        ReportPeriodStr = $"{dateFrom:yyyy-MM-dd} to {dateTo:yyyy-MM-dd}",
        GeneratedAt = DateTime.UtcNow
    };

    // 4. Query machines (always use AsNoTracking for report queries)
    var machinesQuery = _dbContext.Machines
        .AsNoTracking()
        .Where(m => m.CompanyId == companyId && m.IsActive);

    if (queryParams.MachineId.HasValue)
    {
        machinesQuery = machinesQuery.Where(m => m.Id == queryParams.MachineId.Value);
    }

    var machines = await machinesQuery.ToListAsync();
    reportData.TotalSystems = machines.Count;

    if (machines.Count == 0) return reportData;

    // 5. Fetch related data tables concurrently
    var machineIds = machines.Select(m => m.Id).ToList();
    var softwareList = await _dbContext.SoftwareInventories
        .AsNoTracking()
        .Where(s => machineIds.Contains(s.MachineId))
        .ToListAsync();

    // 6. Map data into report items (always handle nulls with fallbacks)
    foreach (var m in machines)
    {
        var machineSoftware = softwareList.Where(s => s.MachineId == m.Id).ToList();
        var item = new SoftwareAuditItem
        {
            MachineName = m.Hostname ?? m.DeviceName ?? "Unknown Device",
            MachineId = m.MachineUid,
            OperatingSystem = m.OperatingSystem ?? m.OsVersion ?? "Not Available",
            InstalledSoftware = machineSoftware.Select(s => new SoftwareEntry
            {
                Name = s.Name,
                Version = s.Version ?? "Not Available",
                Publisher = s.Publisher ?? "Not Available",
                InstalledDate = s.InstallDate ?? "Not Available"
            }).ToList()
        };
        reportData.Systems.Add(item);
        reportData.TotalSoftwareInstalled += machineSoftware.Count;
    }

    return reportData;
}

public byte[] GenerateSoftwareAuditPdf(SoftwareAuditReportData data)
{
    try
    {
        var doc = new SoftwareAuditReportDocument(data);
        return doc.GeneratePdf();
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Failed to compile Software Audit QuestPDF report");
        throw;
    }
}
```

**Critical Patterns to Follow:**
- Always use `.AsNoTracking()` on every query — these are read-only operations.
- Always check `company == null` and throw `KeyNotFoundException` — the controller catches this and returns 404.
- Always provide fallback strings ("Not Available", "Unknown Device") for nullable DB fields.
- Fetch related tables by `machineIds.Contains(...)` to avoid N+1 queries.

---

### Step 4: Build the PDF Layout

Create a new file at `Reports/PDF/SoftwareAuditReportDocument.cs`. Use the existing `AmcHealthSummaryReportDocument.cs` as a template. QuestPDF uses a fluent API:

```csharp
using System;
using System.Linq;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using DeskGuardBackend.Reports.Models;

namespace DeskGuardBackend.Reports.PDF
{
    public class SoftwareAuditReportDocument : IDocument
    {
        private readonly SoftwareAuditReportData _data;

        public SoftwareAuditReportDocument(SoftwareAuditReportData data)
        {
            _data = data;
        }

        public DocumentMetadata GetMetadata()
        {
            return new DocumentMetadata
            {
                Title = "Software Audit Report",
                Author = "DeskGuard Enterprise",
                Subject = $"Software Audit for {_data.CompanyName}"
            };
        }

        public DocumentSettings GetSettings() => DocumentSettings.Default;

        public void Compose(IDocumentContainer container)
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(30f, Unit.Point);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x
                    .FontFamily("Segoe UI")
                    .FontSize(9f)
                    .FontColor("#1F2937"));

                page.Header().Element(ComposeHeader);
                page.Content().Element(ComposeContent);
                page.Footer().Element(ComposeFooter);
            });
        }

        private void ComposeHeader(IContainer container)
        {
            container.PaddingBottom(15f).Row(row =>
            {
                row.RelativeItem().Column(col =>
                {
                    col.Item().Text(t => t.Span("DeskGuard")
                        .FontSize(18f).Bold().FontColor("#1E3A8A"));
                    col.Item().Text(t => t.Span("Software Audit Report")
                        .FontSize(14f).Bold().FontColor("#3B82F6"));
                    col.Item().Text(t => t.Span($"Customer: {_data.CompanyName}")
                        .FontSize(10f).SemiBold());
                });

                row.ConstantItem(180f).AlignRight().Column(col =>
                {
                    col.Item().Text(t => t.Span($"Report ID: #{_data.ReportId}")
                        .FontSize(8f).FontColor("#6B7280"));
                    col.Item().Text(t => t.Span($"Generated: {_data.GeneratedAt:yyyy-MM-dd HH:mm}")
                        .FontSize(8f).FontColor("#6B7280"));
                    col.Item().Text(t => t.Span($"By: {_data.GeneratedBy}")
                        .FontSize(8f).FontColor("#6B7280"));
                });
            });
        }

        private void ComposeContent(IContainer container)
        {
            container.Column(col =>
            {
                col.Item().LineHorizontal(1f).LineColor("#E5E7EB");
                col.Item().PaddingBottom(10f);

                // Add your report sections here using col.Item()
                // Example: col.Item().Element(ComposeSoftwareTable);
            });
        }

        private void ComposeFooter(IContainer container)
        {
            container.PaddingTop(10f).Column(col =>
            {
                col.Item().LineHorizontal(0.5f).LineColor("#E5E7EB");
                col.Item().PaddingTop(3f).Row(row =>
                {
                    row.RelativeItem().Text(t =>
                    {
                        t.Span("Confidential  |  ").FontSize(7f).FontColor("#9CA3AF");
                        t.Span(_data.CompanyName).FontSize(7f).Bold().FontColor("#9CA3AF");
                        t.Span(" - DeskGuard Enterprise Report")
                            .FontSize(7f).FontColor("#9CA3AF");
                    });
                    row.ConstantItem(80f).AlignRight().Text(x =>
                    {
                        x.Span("Page ").FontSize(7f).FontColor("#9CA3AF");
                        x.CurrentPageNumber().FontSize(7f).FontColor("#9CA3AF");
                        x.Span(" of ").FontSize(7f).FontColor("#9CA3AF");
                        x.TotalPages().FontSize(7f).FontColor("#9CA3AF");
                    });
                });
            });
        }
    }
}
```

**PDF Styling Conventions (keep consistent with existing reports):**
- Page: A4, 30pt margins, Segoe UI font, base size 9pt
- Brand color: `#1E3A8A` (dark blue), accent: `#3B82F6`
- Table header background: `#1E3A8A` with white text
- Alternating row colors: `#FFFFFF` / `#F9FAFB`
- Footer: "Confidential" label, company name, "Page X of Y"

---

### Step 5: Wire Up the API Endpoint

Open `Reports/Controllers/ReportApiController.cs` and add a new `[HttpGet]` endpoint:

```csharp
[HttpGet("software-audit")]
public async Task<IActionResult> GetSoftwareAuditReport(
    [FromQuery] SoftwareAuditQueryParameters queryParams)
{
    try
    {
        var userRole = GetCurrentUserRole();
        if (userRole != "Admin" && userRole != "Super Admin")
        {
            return Forbid();
        }

        var userCompanyId = GetCurrentUserCompanyId();
        var targetCompanyId = queryParams.CompanyId
                              ?? queryParams.CustomerId
                              ?? userCompanyId;

        if (userRole != "Super Admin" && targetCompanyId != userCompanyId)
        {
            return Forbid();
        }

        queryParams.CompanyId = targetCompanyId;
        var currentUserName = GetCurrentUserName();

        var reportData = await _reportService.GetSoftwareAuditDataAsync(queryParams);
        reportData.GeneratedBy = currentUserName;

        var pdfBytes = _reportService.GenerateSoftwareAuditPdf(reportData);

        var sanitizedName = SanitizeFileName(reportData.CompanyName);
        var fileName = $"Software_Audit_{sanitizedName}_{DateTime.UtcNow:yyyyMMdd}.pdf";

        return File(pdfBytes, "application/pdf", fileName);
    }
    catch (KeyNotFoundException ex)
    {
        return NotFound(ApiResponse.Fail(ex.Message));
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Failed to generate Software Audit report");
        return StatusCode(500, ApiResponse.Fail("Failed to generate report."));
    }
}
```

---

### Step 6: Add to the "Generate Report" Flow

Open `Controllers/ReportController.cs` and add a new case in the `Generate()` method's switch block:

```csharp
case "software":
    var swParams = new SoftwareAuditQueryParameters { CompanyId = companyId };
    // parse optional filters from body...
    var swData = await _reportService.GetSoftwareAuditDataAsync(swParams);
    swData.GeneratedBy = userName;
    pdfBytes = _reportService.GenerateSoftwareAuditPdf(swData);
    reportTitle = $"Software Audit - {swData.CompanyName} - {DateTime.UtcNow:yyyy-MM-dd}";
    break;
```

Also add `"software"` to the download fallback switch in the `Download()` method.

---

### Step 7: Add the Frontend API Function

Open `src/services/reports.js` and add:

```js
/** Download Software Audit Report PDF */
export function downloadSoftwareAuditReport(params) {
  return api.get('/reports/software-audit', { params, responseType: 'blob' });
}
```

---

### Step 8: Add the Frontend Button and Modal

Open `src/pages/reports/ReportsList.jsx` and follow the pattern of the existing AMC/Asset modals:

1. Add state variables for the new modal (e.g. `showSoftwareModal`, `softwareCompanyId`, etc.)
2. Add a handler function (copy `handleGenerateAmc` and adapt it)
3. Add a button in the header section
4. Add the modal JSX at the bottom (copy the AMC modal structure and rename fields)

The existing AMC and Asset modals are complete templates you can directly copy from.

---

### Step 9: Update the Type Options

If you want the new type to appear in the "Generate Report" modal dropdown, add it to the `TYPE_OPTIONS` array in `ReportsList.jsx`:

```js
const TYPE_OPTIONS = ['health', 'inventory', 'security', 'software', 'custom'];
```

---

## Available Database Tables for Reports

These are all the tables you can query from `ReportGenerationService.cs` via `_dbContext`:

| DbSet | Entity | Description |
|---|---|---|
| `Companies` | Company | Tenant data, AMC contract details |
| `Machines` | Machine | All monitored endpoints |
| `MachineCurrentStatuses` | MachineCurrentStatus | Real-time CPU/RAM/Disk/Battery/Network metrics |
| `HardwareInventories` | HardwareInventory | CPU model, RAM, GPU, Motherboard, BIOS details |
| `SoftwareInventories` | SoftwareInventory | Installed applications per machine |
| `MachineDisks` | MachineDisk | Disk drives (capacity, health, type) |
| `MachineNetworkAdapters` | MachineNetworkAdapter | Network interfaces (MAC, type, speed) |
| `MachineConnectedDevices` | MachineConnectedDevice | Peripherals (printers, webcams, keyboards) |
| `Alerts` | Alert | Alert records (severity, status, timestamps) |
| `ChangeHistories` | ChangeHistory | Hardware/software change audit trail |
| `LoginActivities` | LoginActivity | Login/logout events |
| `UsbActivities` | UsbActivity | USB connect/disconnect events |
| `WindowsServices` | WindowsService | Service statuses |
| `WindowsUpdates` | WindowsUpdate | Pending/installed updates |
| `EventLogs` | EventLog | Windows event log entries |
| `ProcessLogs` | ProcessLog | Running process snapshots |
| `StartupPrograms` | StartupProgram | Auto-start programs |
| `DeviceEvents` | DeviceEvent | Device connect/disconnect events |
| `HealthLogs` | HealthLog | Historical health metrics |

---

## Checklist for New Report Types

- [ ] Query parameters class in `ReportModels.cs`
- [ ] Report data class in `ReportModels.cs`
- [ ] Interface methods in `IReportGenerationService.cs`
- [ ] Data queries in `ReportGenerationService.cs`
- [ ] PDF layout in `Reports/PDF/YourReportDocument.cs`
- [ ] API endpoint in `ReportApiController.cs`
- [ ] Case in `ReportController.Generate()` switch
- [ ] Case in `ReportController.Download()` fallback switch
- [ ] Frontend API function in `reports.js`
- [ ] Frontend button + modal in `ReportsList.jsx`
- [ ] Updated `TYPE_OPTIONS` array (if needed)
- [ ] Context documentation updated in `REPORTS_CONTEXT.md`
