using System.Linq;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;
using DeskGuardBackend.Reports.Models;

namespace DeskGuardBackend.Reports.PDF.Machine
{
    public class MachineActivityDocument : MachineReportBaseDocument
    {
        private readonly MachineActivityReportData _data;

        public MachineActivityDocument(MachineActivityReportData data) : base(data.Metadata)
        {
            _data = data;
        }

        protected override void ComposeReportContent(IContainer container)
        {
            container.Column(col =>
            {
                // Summary
                col.Item().Element(c => SectionTitle(c, "Activity Summary"));
                col.Item().PaddingBottom(12f).Element(ComposeSummary);

                // Login Activities
                col.Item().Element(c => SectionTitle(c, $"Login Activity ({_data.TotalLogins} events)"));
                col.Item().PaddingBottom(12f).Element(ComposeLoginTable);

                // USB Activities
                col.Item().Element(c => SectionTitle(c, $"USB Activity ({_data.TotalUsbEvents} events)"));
                col.Item().Element(ComposeUsbTable);
            });
        }

        private void ComposeSummary(IContainer container)
        {
            container.Row(row =>
            {
                row.RelativeItem().Element(c => MetricCard(c, "Total Logins", _data.TotalLogins.ToString(), ColorPrimary));
                row.ConstantItem(8f);
                row.RelativeItem().Element(c => MetricCard(c, "Successful", _data.SuccessfulLogins.ToString(), ColorSuccess));
                row.ConstantItem(8f);
                row.RelativeItem().Element(c => MetricCard(c, "Failed", _data.FailedLogins.ToString(), ColorDanger));
                row.ConstantItem(8f);
                row.RelativeItem().Element(c => MetricCard(c, "USB Events", _data.TotalUsbEvents.ToString(), ColorWarning));
            });
        }

        private void ComposeLoginTable(IContainer container)
        {
            if (!_data.LoginActivities.Any())
            {
                container.Padding(8f).Text(t => t.Span("No login activity recorded for the selected period.").Italic().FontColor(ColorTextSecondary));
                return;
            }

            container.Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn(1.5f);  // Time
                    columns.RelativeColumn(1.5f);  // Username
                    columns.RelativeColumn(1f);     // Event Type
                    columns.RelativeColumn(1f);     // Logon Type
                    columns.RelativeColumn(1.2f);  // Source IP
                    columns.RelativeColumn(0.8f);  // Result
                });

                table.Header(header =>
                {
                    header.Cell().Element(TableHeaderCell).Text("Time");
                    header.Cell().Element(TableHeaderCell).Text("Username");
                    header.Cell().Element(TableHeaderCell).Text("Event");
                    header.Cell().Element(TableHeaderCell).Text("Logon Type");
                    header.Cell().Element(TableHeaderCell).Text("Source IP");
                    header.Cell().Element(TableHeaderCell).Text("Result");
                });

                int idx = 0;
                foreach (var login in _data.LoginActivities)
                {
                    bool isEven = idx % 2 == 0;
                    table.Cell().Element(c => TableDataCell(c, isEven)).Text(t => t.Span(login.EventTime).FontSize(7f).FontColor(ColorTextSecondary));
                    table.Cell().Element(c => TableDataCell(c, isEven)).Text(t => t.Span(login.Username).FontSize(7.5f));
                    table.Cell().Element(c => TableDataCell(c, isEven)).Text(t => t.Span(login.EventType).FontSize(7.5f));
                    table.Cell().Element(c => TableDataCell(c, isEven)).Text(t => t.Span(login.LogonType).FontSize(7.5f));
                    table.Cell().Element(c => TableDataCell(c, isEven)).Text(t => t.Span(login.SourceIp).FontSize(7.5f));
                    table.Cell().Element(c => TableDataCell(c, isEven)).Text(t => t.Span(login.IsSuccess).FontSize(7.5f)
                        .FontColor(login.IsSuccess == "Success" ? ColorSuccess : ColorDanger));
                    idx++;
                }
            });
        }

        private void ComposeUsbTable(IContainer container)
        {
            if (!_data.UsbActivities.Any())
            {
                container.Padding(8f).Text(t => t.Span("No USB activity recorded for the selected period.").Italic().FontColor(ColorTextSecondary));
                return;
            }

            container.Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn(1.5f);  // Time
                    columns.RelativeColumn(2f);     // Device
                    columns.RelativeColumn(1.5f);  // Serial
                    columns.RelativeColumn(1f);     // Event
                });

                table.Header(header =>
                {
                    header.Cell().Element(TableHeaderCell).Text("Time");
                    header.Cell().Element(TableHeaderCell).Text("Device");
                    header.Cell().Element(TableHeaderCell).Text("Serial");
                    header.Cell().Element(TableHeaderCell).Text("Event");
                });

                int idx = 0;
                foreach (var usb in _data.UsbActivities)
                {
                    bool isEven = idx % 2 == 0;
                    table.Cell().Element(c => TableDataCell(c, isEven)).Text(t => t.Span(usb.EventTime).FontSize(7f).FontColor(ColorTextSecondary));
                    table.Cell().Element(c => TableDataCell(c, isEven)).Text(t => t.Span(usb.DeviceName).FontSize(7.5f));
                    table.Cell().Element(c => TableDataCell(c, isEven)).Text(t => t.Span(usb.DeviceSerial).FontSize(7f).FontColor(ColorTextSecondary));
                    table.Cell().Element(c => TableDataCell(c, isEven)).Text(t => t.Span(usb.EventType).FontSize(7.5f));
                    idx++;
                }
            });
        }
    }
}
