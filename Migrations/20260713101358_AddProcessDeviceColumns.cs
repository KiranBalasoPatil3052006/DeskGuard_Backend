using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DeskGuardBackend.Migrations
{
    /// <inheritdoc />
    public partial class AddProcessDeviceColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Add new columns to process_logs
            migrationBuilder.AddColumn<int>(
                name: "thread_count",
                table: "process_logs",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "user_name",
                table: "process_logs",
                type: "text",
                nullable: true);

            // Add new columns to machine_connected_devices
            migrationBuilder.AddColumn<string>(
                name: "connection_type",
                table: "machine_connected_devices",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "last_seen",
                table: "machine_connected_devices",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "roles",
                keyColumn: "id",
                keyValue: 1L,
                columns: new[] { "created_at", "updated_at" },
                values: new object[] { new DateTime(2026, 7, 13, 10, 13, 53, 86, DateTimeKind.Utc).AddTicks(762), new DateTime(2026, 7, 13, 10, 13, 53, 86, DateTimeKind.Utc).AddTicks(768) });

            migrationBuilder.UpdateData(
                table: "roles",
                keyColumn: "id",
                keyValue: 2L,
                columns: new[] { "created_at", "updated_at" },
                values: new object[] { new DateTime(2026, 7, 13, 10, 13, 53, 86, DateTimeKind.Utc).AddTicks(829), new DateTime(2026, 7, 13, 10, 13, 53, 86, DateTimeKind.Utc).AddTicks(830) });

            migrationBuilder.UpdateData(
                table: "roles",
                keyColumn: "id",
                keyValue: 3L,
                columns: new[] { "created_at", "updated_at" },
                values: new object[] { new DateTime(2026, 7, 13, 10, 13, 53, 86, DateTimeKind.Utc).AddTicks(834), new DateTime(2026, 7, 13, 10, 13, 53, 86, DateTimeKind.Utc).AddTicks(835) });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "thread_count",
                table: "process_logs");

            migrationBuilder.DropColumn(
                name: "user_name",
                table: "process_logs");

            migrationBuilder.DropColumn(
                name: "connection_type",
                table: "machine_connected_devices");

            migrationBuilder.DropColumn(
                name: "last_seen",
                table: "machine_connected_devices");

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
    }
}
