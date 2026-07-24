using System.Linq;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;
using DeskGuardBackend.Reports.Models;

namespace DeskGuardBackend.Reports.PDF.Machine
{
    public class MachineAlertHistoryDocument : MachineReportBaseDocument
    {
        private readonly MachineAlertHistoryReportData _data;

        public MachineAlertHistoryDocument(MachineAlertHistoryReportData data) : base(data.Metadata)
        {
            _data = data;
        }

        protected override void ComposeReportContent(IContainer container)
        {
            container.Column(col =>
            {
                // Summary
                col.Item().Element(c => SectionTitle(c, "Alert Summary"));
                col.Item().PaddingBottom(12f).Element(ComposeSummary);

                // Severity breakdown
                col.Item().Element(c => SectionTitle(c, "Severity Breakdown"));
                col.Item().PaddingBottom(12f).Element(ComposeSeverityBreakdown);

                // Detail table
                col.Item().Element(c => SectionTitle(c, $"Alert History ({_data.TotalAlerts} records)"));
                col.Item().Element(ComposeAlertTable);
            });
        }

        private void ComposeSummary(IContainer container)
        {
            container.Row(row =>
            {
                row.RelativeItem().Element(c => MetricCard(c, "Total Alerts", _data.TotalAlerts.ToString(), ColorPrimary));
                row.ConstantItem(8f);
                row.RelativeItem().Element(c => MetricCard(c, "Open", _data.OpenAlerts.ToString(), ColorWarning));
                row.ConstantItem(8f);
                row.RelativeItem().Element(c => MetricCard(c, "Acknowledged", _data.AcknowledgedAlerts.ToString(), ColorInfo));
                row.ConstantItem(8f);
                row.RelativeItem().Element(c => MetricCard(c, "Resolved", _data.ResolvedAlerts.ToString(), ColorSuccess));
            });
        }

        private void ComposeSeverityBreakdown(IContainer container)
        {
            container.Row(row =>
            {
                row.RelativeItem().Element(c => MetricCard(c, "Critical", _data.CriticalAlerts.ToString(), ColorDanger));
                row.ConstantItem(8f);
                row.RelativeItem().Element(c => MetricCard(c, "Warning", _data.WarningAlerts.ToString(), ColorWarning));
                row.ConstantItem(8f);
                row.RelativeItem().Element(c => MetricCard(c, "Information", _data.InfoAlerts.ToString(), ColorInfo));
            });
        }

        private void ComposeAlertTable(IContainer container)
        {
            if (!_data.Alerts.Any())
            {
                container.Padding(8f).Text(t => t.Span("No alerts recorded for the selected period.").Italic().FontColor(ColorTextSecondary));
                return;
            }

            container.Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn(2.5f);  // Title
                    columns.RelativeColumn(0.8f);   // Severity
                    columns.RelativeColumn(0.8f);   // Status
                    columns.RelativeColumn(1.3f);   // Created
                    columns.RelativeColumn(1.3f);   // Resolved
                    columns.RelativeColumn(2f);     // Description
                });

                table.Header(header =>
                {
                    header.Cell().Element(TableHeaderCell).Text("Title");
                    header.Cell().Element(TableHeaderCell).Text("Severity");
                    header.Cell().Element(TableHeaderCell).Text("Status");
                    header.Cell().Element(TableHeaderCell).Text("Created");
                    header.Cell().Element(TableHeaderCell).Text("Resolved");
                    header.Cell().Element(TableHeaderCell).Text("Description");
                });

                int idx = 0;
                foreach (var alert in _data.Alerts)
                {
                    bool isEven = idx % 2 == 0;
                    table.Cell().Element(c => TableDataCell(c, isEven)).Text(t => t.Span(alert.Title).FontSize(7.5f));
                    table.Cell().Element(c => TableDataCell(c, isEven)).Text(t => t.Span(alert.Severity).FontSize(7.5f).FontColor(GetSeverityColor(alert.Severity)));
                    table.Cell().Element(c => TableDataCell(c, isEven)).Text(t => t.Span(alert.Status).FontSize(7.5f).FontColor(GetStatusColor(alert.Status)));
                    table.Cell().Element(c => TableDataCell(c, isEven)).Text(t => t.Span(alert.CreatedAt).FontSize(7f).FontColor(ColorTextSecondary));
                    table.Cell().Element(c => TableDataCell(c, isEven)).Text(t => t.Span(alert.ResolvedAt).FontSize(7f).FontColor(ColorTextSecondary));
                    table.Cell().Element(c => TableDataCell(c, isEven)).Text(t => t.Span(alert.Description).FontSize(7f));
                    idx++;
                }
            });
        }
    }
}
