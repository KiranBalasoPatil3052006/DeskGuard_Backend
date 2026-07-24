using System.Linq;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;
using DeskGuardBackend.Reports.Models;

namespace DeskGuardBackend.Reports.PDF.Machine
{
    public class MachineChangeTimelineDocument : MachineReportBaseDocument
    {
        private readonly MachineChangeTimelineReportData _data;

        public MachineChangeTimelineDocument(MachineChangeTimelineReportData data) : base(data.Metadata)
        {
            _data = data;
        }

        protected override void ComposeReportContent(IContainer container)
        {
            container.Column(col =>
            {
                // Summary
                col.Item().Element(c => SectionTitle(c, "Change Summary"));
                col.Item().PaddingBottom(12f).Element(ComposeSummary);

                // Detail table
                col.Item().Element(c => SectionTitle(c, $"Change Timeline ({_data.TotalChanges} records)"));
                col.Item().Element(ComposeChangeTable);
            });
        }

        private void ComposeSummary(IContainer container)
        {
            container.Row(row =>
            {
                row.RelativeItem().Element(c => MetricCard(c, "Total Changes", _data.TotalChanges.ToString(), ColorPrimary));
                row.ConstantItem(6f);
                row.RelativeItem().Element(c => MetricCard(c, "Hardware", _data.HardwareChanges.ToString(), ColorAccent));
                row.ConstantItem(6f);
                row.RelativeItem().Element(c => MetricCard(c, "Software", _data.SoftwareChanges.ToString(), ColorInfo));
                row.ConstantItem(6f);
                row.RelativeItem().Element(c => MetricCard(c, "Security", _data.SecurityChanges.ToString(), ColorDanger));
                row.ConstantItem(6f);
                row.RelativeItem().Element(c => MetricCard(c, "Network", _data.NetworkChanges.ToString(), ColorWarning));
                row.ConstantItem(6f);
                row.RelativeItem().Element(c => MetricCard(c, "USB", _data.UsbChanges.ToString(), ColorTextSecondary));
            });
        }

        private void ComposeChangeTable(IContainer container)
        {
            if (!_data.Changes.Any())
            {
                container.Padding(8f).Text(t => t.Span("No changes recorded for the selected period.").Italic().FontColor(ColorTextSecondary));
                return;
            }

            container.Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn(1.5f); // Detected At
                    columns.RelativeColumn(1f);    // Category
                    columns.RelativeColumn(0.8f);  // Type
                    columns.RelativeColumn(1.2f);  // Item
                    columns.RelativeColumn(1.5f);  // Old Value
                    columns.RelativeColumn(1.5f);  // New Value
                    columns.RelativeColumn(0.7f);  // Severity
                    columns.RelativeColumn(0.7f);  // Status
                });

                table.Header(header =>
                {
                    header.Cell().Element(TableHeaderCell).Text("Detected");
                    header.Cell().Element(TableHeaderCell).Text("Category");
                    header.Cell().Element(TableHeaderCell).Text("Type");
                    header.Cell().Element(TableHeaderCell).Text("Item");
                    header.Cell().Element(TableHeaderCell).Text("Old Value");
                    header.Cell().Element(TableHeaderCell).Text("New Value");
                    header.Cell().Element(TableHeaderCell).Text("Severity");
                    header.Cell().Element(TableHeaderCell).Text("Status");
                });

                int idx = 0;
                foreach (var change in _data.Changes)
                {
                    bool isEven = idx % 2 == 0;
                    table.Cell().Element(c => TableDataCell(c, isEven)).Text(t => t.Span(change.DetectedAt).FontSize(7f).FontColor(ColorTextSecondary));
                    table.Cell().Element(c => TableDataCell(c, isEven)).Text(t => t.Span(change.Category).FontSize(7.5f));
                    table.Cell().Element(c => TableDataCell(c, isEven)).Text(t => t.Span(change.ChangeType).FontSize(7.5f));
                    table.Cell().Element(c => TableDataCell(c, isEven)).Text(t => t.Span(change.ItemLabel).FontSize(7f));
                    table.Cell().Element(c => TableDataCell(c, isEven)).Text(t => t.Span(change.PreviousValue).FontSize(7f).FontColor(ColorDanger));
                    table.Cell().Element(c => TableDataCell(c, isEven)).Text(t => t.Span(change.NewValue).FontSize(7f).FontColor(ColorSuccess));
                    table.Cell().Element(c => TableDataCell(c, isEven)).Text(t => t.Span(change.Severity).FontSize(7f).FontColor(GetSeverityColor(change.Severity)));
                    table.Cell().Element(c => TableDataCell(c, isEven)).Text(t => t.Span(change.Status).FontSize(7f));
                    idx++;
                }
            });
        }
    }
}
