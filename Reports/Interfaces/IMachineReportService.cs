using System;
using System.Threading.Tasks;

namespace DeskGuardBackend.Reports.Interfaces
{
    /// <summary>
    /// Service interface for generating single-machine PDF reports.
    /// Supports 7 report types: health, hardware, performance, changes, alerts, activity, systemlog.
    /// </summary>
    public interface IMachineReportService
    {
        /// <summary>
        /// Generates a PDF report for a single machine.
        /// </summary>
        /// <param name="machineId">The machine's database ID.</param>
        /// <param name="reportType">One of: health, hardware, performance, changes, alerts, activity, systemlog.</param>
        /// <param name="dateFrom">Optional start date for filtering time-based data.</param>
        /// <param name="dateTo">Optional end date for filtering time-based data.</param>
        /// <param name="generatedBy">Display name of the user generating the report.</param>
        /// <param name="companyId">Company ID for tenant boundary validation.</param>
        /// <returns>PDF file bytes.</returns>
        Task<byte[]> GenerateReportAsync(long machineId, string reportType,
            DateTime? dateFrom, DateTime? dateTo, string generatedBy, long companyId);
    }
}
