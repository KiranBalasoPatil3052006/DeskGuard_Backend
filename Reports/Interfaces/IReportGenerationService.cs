using System.Threading.Tasks;
using DeskGuardBackend.Reports.Models;

namespace DeskGuardBackend.Reports.Interfaces
{
    public interface IReportGenerationService
    {
        Task<AmcHealthSummaryReportData> GetAmcHealthSummaryDataAsync(AmcHealthSummaryQueryParameters queryParams);
        byte[] GenerateAmcHealthSummaryPdf(AmcHealthSummaryReportData data);
        Task<AssetInventoryReportData> GetAssetInventoryDataAsync(AssetInventoryQueryParameters queryParams);
        byte[] GenerateAssetInventoryPdf(AssetInventoryReportData data);
    }
}
