using System.Linq;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;
using DeskGuardBackend.Reports.Models;

namespace DeskGuardBackend.Reports.PDF.Machine
{
    public class MachineHardwareDocument : MachineReportBaseDocument
    {
        private readonly MachineHardwareReportData _data;

        public MachineHardwareDocument(MachineHardwareReportData data) : base(data.Metadata)
        {
            _data = data;
        }

        protected override void ComposeReportContent(IContainer container)
        {
            container.Column(col =>
            {
                // Section 1: Processor & Motherboard
                col.Item().PaddingBottom(8f).Column(c =>
                {
                    c.Item().Element(x => SectionTitle(x, "Processor & Motherboard"));
                    c.Item().Element(ComposeCpuAndMotherboard);
                });

                // Section 2: Memory & GPU
                col.Item().PaddingBottom(8f).Column(c =>
                {
                    c.Item().Element(x => SectionTitle(x, "Memory (RAM) & Graphics (GPU)"));
                    c.Item().Element(ComposeMemoryAndGpu);
                });

                // Section 3: Battery & Power
                col.Item().PaddingBottom(8f).Column(c =>
                {
                    c.Item().Element(x => SectionTitle(x, "Battery & Power Status"));
                    c.Item().Element(ComposeBattery);
                });

                // Section 4: Storage Drives
                col.Item().PaddingBottom(8f).Column(c =>
                {
                    c.Item().Element(x => SectionTitle(x, "Storage Drives"));
                    c.Item().Element(ComposeStorage);
                });

                // Section 5: Network Adapters
                col.Item().PaddingBottom(8f).Column(c =>
                {
                    c.Item().Element(x => SectionTitle(x, "Network Adapters"));
                    c.Item().Element(ComposeNetworkAdapters);
                });

                // Section 6: Connected Devices & Peripherals
                col.Item().Column(c =>
                {
                    c.Item().Element(x => SectionTitle(x, "Connected Devices & Peripherals"));
                    c.Item().Element(ComposeConnectedDevices);
                });
            });
        }

        private void ComposeCpuAndMotherboard(IContainer container)
        {
            container.Border(1f).BorderColor(ColorBorder).Padding(6f).Row(row =>
            {
                row.RelativeItem().Column(c =>
                {
                    c.Item().Text(t => t.Span("CPU / Processor").FontSize(8.5f).Bold().FontColor(ColorPrimary));
                    c.Item().Element(x => InfoRow(x, "Model", _data.CpuModel));
                    c.Item().Element(x => InfoRow(x, "Cores / Threads", $"{_data.CpuCores} / {_data.CpuThreads}"));
                    c.Item().Element(x => InfoRow(x, "Max Clock Speed", _data.CpuMaxClockSpeed));
                    c.Item().Element(x => InfoRow(x, "Architecture", _data.CpuArchitecture));
                });
                row.ConstantItem(15f);
                row.RelativeItem().Column(c =>
                {
                    c.Item().Text(t => t.Span("Motherboard & System").FontSize(8.5f).Bold().FontColor(ColorPrimary));
                    c.Item().Element(x => InfoRow(x, "Manufacturer", _data.MotherboardManufacturer));
                    c.Item().Element(x => InfoRow(x, "Model", _data.MotherboardModel));
                    c.Item().Element(x => InfoRow(x, "BIOS Version", _data.BiosVersion));
                    c.Item().Element(x => InfoRow(x, "Serial Number", _data.MachineSerialNumber));
                });
            });
        }

        private void ComposeMemoryAndGpu(IContainer container)
        {
            container.Border(1f).BorderColor(ColorBorder).Padding(6f).Row(row =>
            {
                row.RelativeItem().Column(c =>
                {
                    c.Item().Text(t => t.Span("Memory (RAM)").FontSize(8.5f).Bold().FontColor(ColorPrimary));
                    c.Item().Element(x => InfoRow(x, "Total Capacity", _data.RamTotal));
                    c.Item().Element(x => InfoRow(x, "Type / Speed", $"{_data.RamType} ({_data.RamSpeed})"));
                    c.Item().Element(x => InfoRow(x, "Memory Slots", _data.RamSlots));
                });
                row.ConstantItem(15f);
                row.RelativeItem().Column(c =>
                {
                    c.Item().Text(t => t.Span("Graphics (GPU)").FontSize(8.5f).Bold().FontColor(ColorPrimary));
                    c.Item().Element(x => InfoRow(x, "GPU Model", _data.GpuName));
                    c.Item().Element(x => InfoRow(x, "Driver Version", _data.GpuDriverVersion));
                    c.Item().Element(x => InfoRow(x, "VRAM Capacity", _data.GpuMemory));
                });
            });
        }

        private void ComposeBattery(IContainer container)
        {
            container.Border(1f).BorderColor(ColorBorder).Padding(6f).Row(row =>
            {
                row.RelativeItem().Column(c =>
                {
                    c.Item().Element(x => InfoRow(x, "Battery Status", _data.BatteryPresent ? "Battery Installed" : "AC Power / No Battery"));
                    c.Item().Element(x => InfoRow(x, "Charge Level", _data.BatteryPercentage));
                    c.Item().Element(x => InfoRow(x, "Power State", _data.BatteryChargingStatus));
                });
                row.ConstantItem(15f);
                row.RelativeItem().Column(c =>
                {
                    c.Item().Element(x => InfoRow(x, "Wear Level", _data.BatteryWearLevel));
                    c.Item().Element(x => InfoRow(x, "Design Capacity", _data.BatteryDesignCapacity));
                    c.Item().Element(x => InfoRow(x, "Full Charge Capacity", _data.BatteryFullChargeCapacity));
                });
            });
        }

        private void ComposeStorage(IContainer container)
        {
            if (!_data.Disks.Any())
            {
                container.Padding(4f).Text(t => t.Span("No storage disk details available.").Italic().FontColor(ColorTextSecondary));
                return;
            }

            container.Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn(0.8f);
                    columns.RelativeColumn(1.2f);
                    columns.RelativeColumn(0.8f);
                    columns.RelativeColumn(0.8f);
                    columns.RelativeColumn(1f);
                    columns.RelativeColumn(1f);
                    columns.RelativeColumn(1f);
                    columns.RelativeColumn(0.8f);
                });

                table.Header(header =>
                {
                    header.Cell().Element(TableHeaderCell).Text("Drive");
                    header.Cell().Element(TableHeaderCell).Text("Label");
                    header.Cell().Element(TableHeaderCell).Text("Type");
                    header.Cell().Element(TableHeaderCell).Text("FS");
                    header.Cell().Element(TableHeaderCell).Text("Total");
                    header.Cell().Element(TableHeaderCell).Text("Used");
                    header.Cell().Element(TableHeaderCell).Text("Free");
                    header.Cell().Element(TableHeaderCell).Text("Health");
                });

                int idx = 0;
                foreach (var disk in _data.Disks)
                {
                    bool isEven = idx % 2 == 0;
                    table.Cell().Element(c => TableDataCell(c, isEven)).Text(t => t.Span(disk.DriveLetter).FontSize(7.5f).Bold());
                    table.Cell().Element(c => TableDataCell(c, isEven)).Text(t => t.Span(disk.VolumeLabel).FontSize(7.5f));
                    table.Cell().Element(c => TableDataCell(c, isEven)).Text(t => t.Span(disk.DriveType).FontSize(7.5f));
                    table.Cell().Element(c => TableDataCell(c, isEven)).Text(t => t.Span(disk.FileSystem).FontSize(7.5f));
                    table.Cell().Element(c => TableDataCell(c, isEven)).Text(t => t.Span(disk.TotalGb).FontSize(7.5f));
                    table.Cell().Element(c => TableDataCell(c, isEven)).Text(t => t.Span(disk.UsedGb).FontSize(7.5f));
                    table.Cell().Element(c => TableDataCell(c, isEven)).Text(t => t.Span(disk.FreeGb).FontSize(7.5f));
                    table.Cell().Element(c => TableDataCell(c, isEven)).Text(t => t.Span(disk.HealthStatus).FontSize(7.5f).FontColor(GetStatusColor(disk.HealthStatus)));
                    idx++;
                }
            });
        }

        private void ComposeNetworkAdapters(IContainer container)
        {
            if (!_data.NetworkAdapters.Any())
            {
                container.Padding(4f).Text(t => t.Span("No network adapter details available.").Italic().FontColor(ColorTextSecondary));
                return;
            }

            container.Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn(2f);
                    columns.RelativeColumn(1.4f);
                    columns.RelativeColumn(1.4f);
                    columns.RelativeColumn(1f);
                    columns.RelativeColumn(1f);
                    columns.RelativeColumn(0.8f);
                });

                table.Header(header =>
                {
                    header.Cell().Element(TableHeaderCell).Text("Adapter");
                    header.Cell().Element(TableHeaderCell).Text("IP Address");
                    header.Cell().Element(TableHeaderCell).Text("MAC Address");
                    header.Cell().Element(TableHeaderCell).Text("Type");
                    header.Cell().Element(TableHeaderCell).Text("Speed");
                    header.Cell().Element(TableHeaderCell).Text("Status");
                });

                int idx = 0;
                foreach (var adapter in _data.NetworkAdapters.Take(12))
                {
                    bool isEven = idx % 2 == 0;
                    table.Cell().Element(c => TableDataCell(c, isEven)).Text(t => t.Span(adapter.AdapterName).FontSize(7.5f));
                    table.Cell().Element(c => TableDataCell(c, isEven)).Text(t => t.Span(adapter.IpAddress).FontSize(7.5f));
                    table.Cell().Element(c => TableDataCell(c, isEven)).Text(t => t.Span(adapter.MacAddress).FontSize(7f).FontColor(ColorTextSecondary));
                    table.Cell().Element(c => TableDataCell(c, isEven)).Text(t => t.Span(adapter.AdapterType).FontSize(7.5f));
                    table.Cell().Element(c => TableDataCell(c, isEven)).Text(t => t.Span(adapter.Speed).FontSize(7.5f));
                    table.Cell().Element(c => TableDataCell(c, isEven)).Text(t => t.Span(adapter.Status).FontSize(7.5f).FontColor(GetStatusColor(adapter.Status)));
                    idx++;
                }
            });
        }

        private void ComposeConnectedDevices(IContainer container)
        {
            if (!_data.ConnectedDevices.Any())
            {
                container.Padding(4f).Text(t => t.Span("No connected peripheral devices recorded.").Italic().FontColor(ColorTextSecondary));
                return;
            }

            container.Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn(2f);
                    columns.RelativeColumn(1f);
                    columns.RelativeColumn(1f);
                    columns.RelativeColumn(1.2f);
                    columns.RelativeColumn(1f);
                    columns.RelativeColumn(0.8f);
                });

                table.Header(header =>
                {
                    header.Cell().Element(TableHeaderCell).Text("Device");
                    header.Cell().Element(TableHeaderCell).Text("Type");
                    header.Cell().Element(TableHeaderCell).Text("Connection");
                    header.Cell().Element(TableHeaderCell).Text("Manufacturer");
                    header.Cell().Element(TableHeaderCell).Text("Driver");
                    header.Cell().Element(TableHeaderCell).Text("Status");
                });

                int idx = 0;
                foreach (var device in _data.ConnectedDevices.Take(25))
                {
                    bool isEven = idx % 2 == 0;
                    table.Cell().Element(c => TableDataCell(c, isEven)).Text(t => t.Span(device.DeviceName).FontSize(7.5f));
                    table.Cell().Element(c => TableDataCell(c, isEven)).Text(t => t.Span(device.DeviceType).FontSize(7.5f));
                    table.Cell().Element(c => TableDataCell(c, isEven)).Text(t => t.Span(device.ConnectionType).FontSize(7.5f));
                    table.Cell().Element(c => TableDataCell(c, isEven)).Text(t => t.Span(device.Manufacturer).FontSize(7.5f));
                    table.Cell().Element(c => TableDataCell(c, isEven)).Text(t => t.Span(device.DriverVersion).FontSize(7f).FontColor(ColorTextSecondary));
                    table.Cell().Element(c => TableDataCell(c, isEven)).Text(t => t.Span(device.Status).FontSize(7.5f).FontColor(GetStatusColor(device.Status)));
                    idx++;
                }
            });
        }
    }
}
