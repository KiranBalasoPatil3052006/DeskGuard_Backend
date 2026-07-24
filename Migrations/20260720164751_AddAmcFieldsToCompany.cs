using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DeskGuardBackend.Migrations
{
    /// <inheritdoc />
    public partial class AddAmcFieldsToCompany : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "amc_end_date",
                table: "companies",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "amc_plan",
                table: "companies",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "amc_start_date",
                table: "companies",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "roles",
                keyColumn: "id",
                keyValue: 1L,
                columns: new[] { "created_at", "updated_at" },
                values: new object[] { new DateTime(2026, 7, 20, 16, 47, 45, 40, DateTimeKind.Utc).AddTicks(1924), new DateTime(2026, 7, 20, 16, 47, 45, 40, DateTimeKind.Utc).AddTicks(1930) });

            migrationBuilder.UpdateData(
                table: "roles",
                keyColumn: "id",
                keyValue: 2L,
                columns: new[] { "created_at", "updated_at" },
                values: new object[] { new DateTime(2026, 7, 20, 16, 47, 45, 40, DateTimeKind.Utc).AddTicks(1939), new DateTime(2026, 7, 20, 16, 47, 45, 40, DateTimeKind.Utc).AddTicks(1939) });

            migrationBuilder.UpdateData(
                table: "roles",
                keyColumn: "id",
                keyValue: 3L,
                columns: new[] { "created_at", "updated_at" },
                values: new object[] { new DateTime(2026, 7, 20, 16, 47, 45, 40, DateTimeKind.Utc).AddTicks(1943), new DateTime(2026, 7, 20, 16, 47, 45, 40, DateTimeKind.Utc).AddTicks(1944) });

            migrationBuilder.UpdateData(
                table: "roles",
                keyColumn: "id",
                keyValue: 4L,
                columns: new[] { "created_at", "updated_at" },
                values: new object[] { new DateTime(2026, 7, 20, 16, 47, 45, 40, DateTimeKind.Utc).AddTicks(1955), new DateTime(2026, 7, 20, 16, 47, 45, 40, DateTimeKind.Utc).AddTicks(1955) });

            migrationBuilder.Sql("UPDATE companies SET amc_plan = 'Gold Premium Support', amc_start_date = '2026-01-01 00:00:00+00', amc_end_date = '2027-01-01 00:00:00+00' WHERE id = 1;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "amc_end_date",
                table: "companies");

            migrationBuilder.DropColumn(
                name: "amc_plan",
                table: "companies");

            migrationBuilder.DropColumn(
                name: "amc_start_date",
                table: "companies");

            migrationBuilder.UpdateData(
                table: "roles",
                keyColumn: "id",
                keyValue: 1L,
                columns: new[] { "created_at", "updated_at" },
                values: new object[] { new DateTime(2026, 7, 15, 8, 12, 15, 230, DateTimeKind.Utc).AddTicks(971), new DateTime(2026, 7, 15, 8, 12, 15, 230, DateTimeKind.Utc).AddTicks(977) });

            migrationBuilder.UpdateData(
                table: "roles",
                keyColumn: "id",
                keyValue: 2L,
                columns: new[] { "created_at", "updated_at" },
                values: new object[] { new DateTime(2026, 7, 15, 8, 12, 15, 230, DateTimeKind.Utc).AddTicks(1060), new DateTime(2026, 7, 15, 8, 12, 15, 230, DateTimeKind.Utc).AddTicks(1061) });

            migrationBuilder.UpdateData(
                table: "roles",
                keyColumn: "id",
                keyValue: 3L,
                columns: new[] { "created_at", "updated_at" },
                values: new object[] { new DateTime(2026, 7, 15, 8, 12, 15, 230, DateTimeKind.Utc).AddTicks(1066), new DateTime(2026, 7, 15, 8, 12, 15, 230, DateTimeKind.Utc).AddTicks(1067) });

            migrationBuilder.UpdateData(
                table: "roles",
                keyColumn: "id",
                keyValue: 4L,
                columns: new[] { "created_at", "updated_at" },
                values: new object[] { new DateTime(2026, 7, 15, 8, 12, 15, 230, DateTimeKind.Utc).AddTicks(1090), new DateTime(2026, 7, 15, 8, 12, 15, 230, DateTimeKind.Utc).AddTicks(1091) });
        }
    }
}
