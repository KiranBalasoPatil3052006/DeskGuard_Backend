using System.Linq;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;
using DeskGuardBackend.Reports.Models;

namespace DeskGuardBackend.Reports.PDF.Machine
{
    public class MachinePerformanceDocument : MachineReportBaseDocument
    {
        private readonly MachinePerformanceReportData _data;

        public MachinePerformanceDocument(MachinePerformanceReportData data) : base(data.Metadata)
        {
            _data = data;
        }

        protected override void ComposeReportContent(IContainer container)
        {
            container.Column(col =>
            {
                // Section 1: Current Status
                col.Item().Element(c => SectionTitle(c, "Current Resource Usage"));
                col.Item().PaddingBottom(12f).Element(ComposeCurrentStatus);

                // Section 2: Performance Summary (Avg / Peak)
                col.Item().Element(c => SectionTitle(c, "Performance Summary"));
                col.Item().PaddingBottom(12f).Element(ComposePerformanceSummary);

                // Section 3: Network Usage
                col.Item().Element(c => SectionTitle(c, "Network Usage"));
                col.Item().PaddingBottom(12f).Element(ComposeNetworkUsage);

                // Section 4: Performance Timeline
                col.Item().Element(c => SectionTitle(c, $"Performance Timeline ({_data.TotalDataPoints} data points)"));
                col.Item().Element(ComposeTimeline);
            });
        }

        private void ComposeCurrentStatus(IContainer container)
        {
            container.Row(row =>
            {
                row.RelativeItem().Element(c => MetricCard(c, "CPU Usage", _data.CurrentCpu, ColorAccent));
                row.ConstantItem(8f);
                row.RelativeItem().Element(c => MetricCard(c, "RAM Usage", _data.CurrentRam, ColorAccent));
                row.ConstantItem(8f);
                row.RelativeItem().Element(c => MetricCard(c, "Disk Usage", _data.CurrentDisk, ColorAccent));
                row.ConstantItem(8f);
                row.RelativeItem().Element(c => MetricCard(c, "CPU Temperature", _data.CurrentCpuTemp, ColorWarning));
            });
        }

        private void ComposePerformanceSummary(IContainer container)
        {
            container.Border(1f).BorderColor(ColorBorder).Padding(10f).Row(row =>
            {
                row.RelativeItem().Column(c =>
                {
                    c.Item().Text(t => t.Span("Averages").FontSize(9f).Bold().FontColor(ColorPrimary));
                    c.Item().Element(x => InfoRow(x, "Average CPU", _data.AvgCpu));
                    c.Item().Element(x => InfoRow(x, "Average RAM", _data.AvgRam));
                    c.Item().Element(x => InfoRow(x, "Average Disk", _data.AvgDisk));
                });
                row.ConstantItem(20f);
                row.RelativeItem().Column(c =>
                {
                    c.Item().Text(t => t.Span("Peaks").FontSize(9f).Bold().FontColor(ColorDanger));
                    c.Item().Element(x => InfoRow(x, "Peak CPU", _data.PeakCpu));
                    c.Item().Element(x => InfoRow(x, "Peak RAM", _data.PeakRam));
                    c.Item().Element(x => InfoRow(x, "Peak Disk", _data.PeakDisk));
                });
            });
        }

        private void ComposeNetworkUsage(IContainer container)
        {
            container.Row(row =>
            {
                row.RelativeItem().Element(c => MetricCard(c, "Total Downloaded", _data.TotalNetworkReceived, ColorInfo));
                row.ConstantItem(8f);
                row.RelativeItem().Element(c => MetricCard(c, "Total Uploaded", _data.TotalNetworkSent, ColorInfo));
            });
        }

        private void ComposeTimeline(IContainer container)
        {
            if (!_data.Timeline.Any())
            {
                container.Padding(8f).Text(t => t.Span("No performance data recorded for the selected period.").Italic().FontColor(ColorTextSecondary));
                return;
            }

            container.Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn(1.5f);
                    columns.RelativeColumn(1f);
                    columns.RelativeColumn(1f);
                    columns.RelativeColumn(1f);
                    columns.RelativeColumn(1f);
                    columns.RelativeColumn(1.2f);
                    columns.RelativeColumn(1.2f);
                });

                table.Header(header =>
                {
                    header.Cell().Element(TableHeaderCell).Text("Time");
                    header.Cell().Element(TableHeaderCell).Text("CPU %");
                    header.Cell().Element(TableHeaderCell).Text("RAM %");
                    header.Cell().Element(TableHeaderCell).Text("Disk %");
                    header.Cell().Element(TableHeaderCell).Text("CPU Temp");
                    header.Cell().Element(TableHeaderCell).Text("Net ↓");
                    header.Cell().Element(TableHeaderCell).Text("Net ↑");
                });

                int idx = 0;
                foreach (var row in _data.Timeline)
                {
                    bool isEven = idx % 2 == 0;
                    table.Cell().Element(c => TableDataCell(c, isEven)).Text(t => t.Span(row.CollectedAt).FontSize(7f).FontColor(ColorTextSecondary));
                    table.Cell().Element(c => TableDataCell(c, isEven)).Text(t => t.Span(row.CpuPercent).FontSize(7.5f));
                    table.Cell().Element(c => TableDataCell(c, isEven)).Text(t => t.Span(row.RamPercent).FontSize(7.5f));
                    table.Cell().Element(c => TableDataCell(c, isEven)).Text(t => t.Span(row.DiskPercent).FontSize(7.5f));
                    table.Cell().Element(c => TableDataCell(c, isEven)).Text(t => t.Span(row.CpuTemp).FontSize(7.5f));
                    table.Cell().Element(c => TableDataCell(c, isEven)).Text(t => t.Span(row.NetworkReceived).FontSize(7f));
                    table.Cell().Element(c => TableDataCell(c, isEven)).Text(t => t.Span(row.NetworkSent).FontSize(7f));
                    idx++;
                }
            });
        }
    }
}
