using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DeskGuardBackend.Migrations
{
    /// <inheritdoc />
    public partial class AddBatteryColumnsToCurrentStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "battery_design_capacity",
                table: "machine_current_status",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "battery_full_charge_capacity",
                table: "machine_current_status",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "battery_is_present",
                table: "machine_current_status",
                type: "boolean",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "roles",
                keyColumn: "id",
                keyValue: 1L,
                columns: new[] { "created_at", "updated_at" },
                values: new object[] { new DateTime(2026, 7, 12, 15, 24, 12, 286, DateTimeKind.Utc).AddTicks(5718), new DateTime(2026, 7, 12, 15, 24, 12, 286, DateTimeKind.Utc).AddTicks(5723) });

            migrationBuilder.UpdateData(
                table: "roles",
                keyColumn: "id",
                keyValue: 2L,
                columns: new[] { "created_at", "updated_at" },
                values: new object[] { new DateTime(2026, 7, 12, 15, 24, 12, 286, DateTimeKind.Utc).AddTicks(5802), new DateTime(2026, 7, 12, 15, 24, 12, 286, DateTimeKind.Utc).AddTicks(5802) });

            migrationBuilder.UpdateData(
                table: "roles",
                keyColumn: "id",
                keyValue: 3L,
                columns: new[] { "created_at", "updated_at" },
                values: new object[] { new DateTime(2026, 7, 12, 15, 24, 12, 286, DateTimeKind.Utc).AddTicks(5875), new DateTime(2026, 7, 12, 15, 24, 12, 286, DateTimeKind.Utc).AddTicks(5875) });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "battery_design_capacity",
                table: "machine_current_status");

            migrationBuilder.DropColumn(
                name: "battery_full_charge_capacity",
                table: "machine_current_status");

            migrationBuilder.DropColumn(
                name: "battery_is_present",
                table: "machine_current_status");

            migrationBuilder.UpdateData(
                table: "roles",
                keyColumn: "id",
                keyValue: 1L,
                columns: new[] { "created_at", "updated_at" },
                values: new object[] { new DateTime(2026, 7, 12, 7, 19, 53, 717, DateTimeKind.Utc).AddTicks(8114), new DateTime(2026, 7, 12, 7, 19, 53, 717, DateTimeKind.Utc).AddTicks(8120) });

            migrationBuilder.UpdateData(
                table: "roles",
                keyColumn: "id",
                keyValue: 2L,
                columns: new[] { "created_at", "updated_at" },
                values: new object[] { new DateTime(2026, 7, 12, 7, 19, 53, 717, DateTimeKind.Utc).AddTicks(8182), new DateTime(2026, 7, 12, 7, 19, 53, 717, DateTimeKind.Utc).AddTicks(8183) });

            migrationBuilder.UpdateData(
                table: "roles",
                keyColumn: "id",
                keyValue: 3L,
                columns: new[] { "created_at", "updated_at" },
                values: new object[] { new DateTime(2026, 7, 12, 7, 19, 53, 717, DateTimeKind.Utc).AddTicks(8184), new DateTime(2026, 7, 12, 7, 19, 53, 717, DateTimeKind.Utc).AddTicks(8185) });
        }
    }
}
