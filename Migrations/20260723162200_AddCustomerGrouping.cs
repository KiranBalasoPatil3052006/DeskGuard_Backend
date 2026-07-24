using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace DeskGuardBackend.Migrations
{
    /// <inheritdoc />
    public partial class AddCustomerGrouping : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {

            migrationBuilder.AddColumn<byte[]>(
                name: "file_content",
                table: "reports",
                type: "bytea",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "generator_name",
                table: "reports",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "customer_id",
                table: "machines",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "department",
                table: "email_recipients",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "customer_id",
                table: "companies",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "alert_type",
                table: "alerts",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "current_value",
                table: "alerts",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "duration_seconds",
                table: "alerts",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "first_detected_at",
                table: "alerts",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "last_detected_at",
                table: "alerts",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "max_recorded_value",
                table: "alerts",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "occurrence_count",
                table: "alerts",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "resource",
                table: "alerts",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "threshold_value",
                table: "alerts",
                type: "numeric",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "customers",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    customer_code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    company_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    customer_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    mobile_number = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValue: "Active"),
                    remarks = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_customers", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "email_logs",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    company_id = table.Column<long>(type: "bigint", nullable: true),
                    alert_id = table.Column<long>(type: "bigint", nullable: true),
                    machine_id = table.Column<long>(type: "bigint", nullable: true),
                    recipient_email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    subject = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    sent_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    failure_reason = table.Column<string>(type: "text", nullable: true),
                    retry_count = table.Column<int>(type: "integer", nullable: false),
                    smtp_response = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_email_logs", x => x.id);
                    table.ForeignKey(
                        name: "fk_email_logs_alerts_alert_id",
                        column: x => x.alert_id,
                        principalTable: "alerts",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_email_logs_companies_company_id",
                        column: x => x.company_id,
                        principalTable: "companies",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_email_logs_machines_machine_id",
                        column: x => x.machine_id,
                        principalTable: "machines",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "notification_rules",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    company_id = table.Column<long>(type: "bigint", nullable: true),
                    category = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    event_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    display_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    send_email = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_notification_rules", x => x.id);
                    table.ForeignKey(
                        name: "fk_notification_rules_companies_company_id",
                        column: x => x.company_id,
                        principalTable: "companies",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "security_settings",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    company_id = table.Column<long>(type: "bigint", nullable: true),
                    min_password_length = table.Column<int>(type: "integer", nullable: false),
                    require_uppercase = table.Column<bool>(type: "boolean", nullable: false),
                    require_lowercase = table.Column<bool>(type: "boolean", nullable: false),
                    require_numbers = table.Column<bool>(type: "boolean", nullable: false),
                    require_special_chars = table.Column<bool>(type: "boolean", nullable: false),
                    idle_session_timeout_minutes = table.Column<int>(type: "integer", nullable: false),
                    max_failed_login_attempts = table.Column<int>(type: "integer", nullable: false),
                    account_lockout_duration_minutes = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_security_settings", x => x.id);
                    table.ForeignKey(
                        name: "fk_security_settings_companies_company_id",
                        column: x => x.company_id,
                        principalTable: "companies",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "smtp_configurations",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    company_id = table.Column<long>(type: "bigint", nullable: true),
                    host = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    port = table.Column<int>(type: "integer", nullable: false),
                    username = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    encrypted_password = table.Column<string>(type: "text", nullable: false),
                    enable_ssl = table.Column<bool>(type: "boolean", nullable: false),
                    encryption_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    from_email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    from_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    timeout_seconds = table.Column<int>(type: "integer", nullable: false),
                    retry_count = table.Column<int>(type: "integer", nullable: false),
                    retry_delay_seconds = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_smtp_configurations", x => x.id);
                    table.ForeignKey(
                        name: "fk_smtp_configurations_companies_company_id",
                        column: x => x.company_id,
                        principalTable: "companies",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "user_login_histories",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    user_id = table.Column<long>(type: "bigint", nullable: true),
                    company_id = table.Column<long>(type: "bigint", nullable: true),
                    email = table.Column<string>(type: "text", nullable: false),
                    login_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    logout_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ip_address = table.Column<string>(type: "text", nullable: true),
                    user_agent = table.Column<string>(type: "text", nullable: true),
                    browser = table.Column<string>(type: "text", nullable: true),
                    operating_system = table.Column<string>(type: "text", nullable: true),
                    status = table.Column<string>(type: "text", nullable: false),
                    failure_reason = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_user_login_histories", x => x.id);
                    table.ForeignKey(
                        name: "fk_user_login_histories_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id");
                });

            migrationBuilder.UpdateData(
                table: "roles",
                keyColumn: "id",
                keyValue: 1L,
                columns: new[] { "created_at", "updated_at" },
                values: new object[] { new DateTime(2026, 7, 23, 16, 21, 57, 328, DateTimeKind.Utc).AddTicks(8438), new DateTime(2026, 7, 23, 16, 21, 57, 328, DateTimeKind.Utc).AddTicks(8446) });

            migrationBuilder.UpdateData(
                table: "roles",
                keyColumn: "id",
                keyValue: 2L,
                columns: new[] { "created_at", "updated_at" },
                values: new object[] { new DateTime(2026, 7, 23, 16, 21, 57, 328, DateTimeKind.Utc).AddTicks(8454), new DateTime(2026, 7, 23, 16, 21, 57, 328, DateTimeKind.Utc).AddTicks(8455) });

            migrationBuilder.UpdateData(
                table: "roles",
                keyColumn: "id",
                keyValue: 3L,
                columns: new[] { "created_at", "updated_at" },
                values: new object[] { new DateTime(2026, 7, 23, 16, 21, 57, 328, DateTimeKind.Utc).AddTicks(8458), new DateTime(2026, 7, 23, 16, 21, 57, 328, DateTimeKind.Utc).AddTicks(8459) });

            migrationBuilder.UpdateData(
                table: "roles",
                keyColumn: "id",
                keyValue: 4L,
                columns: new[] { "created_at", "updated_at" },
                values: new object[] { new DateTime(2026, 7, 23, 16, 21, 57, 328, DateTimeKind.Utc).AddTicks(8471), new DateTime(2026, 7, 23, 16, 21, 57, 328, DateTimeKind.Utc).AddTicks(8472) });

            migrationBuilder.CreateIndex(
                name: "ix_machines_customer_id",
                table: "machines",
                column: "customer_id");

            migrationBuilder.CreateIndex(
                name: "ix_customers_company_name_mobile_number",
                table: "customers",
                columns: new[] { "company_name", "mobile_number" });

            migrationBuilder.CreateIndex(
                name: "ix_email_logs_alert_id",
                table: "email_logs",
                column: "alert_id");

            migrationBuilder.CreateIndex(
                name: "ix_email_logs_company_id",
                table: "email_logs",
                column: "company_id");

            migrationBuilder.CreateIndex(
                name: "ix_email_logs_machine_id",
                table: "email_logs",
                column: "machine_id");

            migrationBuilder.CreateIndex(
                name: "ix_notification_rules_company_id",
                table: "notification_rules",
                column: "company_id");

            migrationBuilder.CreateIndex(
                name: "ix_security_settings_company_id",
                table: "security_settings",
                column: "company_id");

            migrationBuilder.CreateIndex(
                name: "ix_smtp_configurations_company_id",
                table: "smtp_configurations",
                column: "company_id");

            migrationBuilder.CreateIndex(
                name: "ix_user_login_histories_user_id",
                table: "user_login_histories",
                column: "user_id");

            migrationBuilder.AddForeignKey(
                name: "fk_machines_customers_customer_id",
                table: "machines",
                column: "customer_id",
                principalTable: "customers",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_machines_customers_customer_id",
                table: "machines");

            migrationBuilder.DropTable(
                name: "customers");

            migrationBuilder.DropTable(
                name: "email_logs");

            migrationBuilder.DropTable(
                name: "notification_rules");

            migrationBuilder.DropTable(
                name: "security_settings");

            migrationBuilder.DropTable(
                name: "smtp_configurations");

            migrationBuilder.DropTable(
                name: "user_login_histories");

            migrationBuilder.DropIndex(
                name: "ix_machines_customer_id",
                table: "machines");


            migrationBuilder.DropColumn(
                name: "file_content",
                table: "reports");

            migrationBuilder.DropColumn(
                name: "generator_name",
                table: "reports");

            migrationBuilder.DropColumn(
                name: "customer_id",
                table: "machines");

            migrationBuilder.DropColumn(
                name: "department",
                table: "email_recipients");

            migrationBuilder.DropColumn(
                name: "customer_id",
                table: "companies");

            migrationBuilder.DropColumn(
                name: "alert_type",
                table: "alerts");

            migrationBuilder.DropColumn(
                name: "current_value",
                table: "alerts");

            migrationBuilder.DropColumn(
                name: "duration_seconds",
                table: "alerts");

            migrationBuilder.DropColumn(
                name: "first_detected_at",
                table: "alerts");

            migrationBuilder.DropColumn(
                name: "last_detected_at",
                table: "alerts");

            migrationBuilder.DropColumn(
                name: "max_recorded_value",
                table: "alerts");

            migrationBuilder.DropColumn(
                name: "occurrence_count",
                table: "alerts");

            migrationBuilder.DropColumn(
                name: "resource",
                table: "alerts");

            migrationBuilder.DropColumn(
                name: "threshold_value",
                table: "alerts");

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
        }
    }
}
