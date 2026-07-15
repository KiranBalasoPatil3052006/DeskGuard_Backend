using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DeskGuardBackend.Migrations
{
    /// <inheritdoc />
    public partial class AddCompositePerformanceIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "roles",
                keyColumn: "id",
                keyValue: 1L,
                columns: new[] { "created_at", "updated_at" },
                values: new object[] { new DateTime(2026, 7, 12, 6, 57, 32, 56, DateTimeKind.Utc).AddTicks(434), new DateTime(2026, 7, 12, 6, 57, 32, 56, DateTimeKind.Utc).AddTicks(438) });

            migrationBuilder.UpdateData(
                table: "roles",
                keyColumn: "id",
                keyValue: 2L,
                columns: new[] { "created_at", "updated_at" },
                values: new object[] { new DateTime(2026, 7, 12, 6, 57, 32, 56, DateTimeKind.Utc).AddTicks(444), new DateTime(2026, 7, 12, 6, 57, 32, 56, DateTimeKind.Utc).AddTicks(444) });

            migrationBuilder.UpdateData(
                table: "roles",
                keyColumn: "id",
                keyValue: 3L,
                columns: new[] { "created_at", "updated_at" },
                values: new object[] { new DateTime(2026, 7, 12, 6, 57, 32, 56, DateTimeKind.Utc).AddTicks(446), new DateTime(2026, 7, 12, 6, 57, 32, 56, DateTimeKind.Utc).AddTicks(447) });

            migrationBuilder.CreateIndex(
                name: "ix_windows_services_machine_id_service_name",
                table: "windows_services",
                columns: new[] { "machine_id", "service_name" });

            migrationBuilder.CreateIndex(
                name: "ix_process_logs_machine_id_cpu_usage_percentage",
                table: "process_logs",
                columns: new[] { "machine_id", "cpu_usage_percentage" });

            migrationBuilder.CreateIndex(
                name: "ix_machine_network_adapters_machine_id_adapter_name",
                table: "machine_network_adapters",
                columns: new[] { "machine_id", "adapter_name" });

            migrationBuilder.CreateIndex(
                name: "ix_health_logs_company_id_collected_at",
                table: "health_logs",
                columns: new[] { "company_id", "collected_at" });

            migrationBuilder.CreateIndex(
                name: "ix_event_logs_machine_id_time_generated",
                table: "event_logs",
                columns: new[] { "machine_id", "time_generated" });

            migrationBuilder.CreateIndex(
                name: "ix_alerts_machine_id_created_at",
                table: "alerts",
                columns: new[] { "machine_id", "created_at" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_windows_services_machine_id_service_name",
                table: "windows_services");

            migrationBuilder.DropIndex(
                name: "ix_process_logs_machine_id_cpu_usage_percentage",
                table: "process_logs");

            migrationBuilder.DropIndex(
                name: "ix_machine_network_adapters_machine_id_adapter_name",
                table: "machine_network_adapters");

            migrationBuilder.DropIndex(
                name: "ix_health_logs_company_id_collected_at",
                table: "health_logs");

            migrationBuilder.DropIndex(
                name: "ix_event_logs_machine_id_time_generated",
                table: "event_logs");

            migrationBuilder.DropIndex(
                name: "ix_alerts_machine_id_created_at",
                table: "alerts");

            migrationBuilder.UpdateData(
                table: "roles",
                keyColumn: "id",
                keyValue: 1L,
                columns: new[] { "created_at", "updated_at" },
                values: new object[] { new DateTime(2026, 7, 12, 6, 15, 42, 689, DateTimeKind.Utc).AddTicks(1455), new DateTime(2026, 7, 12, 6, 15, 42, 689, DateTimeKind.Utc).AddTicks(1461) });

            migrationBuilder.UpdateData(
                table: "roles",
                keyColumn: "id",
                keyValue: 2L,
                columns: new[] { "created_at", "updated_at" },
                values: new object[] { new DateTime(2026, 7, 12, 6, 15, 42, 689, DateTimeKind.Utc).AddTicks(1472), new DateTime(2026, 7, 12, 6, 15, 42, 689, DateTimeKind.Utc).AddTicks(1472) });

            migrationBuilder.UpdateData(
                table: "roles",
                keyColumn: "id",
                keyValue: 3L,
                columns: new[] { "created_at", "updated_at" },
                values: new object[] { new DateTime(2026, 7, 12, 6, 15, 42, 689, DateTimeKind.Utc).AddTicks(1476), new DateTime(2026, 7, 12, 6, 15, 42, 689, DateTimeKind.Utc).AddTicks(1477) });
        }
    }
}
