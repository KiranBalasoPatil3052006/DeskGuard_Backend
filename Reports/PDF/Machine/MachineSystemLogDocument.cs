using System.Linq;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;
using DeskGuardBackend.Reports.Models;

namespace DeskGuardBackend.Reports.PDF.Machine
{
    public class MachineSystemLogDocument : MachineReportBaseDocument
    {
        private readonly MachineSystemLogReportData _data;

        public MachineSystemLogDocument(MachineSystemLogReportData data) : base(data.Metadata)
        {
            _data = data;
        }

        protected override void ComposeReportContent(IContainer container)
        {
            container.Column(col =>
            {
                // Event Log Summary
                col.Item().Element(c => SectionTitle(c, "Event Log Summary"));
                col.Item().PaddingBottom(12f).Element(ComposeEventSummary);

                // Services Summary
                col.Item().Element(c => SectionTitle(c, "Windows Services Summary"));
                col.Item().PaddingBottom(12f).Element(ComposeServicesSummary);

                // Event Logs Table
                col.Item().Element(c => SectionTitle(c, $"Event Logs ({_data.TotalEvents} records)"));
                col.Item().PaddingBottom(12f).Element(ComposeEventLogTable);

                // Services Table
                col.Item().Element(c => SectionTitle(c, $"Services ({_data.TotalServices} total)"));
                col.Item().PaddingBottom(12f).Element(ComposeServicesTable);

                // Startup Programs
                col.Item().Element(c => SectionTitle(c, $"Startup Programs ({_data.TotalStartupPrograms} total)"));
                col.Item().Element(ComposeStartupTable);
            });
        }

        private void ComposeEventSummary(IContainer container)
        {
            container.Row(row =>
            {
                row.RelativeItem().Element(c => MetricCard(c, "Total Events", _data.TotalEvents.ToString(), ColorPrimary));
                row.ConstantItem(6f);
                row.RelativeItem().Element(c => MetricCard(c, "Critical", _data.CriticalEvents.ToString(), ColorDanger));
                row.ConstantItem(6f);
                row.RelativeItem().Element(c => MetricCard(c, "Errors", _data.ErrorEvents.ToString(), ColorDanger));
                row.ConstantItem(6f);
                row.RelativeItem().Element(c => MetricCard(c, "Warnings", _data.WarningEvents.ToString(), ColorWarning));
                row.ConstantItem(6f);
                row.RelativeItem().Element(c => MetricCard(c, "Information", _data.InformationEvents.ToString(), ColorInfo));
            });
        }

        private void ComposeServicesSummary(IContainer container)
        {
            container.Row(row =>
            {
                row.RelativeItem().Element(c => MetricCard(c, "Total Services", _data.TotalServices.ToString(), ColorPrimary));
                row.ConstantItem(8f);
                row.RelativeItem().Element(c => MetricCard(c, "Running", _data.RunningServices.ToString(), ColorSuccess));
                row.ConstantItem(8f);
                row.RelativeItem().Element(c => MetricCard(c, "Stopped", _data.StoppedServices.ToString(), ColorDanger));
                row.ConstantItem(8f);
                row.RelativeItem().Element(c => MetricCard(c, "Startup Programs", _data.TotalStartupPrograms.ToString(), ColorAccent));
            });
        }

        private void ComposeEventLogTable(IContainer container)
        {
            if (!_data.EventLogs.Any())
            {
                container.Padding(8f).Text(t => t.Span("No event logs available.").Italic().FontColor(ColorTextSecondary));
                return;
            }

            container.Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn(1.3f);  // Time
                    columns.RelativeColumn(0.8f);   // Level
                    columns.RelativeColumn(1f);     // Log Name
                    columns.RelativeColumn(1.2f);  // Source
                    columns.RelativeColumn(0.6f);  // Event ID
                    columns.RelativeColumn(3f);     // Message
                });

                table.Header(header =>
                {
                    header.Cell().Element(TableHeaderCell).Text("Time");
                    header.Cell().Element(TableHeaderCell).Text("Level");
                    header.Cell().Element(TableHeaderCell).Text("Log");
                    header.Cell().Element(TableHeaderCell).Text("Source");
                    header.Cell().Element(TableHeaderCell).Text("ID");
                    header.Cell().Element(TableHeaderCell).Text("Message");
                });

                int idx = 0;
                foreach (var log in _data.EventLogs)
                {
                    bool isEven = idx % 2 == 0;
                    table.Cell().Element(c => TableDataCell(c, isEven)).Text(t => t.Span(log.TimeGenerated).FontSize(7f).FontColor(ColorTextSecondary));
                    table.Cell().Element(c => TableDataCell(c, isEven)).Text(t => t.Span(log.Level).FontSize(7.5f).FontColor(GetSeverityColor(log.Level)));
                    table.Cell().Element(c => TableDataCell(c, isEven)).Text(t => t.Span(log.LogName).FontSize(7f));
                    table.Cell().Element(c => TableDataCell(c, isEven)).Text(t => t.Span(log.Source).FontSize(7f));
                    table.Cell().Element(c => TableDataCell(c, isEven)).Text(t => t.Span(log.EventId).FontSize(7f));
                    table.Cell().Element(c => TableDataCell(c, isEven)).Text(t =>
                    {
                        var msg = log.Message.Length > 120 ? log.Message.Substring(0, 120) + "…" : log.Message;
                        t.Span(msg).FontSize(6.5f);
                    });
                    idx++;
                }
            });
        }

        private void ComposeServicesTable(IContainer container)
        {
            if (!_data.Services.Any())
            {
                container.Padding(8f).Text(t => t.Span("No service information available.").Italic().FontColor(ColorTextSecondary));
                return;
            }

            container.Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn(2f);    // Service Name
                    columns.RelativeColumn(2.5f);  // Display Name
                    columns.RelativeColumn(1f);     // Status
                    columns.RelativeColumn(1f);     // Start Type
                });

                table.Header(header =>
                {
                    header.Cell().Element(TableHeaderCell).Text("Service Name");
                    header.Cell().Element(TableHeaderCell).Text("Display Name");
                    header.Cell().Element(TableHeaderCell).Text("Status");
                    header.Cell().Element(TableHeaderCell).Text("Start Type");
                });

                int idx = 0;
                foreach (var svc in _data.Services)
                {
                    bool isEven = idx % 2 == 0;
                    table.Cell().Element(c => TableDataCell(c, isEven)).Text(t => t.Span(svc.ServiceName).FontSize(7.5f));
                    table.Cell().Element(c => TableDataCell(c, isEven)).Text(t => t.Span(svc.DisplayName).FontSize(7f));
                    table.Cell().Element(c => TableDataCell(c, isEven)).Text(t => t.Span(svc.Status).FontSize(7.5f).FontColor(GetStatusColor(svc.Status)));
                    table.Cell().Element(c => TableDataCell(c, isEven)).Text(t => t.Span(svc.StartType).FontSize(7.5f));
                    idx++;
                }
            });
        }

        private void ComposeStartupTable(IContainer container)
        {
            if (!_data.StartupPrograms.Any())
            {
                container.Padding(8f).Text(t => t.Span("No startup program information available.").Italic().FontColor(ColorTextSecondary));
                return;
            }

            container.Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn(2f);    // Name
                    columns.RelativeColumn(3f);    // Command
                    columns.RelativeColumn(1.5f);  // Location
                    columns.RelativeColumn(0.8f);  // Status
                });

                table.Header(header =>
                {
                    header.Cell().Element(TableHeaderCell).Text("Name");
                    header.Cell().Element(TableHeaderCell).Text("Command");
                    header.Cell().Element(TableHeaderCell).Text("Location");
                    header.Cell().Element(TableHeaderCell).Text("Status");
                });

                int idx = 0;
                foreach (var prog in _data.StartupPrograms)
                {
                    bool isEven = idx % 2 == 0;
                    table.Cell().Element(c => TableDataCell(c, isEven)).Text(t => t.Span(prog.Name).FontSize(7.5f));
                    table.Cell().Element(c => TableDataCell(c, isEven)).Text(t => t.Span(prog.Command).FontSize(6.5f).FontColor(ColorTextSecondary));
                    table.Cell().Element(c => TableDataCell(c, isEven)).Text(t => t.Span(prog.Location).FontSize(7f));
                    table.Cell().Element(c => TableDataCell(c, isEven)).Text(t => t.Span(prog.Status).FontSize(7.5f));
                    idx++;
                }
            });
        }
    }
}
