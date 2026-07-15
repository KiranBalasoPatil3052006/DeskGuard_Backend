using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace DeskGuardBackend.Migrations
{
    /// <inheritdoc />
    public partial class AddAlertProfiles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "custom_alert_profile_id",
                table: "machines",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "alert_profile_id",
                table: "companies",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "alert_profiles",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    is_default = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_alert_profiles", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "alert_thresholds",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    profile_id = table.Column<long>(type: "bigint", nullable: false),
                    cpu_warning_percent = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: true),
                    cpu_critical_percent = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: true),
                    cpu_warning_duration_minutes = table.Column<int>(type: "integer", nullable: true),
                    ram_warning_percent = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: true),
                    ram_critical_percent = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: true),
                    ram_warning_duration_minutes = table.Column<int>(type: "integer", nullable: true),
                    cpu_temp_warning = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: true),
                    cpu_temp_critical = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: true),
                    disk_warning_percent = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: true),
                    disk_critical_percent = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: true),
                    disk_smart_warning_enabled = table.Column<bool>(type: "boolean", nullable: true),
                    disk_smart_critical_enabled = table.Column<bool>(type: "boolean", nullable: true),
                    offline_warning_minutes = table.Column<int>(type: "integer", nullable: true),
                    offline_critical_minutes = table.Column<int>(type: "integer", nullable: true),
                    failed_login_warning_count = table.Column<int>(type: "integer", nullable: true),
                    failed_login_critical_count = table.Column<int>(type: "integer", nullable: true),
                    network_disconnect_warning_count = table.Column<int>(type: "integer", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_alert_thresholds", x => x.id);
                    table.ForeignKey(
                        name: "fk_alert_thresholds_alert_profiles_profile_id",
                        column: x => x.profile_id,
                        principalTable: "alert_profiles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "alert_profiles",
                columns: new[] { "id", "created_at", "description", "is_default", "name", "updated_at" },
                values: new object[,]
                {
                    { 1L, new DateTime(2026, 7, 15, 0, 0, 0, 0, DateTimeKind.Utc), "Standard monitoring thresholds for general office workstations.", true, "Default Profile", new DateTime(2026, 7, 15, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 2L, new DateTime(2026, 7, 15, 0, 0, 0, 0, DateTimeKind.Utc), "Optimized for typical office productivity workloads.", false, "Office Workstations", new DateTime(2026, 7, 15, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 3L, new DateTime(2026, 7, 15, 0, 0, 0, 0, DateTimeKind.Utc), "Relaxed thresholds for developer machines with high resource usage.", false, "Development Machines", new DateTime(2026, 7, 15, 0, 0, 0, 0, DateTimeKind.Utc) }
                });

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

            migrationBuilder.InsertData(
                table: "alert_thresholds",
                columns: new[] { "id", "cpu_critical_percent", "cpu_temp_critical", "cpu_temp_warning", "cpu_warning_duration_minutes", "cpu_warning_percent", "created_at", "disk_critical_percent", "disk_smart_critical_enabled", "disk_smart_warning_enabled", "disk_warning_percent", "failed_login_critical_count", "failed_login_warning_count", "network_disconnect_warning_count", "offline_critical_minutes", "offline_warning_minutes", "profile_id", "ram_critical_percent", "ram_warning_duration_minutes", "ram_warning_percent", "updated_at" },
                values: new object[,]
                {
                    { 1L, 95m, 90m, 80m, 5, 80m, new DateTime(2026, 7, 15, 0, 0, 0, 0, DateTimeKind.Utc), 95m, true, true, 85m, 15, 5, 3, 30, 10, 1L, 95m, 5, 80m, new DateTime(2026, 7, 15, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 2L, 90m, 85m, 75m, 5, 75m, new DateTime(2026, 7, 15, 0, 0, 0, 0, DateTimeKind.Utc), 95m, true, true, 85m, 10, 5, 3, 45, 15, 2L, 90m, 5, 75m, new DateTime(2026, 7, 15, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 3L, 98m, 95m, 85m, 10, 90m, new DateTime(2026, 7, 15, 0, 0, 0, 0, DateTimeKind.Utc), 97m, true, true, 90m, 10, 3, 5, 30, 10, 3L, 97m, 10, 85m, new DateTime(2026, 7, 15, 0, 0, 0, 0, DateTimeKind.Utc) }
                });

            migrationBuilder.CreateIndex(
                name: "ix_machines_custom_alert_profile_id",
                table: "machines",
                column: "custom_alert_profile_id");

            migrationBuilder.CreateIndex(
                name: "ix_companies_alert_profile_id",
                table: "companies",
                column: "alert_profile_id");

            migrationBuilder.CreateIndex(
                name: "ix_alert_profiles_name",
                table: "alert_profiles",
                column: "name");

            migrationBuilder.CreateIndex(
                name: "ix_alert_thresholds_profile_id",
                table: "alert_thresholds",
                column: "profile_id",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "fk_companies_alert_profiles_alert_profile_id",
                table: "companies",
                column: "alert_profile_id",
                principalTable: "alert_profiles",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "fk_machines_alert_profiles_custom_alert_profile_id",
                table: "machines",
                column: "custom_alert_profile_id",
                principalTable: "alert_profiles",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_companies_alert_profiles_alert_profile_id",
                table: "companies");

            migrationBuilder.DropForeignKey(
                name: "fk_machines_alert_profiles_custom_alert_profile_id",
                table: "machines");

            migrationBuilder.DropTable(
                name: "alert_thresholds");

            migrationBuilder.DropTable(
                name: "alert_profiles");

            migrationBuilder.DropIndex(
                name: "ix_machines_custom_alert_profile_id",
                table: "machines");

            migrationBuilder.DropIndex(
                name: "ix_companies_alert_profile_id",
                table: "companies");

            migrationBuilder.DropColumn(
                name: "custom_alert_profile_id",
                table: "machines");

            migrationBuilder.DropColumn(
                name: "alert_profile_id",
                table: "companies");

            migrationBuilder.UpdateData(
                table: "roles",
                keyColumn: "id",
                keyValue: 1L,
                columns: new[] { "created_at", "updated_at" },
                values: new object[] { new DateTime(2026, 7, 14, 16, 9, 12, 496, DateTimeKind.Utc).AddTicks(9170), new DateTime(2026, 7, 14, 16, 9, 12, 496, DateTimeKind.Utc).AddTicks(9174) });

            migrationBuilder.UpdateData(
                table: "roles",
                keyColumn: "id",
                keyValue: 2L,
                columns: new[] { "created_at", "updated_at" },
                values: new object[] { new DateTime(2026, 7, 14, 16, 9, 12, 496, DateTimeKind.Utc).AddTicks(9180), new DateTime(2026, 7, 14, 16, 9, 12, 496, DateTimeKind.Utc).AddTicks(9181) });

            migrationBuilder.UpdateData(
                table: "roles",
                keyColumn: "id",
                keyValue: 3L,
                columns: new[] { "created_at", "updated_at" },
                values: new object[] { new DateTime(2026, 7, 14, 16, 9, 12, 496, DateTimeKind.Utc).AddTicks(9183), new DateTime(2026, 7, 14, 16, 9, 12, 496, DateTimeKind.Utc).AddTicks(9183) });

            migrationBuilder.UpdateData(
                table: "roles",
                keyColumn: "id",
                keyValue: 4L,
                columns: new[] { "created_at", "updated_at" },
                values: new object[] { new DateTime(2026, 7, 14, 16, 9, 12, 496, DateTimeKind.Utc).AddTicks(9185), new DateTime(2026, 7, 14, 16, 9, 12, 496, DateTimeKind.Utc).AddTicks(9186) });
        }
    }
}
