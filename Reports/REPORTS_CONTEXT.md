# DeskGuard - Reports Engine Context

## Overview
The Reports Engine is an enterprise-grade module designed to aggregate endpoint telemetry and contract details to produce professional compliance and hardware inventory reports for IT Managers, AMC Companies, and Auditors.

---

## Architecture
The reporting module is isolated and structured into three layers:
1. **API Controller (`ReportApiController.cs`)**: Validates administrative roles (Admin/Super Admin), enforces company tenant boundaries, parses query parameters, and streams PDF file responses to the client.
2. **Service Layer (`ReportGenerationService.cs`)**: Queries PostgreSQL concurrently using Entity Framework, executes compliance algorithms (such as health scoring), collects change records, and synthesizes data-driven recommendations.
3. **PDF Generation Layer (`AmcHealthSummaryReportDocument.cs`, `AssetInventoryReportDocument.cs`)**: Fluent layouts built on QuestPDF that draw aligned, styled PDF documents with page numbering, cards, and data tables.

---

## API Endpoints

### 1. AMC Health Summary Report
- **Endpoint**: `GET /api/reports/amc-health-summary`
- **Purpose**: Executive summaries of compliance health, recent hardware/software changes, and alerts.
- **Filters**:
  - `CompanyId` / `CustomerId` (long): Target company.
  - `DateFrom` / `StartDate` (date): Start date range.
  - `DateTo` / `EndDate` (date): End date range.
  - `AmcPlan` (string): Plan override parameter.

### 2. Asset Inventory Report
- **Endpoint**: `GET /api/reports/asset-inventory`
- **Purpose**: Exhaustive listing of hardware inventory details (CPU models/threads, RAM capacity/modules, Storage drive types/SMART warnings, Motherboards, Batteries, Network interfaces, and connected USB devices).
- **Filters**:
  - `CompanyId` / `CustomerId` (long): Target company.
  - `MachineId` (long): Filter to a specific monitored machine.
  - `DateFrom` / `StartDate` (date): Start date range.
  - `DateTo` / `EndDate` (date): End date range.
  - `AmcPlan` (string): Plan override parameter.

---

## Database Telemetry Queries & Optimizations
- **Concurrency**: Avoids N+1 querying by fetching all sub-tables (Disks, Connected Devices, Network Adapters, current statuses) concurrently using list mappings keyed by `MachineId`.
- **Performance**: Enforces `.AsNoTracking()` on all DB queries to bypass EF tracker overhead.
- **Null Safety**: Safe fallbacks (such as displaying "Not Available") are populated if any hardware telemetry field has not been synchronized by the agent yet.

---

## PDF Styling & Branding
- **Grid Layouts**: Flexible tabular configurations for hardware summaries.
- **Executive Summary Cards**: High-level indicator cards with distinct border colors and icons.
- **Uniformity**: Uses matching Segoe UI fonts, slate margins, confidential footer labels, and dynamic page calculations (`Page X of Y`).
