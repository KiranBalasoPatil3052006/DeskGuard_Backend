using System.Linq;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;
using DeskGuardBackend.Reports.Models;

namespace DeskGuardBackend.Reports.PDF.Machine
{
    public class MachineHealthDocument : MachineReportBaseDocument
    {
        private readonly MachineHealthReportData _data;

        public MachineHealthDocument(MachineHealthReportData data) : base(data.Metadata)
        {
            _data = data;
        }

        protected override void ComposeReportContent(IContainer container)
        {
            container.Column(col =>
            {
                // Section 1: Machine Information
                col.Item().PaddingBottom(8f).Column(c =>
                {
                    c.Item().Element(x => SectionTitle(x, "Machine Information"));
                    c.Item().Element(ComposeMachineInfo);
                });

                // Section 2: Health & Resource Status
                col.Item().PaddingBottom(8f).Column(c =>
                {
                    c.Item().Element(x => SectionTitle(x, "Health & Resource Status"));
                    c.Item().Element(ComposeHealthStatus);
                });

                // Section 3: Security Status
                col.Item().PaddingBottom(8f).Column(c =>
                {
                    c.Item().Element(x => SectionTitle(x, "Security Status"));
                    c.Item().Element(ComposeSecurityStatus);
                });

                // Section 4: Current Alerts
                col.Item().PaddingBottom(8f).Column(c =>
                {
                    c.Item().Element(x => SectionTitle(x, "Current Alerts"));
                    c.Item().Element(ComposeAlerts);
                });

                // Section 5: Recent Changes
                col.Item().Column(c =>
                {
                    c.Item().Element(x => SectionTitle(x, "Recent Changes"));
                    c.Item().Element(ComposeChanges);
                });
            });
        }

        private void ComposeMachineInfo(IContainer container)
        {
            container.Border(1f).BorderColor(ColorBorder).Padding(6f).Column(col =>
            {
                col.Item().Row(row =>
                {
                    row.RelativeItem().Column(c =>
                    {
                        c.Item().Element(x => InfoRow(x, "Device Name", _data.DeviceName));
                        c.Item().Element(x => InfoRow(x, "Hostname", _data.Hostname));
                        c.Item().Element(x => InfoRow(x, "Operating System", _data.Metadata.OperatingSystem));
                        c.Item().Element(x => InfoRow(x, "OS Version", _data.OsVersion));
                        c.Item().Element(x => InfoRow(x, "Manufacturer", _data.Manufacturer));
                        c.Item().Element(x => InfoRow(x, "Model", _data.Model));
                    });
                    row.ConstantItem(15f);
                    row.RelativeItem().Column(c =>
                    {
                        c.Item().Element(x => InfoRow(x, "Serial Number", _data.SerialNumber));
                        c.Item().Element(x => InfoRow(x, "Processor", _data.Processor));
                        c.Item().Element(x => InfoRow(x, "RAM", _data.RamGb));
                        c.Item().Element(x => InfoRow(x, "Registration Date", _data.RegistrationDate));
                        c.Item().Element(x => InfoRow(x, "Last Heartbeat", _data.LastHeartbeat));
                        c.Item().Element(x => InfoRow(x, "Status", _data.IsOnline ? "Online" : "Offline"));
                    });
                });
            });
        }

        private void ComposeHealthStatus(IContainer container)
        {
            container.Column(col =>
            {
                col.Item().Row(row =>
                {
                    row.RelativeItem().Element(c => MetricCard(c, "Health Score", $"{_data.HealthScore:F0}%",
                        _data.HealthScore >= 90 ? ColorSuccess : _data.HealthScore >= 70 ? ColorAccent : _data.HealthScore >= 50 ? ColorWarning : ColorDanger));
                    row.ConstantItem(6f);
                    row.RelativeItem().Element(c => MetricCard(c, "CPU Usage", _data.CpuStatus, ColorPrimary));
                    row.ConstantItem(6f);
                    row.RelativeItem().Element(c => MetricCard(c, "RAM Usage", _data.RamStatus, ColorPrimary));
                    row.ConstantItem(6f);
                    row.RelativeItem().Element(c => MetricCard(c, "Disk Usage", _data.DiskStatus, ColorPrimary));
                    row.ConstantItem(6f);
                    row.RelativeItem().Element(c => MetricCard(c, "Network", _data.NetworkStatus, ColorPrimary));
                });
            });
        }

        private void ComposeSecurityStatus(IContainer container)
        {
            container.Border(1f).BorderColor(ColorBorder).Padding(6f).Column(col =>
            {
                col.Item().Row(row =>
                {
                    row.RelativeItem().Column(c =>
                    {
                        c.Item().Element(x => InfoRow(x, "Antivirus Engine", _data.AntivirusName));
                        c.Item().Element(x => InfoRow(x, "Real-time Protection", _data.AntivirusEnabled ? "Enabled" : "Disabled"));
                    });
                    row.ConstantItem(15f);
                    row.RelativeItem().Column(c =>
                    {
                        c.Item().Element(x => InfoRow(x, "Firewall Status", _data.FirewallEnabled ? "Active" : "Disabled"));
                        c.Item().Element(x => InfoRow(x, "Security Rating", _data.SecurityScore));
                    });
                });
            });
        }

        private void ComposeAlerts(IContainer container)
        {
            if (!_data.RecentAlerts.Any())
            {
                container.Padding(4f).Text(t => t.Span("No open or recent alerts recorded for this machine.").Italic().FontColor(ColorTextSecondary));
                return;
            }

            container.Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn(3f);
                    columns.RelativeColumn(1f);
                    columns.RelativeColumn(1f);
                    columns.RelativeColumn(1.5f);
                });

                table.Header(header =>
                {
                    header.Cell().Element(TableHeaderCell).Text("Title");
                    header.Cell().Element(TableHeaderCell).Text("Severity");
                    header.Cell().Element(TableHeaderCell).Text("Status");
                    header.Cell().Element(TableHeaderCell).Text("Created");
                });

                int idx = 0;
                foreach (var alert in _data.RecentAlerts.Take(10))
                {
                    bool isEven = idx % 2 == 0;
                    table.Cell().Element(c => TableDataCell(c, isEven)).Text(t => t.Span(alert.Title).FontSize(7.5f));
                    table.Cell().Element(c => TableDataCell(c, isEven)).Text(t => t.Span(alert.Severity).FontSize(7.5f).FontColor(GetSeverityColor(alert.Severity)));
                    table.Cell().Element(c => TableDataCell(c, isEven)).Text(t => t.Span(alert.Status).FontSize(7.5f).FontColor(GetStatusColor(alert.Status)));
                    table.Cell().Element(c => TableDataCell(c, isEven)).Text(t => t.Span(alert.CreatedAt).FontSize(7f).FontColor(ColorTextSecondary));
                    idx++;
                }
            });
        }

        private void ComposeChanges(IContainer container)
        {
            if (!_data.RecentChanges.Any())
            {
                container.Padding(4f).Text(t => t.Span("No recent hardware or software changes recorded.").Italic().FontColor(ColorTextSecondary));
                return;
            }

            container.Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn(1.2f);
                    columns.RelativeColumn(1f);
                    columns.RelativeColumn(3f);
                    columns.RelativeColumn(1.5f);
                });

                table.Header(header =>
                {
                    header.Cell().Element(TableHeaderCell).Text("Category");
                    header.Cell().Element(TableHeaderCell).Text("Type");
                    header.Cell().Element(TableHeaderCell).Text("Description");
                    header.Cell().Element(TableHeaderCell).Text("Detected");
                });

                int idx = 0;
                foreach (var change in _data.RecentChanges.Take(10))
                {
                    bool isEven = idx % 2 == 0;
                    table.Cell().Element(c => TableDataCell(c, isEven)).Text(t => t.Span(change.Category).FontSize(7.5f));
                    table.Cell().Element(c => TableDataCell(c, isEven)).Text(t => t.Span(change.ChangeType).FontSize(7.5f));
                    table.Cell().Element(c => TableDataCell(c, isEven)).Text(t => t.Span(change.Description).FontSize(7f));
                    table.Cell().Element(c => TableDataCell(c, isEven)).Text(t => t.Span(change.DetectedAt).FontSize(7f).FontColor(ColorTextSecondary));
                    idx++;
                }
            });
        }
    }
}
