using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace DeskGuardBackend.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "companies",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    phone = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    address = table.Column<string>(type: "text", nullable: true),
                    website = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_companies", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "otp_codes",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    mobile_number = table.Column<string>(type: "text", nullable: false),
                    otp = table.Column<string>(type: "text", nullable: false),
                    is_used = table.Column<bool>(type: "boolean", nullable: false),
                    used_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_otp_codes", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "permissions",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "text", nullable: false),
                    guard_name = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_permissions", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "personal_access_tokens",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    tokenable_type = table.Column<string>(type: "text", nullable: false),
                    tokenable_id = table.Column<long>(type: "bigint", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    token = table.Column<string>(type: "text", nullable: false),
                    abilities = table.Column<string>(type: "text", nullable: true),
                    last_used_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_personal_access_tokens", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "raw_payload_logs",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    machine_id = table.Column<long>(type: "bigint", nullable: true),
                    machine_uid = table.Column<string>(type: "text", nullable: true),
                    payload = table.Column<string>(type: "jsonb", nullable: true),
                    source_ip = table.Column<string>(type: "text", nullable: true),
                    received_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_raw_payload_logs", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "roles",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "text", nullable: false),
                    guard_name = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_roles", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "alert_rules",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    company_id = table.Column<long>(type: "bigint", nullable: true),
                    name = table.Column<string>(type: "text", nullable: false),
                    metric_type = table.Column<string>(type: "text", nullable: false),
                    condition = table.Column<string>(type: "text", nullable: false),
                    threshold_value = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    severity = table.Column<string>(type: "text", nullable: false),
                    is_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    cooldown_minutes = table.Column<int>(type: "integer", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_alert_rules", x => x.id);
                    table.ForeignKey(
                        name: "fk_alert_rules_companies_company_id",
                        column: x => x.company_id,
                        principalTable: "companies",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "email_recipients",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    company_id = table.Column<long>(type: "bigint", nullable: false),
                    email = table.Column<string>(type: "text", nullable: false),
                    name = table.Column<string>(type: "text", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_email_recipients", x => x.id);
                    table.ForeignKey(
                        name: "fk_email_recipients_companies_company_id",
                        column: x => x.company_id,
                        principalTable: "companies",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    company_id = table.Column<long>(type: "bigint", nullable: true),
                    mobile_number = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    password = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    is_verified = table.Column<bool>(type: "boolean", nullable: false),
                    phone = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    avatar = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    must_change_password = table.Column<bool>(type: "boolean", nullable: false),
                    last_login_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    activation_token = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_users", x => x.id);
                    table.ForeignKey(
                        name: "fk_users_companies_company_id",
                        column: x => x.company_id,
                        principalTable: "companies",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "role_has_permissions",
                columns: table => new
                {
                    permission_id = table.Column<long>(type: "bigint", nullable: false),
                    role_id = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_role_has_permissions", x => new { x.permission_id, x.role_id });
                    table.ForeignKey(
                        name: "fk_role_has_permissions_permissions_permission_id",
                        column: x => x.permission_id,
                        principalTable: "permissions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_role_has_permissions_roles_role_id",
                        column: x => x.role_id,
                        principalTable: "roles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "machines",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    company_id = table.Column<long>(type: "bigint", nullable: true),
                    user_id = table.Column<long>(type: "bigint", nullable: true),
                    machine_uid = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    hostname = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    device_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    operating_system = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    os_version = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    manufacturer = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    model = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    serial_number = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    bios_version = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    processor = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    ram_gb = table.Column<int>(type: "integer", nullable: true),
                    is_online = table.Column<bool>(type: "boolean", nullable: false),
                    last_heartbeat_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    activation_token = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    activated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    employee_mobile_number = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_machines", x => x.id);
                    table.ForeignKey(
                        name: "fk_machines_companies_company_id",
                        column: x => x.company_id,
                        principalTable: "companies",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_machines_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "model_has_roles",
                columns: table => new
                {
                    role_id = table.Column<long>(type: "bigint", nullable: false),
                    model_id = table.Column<long>(type: "bigint", nullable: false),
                    model_type = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_model_has_roles", x => new { x.role_id, x.model_id, x.model_type });
                    table.ForeignKey(
                        name: "fk_model_has_roles_roles_role_id",
                        column: x => x.role_id,
                        principalTable: "roles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_model_has_roles_users_user_id",
                        column: x => x.model_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "notifications",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    company_id = table.Column<long>(type: "bigint", nullable: true),
                    user_id = table.Column<long>(type: "bigint", nullable: true),
                    title = table.Column<string>(type: "text", nullable: false),
                    message = table.Column<string>(type: "text", nullable: true),
                    type = table.Column<string>(type: "text", nullable: true),
                    reference_type = table.Column<string>(type: "text", nullable: true),
                    reference_id = table.Column<long>(type: "bigint", nullable: true),
                    is_read = table.Column<bool>(type: "boolean", nullable: false),
                    read_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_notifications", x => x.id);
                    table.ForeignKey(
                        name: "fk_notifications_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "reports",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    company_id = table.Column<long>(type: "bigint", nullable: true),
                    generated_by = table.Column<long>(type: "bigint", nullable: true),
                    title = table.Column<string>(type: "text", nullable: false),
                    report_type = table.Column<string>(type: "text", nullable: true),
                    format = table.Column<string>(type: "text", nullable: true),
                    file_path = table.Column<string>(type: "text", nullable: true),
                    status = table.Column<string>(type: "text", nullable: true),
                    parameters = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_reports", x => x.id);
                    table.ForeignKey(
                        name: "fk_reports_companies_company_id",
                        column: x => x.company_id,
                        principalTable: "companies",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_reports_users_generated_by",
                        column: x => x.generated_by,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "alerts",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    company_id = table.Column<long>(type: "bigint", nullable: false),
                    machine_id = table.Column<long>(type: "bigint", nullable: false),
                    alert_rule_id = table.Column<long>(type: "bigint", nullable: true),
                    title = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    severity = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    status = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    acknowledged_by = table.Column<long>(type: "bigint", nullable: true),
                    acknowledged_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    resolved_by = table.Column<long>(type: "bigint", nullable: true),
                    resolved_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    resolution_note = table.Column<string>(type: "text", nullable: true),
                    metadata = table.Column<string>(type: "jsonb", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_alerts", x => x.id);
                    table.ForeignKey(
                        name: "fk_alerts_alert_rules_alert_rule_id",
                        column: x => x.alert_rule_id,
                        principalTable: "alert_rules",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_alerts_companies_company_id",
                        column: x => x.company_id,
                        principalTable: "companies",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_alerts_machines_machine_id",
                        column: x => x.machine_id,
                        principalTable: "machines",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_alerts_users_acknowledged_by",
                        column: x => x.acknowledged_by,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_alerts_users_resolved_by",
                        column: x => x.resolved_by,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "antivirus_status",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    machine_id = table.Column<long>(type: "bigint", nullable: false),
                    display_name = table.Column<string>(type: "text", nullable: true),
                    is_real_time_protection_enabled = table.Column<bool>(type: "boolean", nullable: true),
                    is_signature_up_to_date = table.Column<bool>(type: "boolean", nullable: true),
                    product_version = table.Column<string>(type: "text", nullable: true),
                    product_state = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_antivirus_status", x => x.id);
                    table.ForeignKey(
                        name: "fk_antivirus_status_machines_machine_id",
                        column: x => x.machine_id,
                        principalTable: "machines",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "audit_logs",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    user_id = table.Column<long>(type: "bigint", nullable: true),
                    machine_id = table.Column<long>(type: "bigint", nullable: true),
                    company_id = table.Column<long>(type: "bigint", nullable: true),
                    event_type = table.Column<string>(type: "text", nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    old_values = table.Column<string>(type: "jsonb", nullable: true),
                    new_values = table.Column<string>(type: "jsonb", nullable: true),
                    ip_address = table.Column<string>(type: "text", nullable: true),
                    user_agent = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_audit_logs", x => x.id);
                    table.ForeignKey(
                        name: "fk_audit_logs_machines_machine_id",
                        column: x => x.machine_id,
                        principalTable: "machines",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_audit_logs_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "change_history",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    company_id = table.Column<long>(type: "bigint", nullable: false),
                    machine_id = table.Column<long>(type: "bigint", nullable: false),
                    category = table.Column<string>(type: "text", nullable: false),
                    change_type = table.Column<string>(type: "text", nullable: false),
                    item_identifier = table.Column<string>(type: "text", nullable: true),
                    item_label = table.Column<string>(type: "text", nullable: true),
                    previous_value = table.Column<string>(type: "text", nullable: true),
                    new_value = table.Column<string>(type: "text", nullable: true),
                    description = table.Column<string>(type: "text", nullable: true),
                    severity = table.Column<string>(type: "text", nullable: true),
                    status = table.Column<string>(type: "text", nullable: true),
                    metadata = table.Column<string>(type: "jsonb", nullable: true),
                    detected_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_change_history", x => x.id);
                    table.ForeignKey(
                        name: "fk_change_history_companies_company_id",
                        column: x => x.company_id,
                        principalTable: "companies",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_change_history_machines_machine_id",
                        column: x => x.machine_id,
                        principalTable: "machines",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "configuration_baselines",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    machine_id = table.Column<long>(type: "bigint", nullable: false),
                    setting_type = table.Column<string>(type: "text", nullable: false),
                    identifier = table.Column<string>(type: "text", nullable: false),
                    label = table.Column<string>(type: "text", nullable: true),
                    current_value = table.Column<string>(type: "text", nullable: true),
                    metadata = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_configuration_baselines", x => x.id);
                    table.ForeignKey(
                        name: "fk_configuration_baselines_machines_machine_id",
                        column: x => x.machine_id,
                        principalTable: "machines",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "device_events",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    machine_id = table.Column<long>(type: "bigint", nullable: false),
                    device_name = table.Column<string>(type: "text", nullable: true),
                    device_type = table.Column<string>(type: "text", nullable: true),
                    device_id = table.Column<string>(type: "text", nullable: true),
                    event_type = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_device_events", x => x.id);
                    table.ForeignKey(
                        name: "fk_device_events_machines_machine_id",
                        column: x => x.machine_id,
                        principalTable: "machines",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "event_logs",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    machine_id = table.Column<long>(type: "bigint", nullable: false),
                    log_name = table.Column<string>(type: "text", nullable: true),
                    source = table.Column<string>(type: "text", nullable: true),
                    event_id = table.Column<int>(type: "integer", nullable: true),
                    level = table.Column<string>(type: "text", nullable: true),
                    message = table.Column<string>(type: "text", nullable: true),
                    time_generated = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    category = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_event_logs", x => x.id);
                    table.ForeignKey(
                        name: "fk_event_logs_machines_machine_id",
                        column: x => x.machine_id,
                        principalTable: "machines",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "firewall_status",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    machine_id = table.Column<long>(type: "bigint", nullable: false),
                    display_name = table.Column<string>(type: "text", nullable: true),
                    is_domain_firewall_enabled = table.Column<bool>(type: "boolean", nullable: true),
                    is_private_firewall_enabled = table.Column<bool>(type: "boolean", nullable: true),
                    is_public_firewall_enabled = table.Column<bool>(type: "boolean", nullable: true),
                    active_profile = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_firewall_status", x => x.id);
                    table.ForeignKey(
                        name: "fk_firewall_status_machines_machine_id",
                        column: x => x.machine_id,
                        principalTable: "machines",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "hardware_baselines",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    machine_id = table.Column<long>(type: "bigint", nullable: false),
                    component_type = table.Column<string>(type: "text", nullable: false),
                    identifier = table.Column<string>(type: "text", nullable: false),
                    label = table.Column<string>(type: "text", nullable: true),
                    current_value = table.Column<string>(type: "text", nullable: true),
                    metadata = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_hardware_baselines", x => x.id);
                    table.ForeignKey(
                        name: "fk_hardware_baselines_machines_machine_id",
                        column: x => x.machine_id,
                        principalTable: "machines",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "hardware_inventory",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    machine_id = table.Column<long>(type: "bigint", nullable: false),
                    cpu_model = table.Column<string>(type: "text", nullable: true),
                    cpu_cores = table.Column<int>(type: "integer", nullable: true),
                    cpu_threads = table.Column<int>(type: "integer", nullable: true),
                    cpu_max_clock_speed = table.Column<decimal>(type: "numeric", nullable: true),
                    cpu_architecture = table.Column<string>(type: "text", nullable: true),
                    total_ram_bytes = table.Column<long>(type: "bigint", nullable: true),
                    ram_slots = table.Column<int>(type: "integer", nullable: true),
                    ram_type = table.Column<string>(type: "text", nullable: true),
                    ram_speed = table.Column<string>(type: "text", nullable: true),
                    manufacturer = table.Column<string>(type: "text", nullable: true),
                    model = table.Column<string>(type: "text", nullable: true),
                    serial_number = table.Column<string>(type: "text", nullable: true),
                    bios_version = table.Column<string>(type: "text", nullable: true),
                    motherboard_model = table.Column<string>(type: "text", nullable: true),
                    gpu_name = table.Column<string>(type: "text", nullable: true),
                    gpu_driver_version = table.Column<string>(type: "text", nullable: true),
                    gpu_memory_bytes = table.Column<long>(type: "bigint", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_hardware_inventory", x => x.id);
                    table.ForeignKey(
                        name: "fk_hardware_inventory_machines_machine_id",
                        column: x => x.machine_id,
                        principalTable: "machines",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "health_logs",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    company_id = table.Column<long>(type: "bigint", nullable: true),
                    machine_id = table.Column<long>(type: "bigint", nullable: false),
                    cpu_percentage = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: true),
                    cpu_temperature = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: true),
                    cpu_clock_speed = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: true),
                    ram_total_bytes = table.Column<long>(type: "bigint", nullable: true),
                    ram_used_bytes = table.Column<long>(type: "bigint", nullable: true),
                    ram_available_bytes = table.Column<long>(type: "bigint", nullable: true),
                    ram_percentage = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: true),
                    disk_total_bytes = table.Column<long>(type: "bigint", nullable: true),
                    disk_used_bytes = table.Column<long>(type: "bigint", nullable: true),
                    disk_free_bytes = table.Column<long>(type: "bigint", nullable: true),
                    disk_percentage = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: true),
                    battery_percentage = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: true),
                    battery_charging_status = table.Column<bool>(type: "boolean", nullable: true),
                    network_received_bytes = table.Column<long>(type: "bigint", nullable: true),
                    network_sent_bytes = table.Column<long>(type: "bigint", nullable: true),
                    collected_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_health_logs", x => x.id);
                    table.ForeignKey(
                        name: "fk_health_logs_companies_company_id",
                        column: x => x.company_id,
                        principalTable: "companies",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_health_logs_machines_machine_id",
                        column: x => x.machine_id,
                        principalTable: "machines",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "login_activities",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    machine_id = table.Column<long>(type: "bigint", nullable: false),
                    company_id = table.Column<long>(type: "bigint", nullable: true),
                    username = table.Column<string>(type: "text", nullable: true),
                    event_type = table.Column<string>(type: "text", nullable: true),
                    is_success = table.Column<bool>(type: "boolean", nullable: false),
                    logon_type = table.Column<string>(type: "text", nullable: true),
                    source_ip = table.Column<string>(type: "text", nullable: true),
                    session_id = table.Column<string>(type: "text", nullable: true),
                    event_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_login_activities", x => x.id);
                    table.ForeignKey(
                        name: "fk_login_activities_machines_machine_id",
                        column: x => x.machine_id,
                        principalTable: "machines",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "machine_connected_devices",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    machine_id = table.Column<long>(type: "bigint", nullable: false),
                    device_name = table.Column<string>(type: "text", nullable: true),
                    device_type = table.Column<string>(type: "text", nullable: true),
                    device_id = table.Column<string>(type: "text", nullable: true),
                    status = table.Column<string>(type: "text", nullable: true),
                    manufacturer = table.Column<string>(type: "text", nullable: true),
                    driver_version = table.Column<string>(type: "text", nullable: true),
                    has_problem = table.Column<bool>(type: "boolean", nullable: true),
                    problem_description = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_machine_connected_devices", x => x.id);
                    table.ForeignKey(
                        name: "fk_machine_connected_devices_machines_machine_id",
                        column: x => x.machine_id,
                        principalTable: "machines",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "machine_current_status",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    machine_id = table.Column<long>(type: "bigint", nullable: false),
                    company_id = table.Column<long>(type: "bigint", nullable: true),
                    cpu_percentage = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: true),
                    cpu_temperature = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: true),
                    cpu_clock_speed = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: true),
                    cpu_core_count = table.Column<int>(type: "integer", nullable: true),
                    ram_total_bytes = table.Column<long>(type: "bigint", nullable: true),
                    ram_used_bytes = table.Column<long>(type: "bigint", nullable: true),
                    ram_available_bytes = table.Column<long>(type: "bigint", nullable: true),
                    ram_percentage = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: true),
                    disk_total_bytes = table.Column<long>(type: "bigint", nullable: true),
                    disk_used_bytes = table.Column<long>(type: "bigint", nullable: true),
                    disk_free_bytes = table.Column<long>(type: "bigint", nullable: true),
                    disk_percentage = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: true),
                    disk_health_status = table.Column<string>(type: "text", nullable: true),
                    battery_percentage = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: true),
                    battery_charging_status = table.Column<bool>(type: "boolean", nullable: true),
                    battery_wear_level = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: true),
                    network_received_bytes = table.Column<long>(type: "bigint", nullable: true),
                    network_sent_bytes = table.Column<long>(type: "bigint", nullable: true),
                    antivirus_name = table.Column<string>(type: "text", nullable: true),
                    antivirus_enabled = table.Column<bool>(type: "boolean", nullable: true),
                    firewall_enabled = table.Column<bool>(type: "boolean", nullable: true),
                    network_interfaces = table.Column<string>(type: "text", nullable: true),
                    online_status = table.Column<bool>(type: "boolean", nullable: false),
                    last_collected_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    collected_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_machine_current_status", x => x.id);
                    table.ForeignKey(
                        name: "fk_machine_current_status_machines_machine_id",
                        column: x => x.machine_id,
                        principalTable: "machines",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "machine_disks",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    machine_id = table.Column<long>(type: "bigint", nullable: false),
                    drive_letter = table.Column<string>(type: "text", nullable: true),
                    volume_label = table.Column<string>(type: "text", nullable: true),
                    file_system = table.Column<string>(type: "text", nullable: true),
                    drive_type = table.Column<string>(type: "text", nullable: true),
                    total_gb = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: true),
                    used_gb = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: true),
                    free_gb = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: true),
                    health_status = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_machine_disks", x => x.id);
                    table.ForeignKey(
                        name: "fk_machine_disks_machines_machine_id",
                        column: x => x.machine_id,
                        principalTable: "machines",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "machine_network_adapters",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    machine_id = table.Column<long>(type: "bigint", nullable: false),
                    adapter_name = table.Column<string>(type: "text", nullable: false),
                    ip_address = table.Column<string>(type: "text", nullable: true),
                    mac_address = table.Column<string>(type: "text", nullable: true),
                    adapter_type = table.Column<string>(type: "text", nullable: true),
                    speed = table.Column<long>(type: "bigint", nullable: true),
                    status = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_machine_network_adapters", x => x.id);
                    table.ForeignKey(
                        name: "fk_machine_network_adapters_machines_machine_id",
                        column: x => x.machine_id,
                        principalTable: "machines",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "machine_tokens",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    machine_id = table.Column<long>(type: "bigint", nullable: false),
                    token = table.Column<string>(type: "text", nullable: false),
                    expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    last_used_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_machine_tokens", x => x.id);
                    table.ForeignKey(
                        name: "fk_machine_tokens_machines_machine_id",
                        column: x => x.machine_id,
                        principalTable: "machines",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "process_logs",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    machine_id = table.Column<long>(type: "bigint", nullable: false),
                    process_name = table.Column<string>(type: "text", nullable: false),
                    process_id = table.Column<int>(type: "integer", nullable: true),
                    cpu_usage_percentage = table.Column<decimal>(type: "numeric(10,4)", precision: 10, scale: 4, nullable: true),
                    working_set_bytes = table.Column<long>(type: "bigint", nullable: true),
                    memory_usage_mb = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_process_logs", x => x.id);
                    table.ForeignKey(
                        name: "fk_process_logs_machines_machine_id",
                        column: x => x.machine_id,
                        principalTable: "machines",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "security_baselines",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    machine_id = table.Column<long>(type: "bigint", nullable: false),
                    setting_type = table.Column<string>(type: "text", nullable: false),
                    identifier = table.Column<string>(type: "text", nullable: false),
                    label = table.Column<string>(type: "text", nullable: true),
                    current_value = table.Column<string>(type: "text", nullable: true),
                    metadata = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_security_baselines", x => x.id);
                    table.ForeignKey(
                        name: "fk_security_baselines_machines_machine_id",
                        column: x => x.machine_id,
                        principalTable: "machines",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "software_baselines",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    machine_id = table.Column<long>(type: "bigint", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    version = table.Column<string>(type: "text", nullable: true),
                    publisher = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_software_baselines", x => x.id);
                    table.ForeignKey(
                        name: "fk_software_baselines_machines_machine_id",
                        column: x => x.machine_id,
                        principalTable: "machines",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "software_inventory",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    machine_id = table.Column<long>(type: "bigint", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    version = table.Column<string>(type: "text", nullable: true),
                    publisher = table.Column<string>(type: "text", nullable: true),
                    install_date = table.Column<string>(type: "text", nullable: true),
                    install_location = table.Column<string>(type: "text", nullable: true),
                    estimated_size = table.Column<long>(type: "bigint", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_software_inventory", x => x.id);
                    table.ForeignKey(
                        name: "fk_software_inventory_machines_machine_id",
                        column: x => x.machine_id,
                        principalTable: "machines",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "startup_programs",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    machine_id = table.Column<long>(type: "bigint", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    command = table.Column<string>(type: "text", nullable: true),
                    location = table.Column<string>(type: "text", nullable: true),
                    user = table.Column<string>(type: "text", nullable: true),
                    registry_key = table.Column<string>(type: "text", nullable: true),
                    status = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_startup_programs", x => x.id);
                    table.ForeignKey(
                        name: "fk_startup_programs_machines_machine_id",
                        column: x => x.machine_id,
                        principalTable: "machines",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "usb_activities",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    machine_id = table.Column<long>(type: "bigint", nullable: false),
                    company_id = table.Column<long>(type: "bigint", nullable: true),
                    device_name = table.Column<string>(type: "text", nullable: true),
                    device_serial = table.Column<string>(type: "text", nullable: true),
                    event_type = table.Column<string>(type: "text", nullable: true),
                    event_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_usb_activities", x => x.id);
                    table.ForeignKey(
                        name: "fk_usb_activities_machines_machine_id",
                        column: x => x.machine_id,
                        principalTable: "machines",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "windows_services",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    machine_id = table.Column<long>(type: "bigint", nullable: false),
                    service_name = table.Column<string>(type: "text", nullable: false),
                    display_name = table.Column<string>(type: "text", nullable: true),
                    status = table.Column<string>(type: "text", nullable: true),
                    start_type = table.Column<string>(type: "text", nullable: true),
                    service_type = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_windows_services", x => x.id);
                    table.ForeignKey(
                        name: "fk_windows_services_machines_machine_id",
                        column: x => x.machine_id,
                        principalTable: "machines",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "windows_updates",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    machine_id = table.Column<long>(type: "bigint", nullable: false),
                    update_title = table.Column<string>(type: "text", nullable: true),
                    kb_article_id = table.Column<string>(type: "text", nullable: true),
                    is_installed = table.Column<bool>(type: "boolean", nullable: false),
                    is_mandatory = table.Column<bool>(type: "boolean", nullable: true),
                    severity = table.Column<string>(type: "text", nullable: true),
                    installed_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    pending_update_count = table.Column<int>(type: "integer", nullable: true),
                    last_checked_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_windows_updates", x => x.id);
                    table.ForeignKey(
                        name: "fk_windows_updates_machines_machine_id",
                        column: x => x.machine_id,
                        principalTable: "machines",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "roles",
                columns: new[] { "id", "created_at", "guard_name", "name", "updated_at" },
                values: new object[,]
                {
                    { 1L, new DateTime(2026, 7, 12, 6, 15, 42, 689, DateTimeKind.Utc).AddTicks(1455), "web", "Super Admin", new DateTime(2026, 7, 12, 6, 15, 42, 689, DateTimeKind.Utc).AddTicks(1461) },
                    { 2L, new DateTime(2026, 7, 12, 6, 15, 42, 689, DateTimeKind.Utc).AddTicks(1472), "web", "Company Admin", new DateTime(2026, 7, 12, 6, 15, 42, 689, DateTimeKind.Utc).AddTicks(1472) },
                    { 3L, new DateTime(2026, 7, 12, 6, 15, 42, 689, DateTimeKind.Utc).AddTicks(1476), "web", "Support Technician", new DateTime(2026, 7, 12, 6, 15, 42, 689, DateTimeKind.Utc).AddTicks(1477) }
                });

            migrationBuilder.CreateIndex(
                name: "ix_alert_rules_company_id",
                table: "alert_rules",
                column: "company_id");

            migrationBuilder.CreateIndex(
                name: "ix_alerts_acknowledged_by",
                table: "alerts",
                column: "acknowledged_by");

            migrationBuilder.CreateIndex(
                name: "ix_alerts_alert_rule_id",
                table: "alerts",
                column: "alert_rule_id");

            migrationBuilder.CreateIndex(
                name: "ix_alerts_company_id",
                table: "alerts",
                column: "company_id");

            migrationBuilder.CreateIndex(
                name: "ix_alerts_company_id_status_created_at",
                table: "alerts",
                columns: new[] { "company_id", "status", "created_at" });

            migrationBuilder.CreateIndex(
                name: "ix_alerts_created_at",
                table: "alerts",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "ix_alerts_machine_id",
                table: "alerts",
                column: "machine_id");

            migrationBuilder.CreateIndex(
                name: "ix_alerts_resolved_by",
                table: "alerts",
                column: "resolved_by");

            migrationBuilder.CreateIndex(
                name: "ix_alerts_severity",
                table: "alerts",
                column: "severity");

            migrationBuilder.CreateIndex(
                name: "ix_alerts_status",
                table: "alerts",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ix_antivirus_status_machine_id",
                table: "antivirus_status",
                column: "machine_id");

            migrationBuilder.CreateIndex(
                name: "ix_audit_logs_company_id",
                table: "audit_logs",
                column: "company_id");

            migrationBuilder.CreateIndex(
                name: "ix_audit_logs_created_at",
                table: "audit_logs",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "ix_audit_logs_event_type",
                table: "audit_logs",
                column: "event_type");

            migrationBuilder.CreateIndex(
                name: "ix_audit_logs_machine_id",
                table: "audit_logs",
                column: "machine_id");

            migrationBuilder.CreateIndex(
                name: "ix_audit_logs_user_id",
                table: "audit_logs",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_change_history_category",
                table: "change_history",
                column: "category");

            migrationBuilder.CreateIndex(
                name: "ix_change_history_change_type",
                table: "change_history",
                column: "change_type");

            migrationBuilder.CreateIndex(
                name: "ix_change_history_company_id",
                table: "change_history",
                column: "company_id");

            migrationBuilder.CreateIndex(
                name: "ix_change_history_machine_id_detected_at",
                table: "change_history",
                columns: new[] { "machine_id", "detected_at" });

            migrationBuilder.CreateIndex(
                name: "ix_companies_email",
                table: "companies",
                column: "email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_companies_is_active",
                table: "companies",
                column: "is_active");

            migrationBuilder.CreateIndex(
                name: "ix_configuration_baselines_machine_id",
                table: "configuration_baselines",
                column: "machine_id");

            migrationBuilder.CreateIndex(
                name: "ix_device_events_machine_id",
                table: "device_events",
                column: "machine_id");

            migrationBuilder.CreateIndex(
                name: "ix_email_recipients_company_id",
                table: "email_recipients",
                column: "company_id");

            migrationBuilder.CreateIndex(
                name: "ix_event_logs_machine_id",
                table: "event_logs",
                column: "machine_id");

            migrationBuilder.CreateIndex(
                name: "ix_event_logs_time_generated",
                table: "event_logs",
                column: "time_generated");

            migrationBuilder.CreateIndex(
                name: "ix_firewall_status_machine_id",
                table: "firewall_status",
                column: "machine_id");

            migrationBuilder.CreateIndex(
                name: "ix_hardware_baselines_machine_id",
                table: "hardware_baselines",
                column: "machine_id");

            migrationBuilder.CreateIndex(
                name: "ix_hardware_baselines_machine_id_component_type_identifier",
                table: "hardware_baselines",
                columns: new[] { "machine_id", "component_type", "identifier" });

            migrationBuilder.CreateIndex(
                name: "ix_hardware_inventory_machine_id",
                table: "hardware_inventory",
                column: "machine_id");

            migrationBuilder.CreateIndex(
                name: "ix_health_logs_collected_at",
                table: "health_logs",
                column: "collected_at");

            migrationBuilder.CreateIndex(
                name: "ix_health_logs_company_id",
                table: "health_logs",
                column: "company_id");

            migrationBuilder.CreateIndex(
                name: "ix_health_logs_machine_id",
                table: "health_logs",
                column: "machine_id");

            migrationBuilder.CreateIndex(
                name: "ix_health_logs_machine_id_collected_at",
                table: "health_logs",
                columns: new[] { "machine_id", "collected_at" });

            migrationBuilder.CreateIndex(
                name: "ix_login_activities_company_id",
                table: "login_activities",
                column: "company_id");

            migrationBuilder.CreateIndex(
                name: "ix_login_activities_event_time",
                table: "login_activities",
                column: "event_time");

            migrationBuilder.CreateIndex(
                name: "ix_login_activities_machine_id",
                table: "login_activities",
                column: "machine_id");

            migrationBuilder.CreateIndex(
                name: "ix_machine_connected_devices_machine_id",
                table: "machine_connected_devices",
                column: "machine_id");

            migrationBuilder.CreateIndex(
                name: "ix_machine_current_status_machine_id",
                table: "machine_current_status",
                column: "machine_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_machine_disks_machine_id",
                table: "machine_disks",
                column: "machine_id");

            migrationBuilder.CreateIndex(
                name: "ix_machine_network_adapters_machine_id",
                table: "machine_network_adapters",
                column: "machine_id");

            migrationBuilder.CreateIndex(
                name: "ix_machine_tokens_machine_id",
                table: "machine_tokens",
                column: "machine_id");

            migrationBuilder.CreateIndex(
                name: "ix_machine_tokens_token",
                table: "machine_tokens",
                column: "token");

            migrationBuilder.CreateIndex(
                name: "ix_machines_company_id",
                table: "machines",
                column: "company_id");

            migrationBuilder.CreateIndex(
                name: "ix_machines_company_id_is_online",
                table: "machines",
                columns: new[] { "company_id", "is_online" });

            migrationBuilder.CreateIndex(
                name: "ix_machines_is_online",
                table: "machines",
                column: "is_online");

            migrationBuilder.CreateIndex(
                name: "ix_machines_machine_uid",
                table: "machines",
                column: "machine_uid",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_machines_user_id",
                table: "machines",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_model_has_roles_user_id",
                table: "model_has_roles",
                column: "model_id");

            migrationBuilder.CreateIndex(
                name: "ix_notifications_user_id_is_read",
                table: "notifications",
                columns: new[] { "user_id", "is_read" });

            migrationBuilder.CreateIndex(
                name: "ix_otp_codes_mobile_number",
                table: "otp_codes",
                column: "mobile_number");

            migrationBuilder.CreateIndex(
                name: "ix_otp_codes_mobile_number_is_used_expires_at",
                table: "otp_codes",
                columns: new[] { "mobile_number", "is_used", "expires_at" });

            migrationBuilder.CreateIndex(
                name: "ix_permissions_name_guard_name",
                table: "permissions",
                columns: new[] { "name", "guard_name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_personal_access_tokens_token",
                table: "personal_access_tokens",
                column: "token",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_personal_access_tokens_tokenable_type_tokenable_id",
                table: "personal_access_tokens",
                columns: new[] { "tokenable_type", "tokenable_id" });

            migrationBuilder.CreateIndex(
                name: "ix_process_logs_machine_id",
                table: "process_logs",
                column: "machine_id");

            migrationBuilder.CreateIndex(
                name: "ix_raw_payload_logs_machine_id",
                table: "raw_payload_logs",
                column: "machine_id");

            migrationBuilder.CreateIndex(
                name: "ix_reports_company_id",
                table: "reports",
                column: "company_id");

            migrationBuilder.CreateIndex(
                name: "ix_reports_generated_by",
                table: "reports",
                column: "generated_by");

            migrationBuilder.CreateIndex(
                name: "ix_role_has_permissions_role_id",
                table: "role_has_permissions",
                column: "role_id");

            migrationBuilder.CreateIndex(
                name: "ix_roles_name_guard_name",
                table: "roles",
                columns: new[] { "name", "guard_name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_security_baselines_machine_id",
                table: "security_baselines",
                column: "machine_id");

            migrationBuilder.CreateIndex(
                name: "ix_software_baselines_machine_id",
                table: "software_baselines",
                column: "machine_id");

            migrationBuilder.CreateIndex(
                name: "ix_software_baselines_machine_id_name",
                table: "software_baselines",
                columns: new[] { "machine_id", "name" });

            migrationBuilder.CreateIndex(
                name: "ix_software_inventory_machine_id",
                table: "software_inventory",
                column: "machine_id");

            migrationBuilder.CreateIndex(
                name: "ix_software_inventory_machine_id_name",
                table: "software_inventory",
                columns: new[] { "machine_id", "name" });

            migrationBuilder.CreateIndex(
                name: "ix_startup_programs_machine_id",
                table: "startup_programs",
                column: "machine_id");

            migrationBuilder.CreateIndex(
                name: "ix_usb_activities_company_id",
                table: "usb_activities",
                column: "company_id");

            migrationBuilder.CreateIndex(
                name: "ix_usb_activities_event_time",
                table: "usb_activities",
                column: "event_time");

            migrationBuilder.CreateIndex(
                name: "ix_usb_activities_machine_id",
                table: "usb_activities",
                column: "machine_id");

            migrationBuilder.CreateIndex(
                name: "ix_users_company_id",
                table: "users",
                column: "company_id");

            migrationBuilder.CreateIndex(
                name: "ix_users_email",
                table: "users",
                column: "email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_users_is_active",
                table: "users",
                column: "is_active");

            migrationBuilder.CreateIndex(
                name: "ix_users_mobile_number",
                table: "users",
                column: "mobile_number");

            migrationBuilder.CreateIndex(
                name: "ix_windows_services_machine_id",
                table: "windows_services",
                column: "machine_id");

            migrationBuilder.CreateIndex(
                name: "ix_windows_updates_machine_id",
                table: "windows_updates",
                column: "machine_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "alerts");

            migrationBuilder.DropTable(
                name: "antivirus_status");

            migrationBuilder.DropTable(
                name: "audit_logs");

            migrationBuilder.DropTable(
                name: "change_history");

            migrationBuilder.DropTable(
                name: "configuration_baselines");

            migrationBuilder.DropTable(
                name: "device_events");

            migrationBuilder.DropTable(
                name: "email_recipients");

            migrationBuilder.DropTable(
                name: "event_logs");

            migrationBuilder.DropTable(
                name: "firewall_status");

            migrationBuilder.DropTable(
                name: "hardware_baselines");

            migrationBuilder.DropTable(
                name: "hardware_inventory");

            migrationBuilder.DropTable(
                name: "health_logs");

            migrationBuilder.DropTable(
                name: "login_activities");

            migrationBuilder.DropTable(
                name: "machine_connected_devices");

            migrationBuilder.DropTable(
                name: "machine_current_status");

            migrationBuilder.DropTable(
                name: "machine_disks");

            migrationBuilder.DropTable(
                name: "machine_network_adapters");

            migrationBuilder.DropTable(
                name: "machine_tokens");

            migrationBuilder.DropTable(
                name: "model_has_roles");

            migrationBuilder.DropTable(
                name: "notifications");

            migrationBuilder.DropTable(
                name: "otp_codes");

            migrationBuilder.DropTable(
                name: "personal_access_tokens");

            migrationBuilder.DropTable(
                name: "process_logs");

            migrationBuilder.DropTable(
                name: "raw_payload_logs");

            migrationBuilder.DropTable(
                name: "reports");

            migrationBuilder.DropTable(
                name: "role_has_permissions");

            migrationBuilder.DropTable(
                name: "security_baselines");

            migrationBuilder.DropTable(
                name: "software_baselines");

            migrationBuilder.DropTable(
                name: "software_inventory");

            migrationBuilder.DropTable(
                name: "startup_programs");

            migrationBuilder.DropTable(
                name: "usb_activities");

            migrationBuilder.DropTable(
                name: "windows_services");

            migrationBuilder.DropTable(
                name: "windows_updates");

            migrationBuilder.DropTable(
                name: "alert_rules");

            migrationBuilder.DropTable(
                name: "permissions");

            migrationBuilder.DropTable(
                name: "roles");

            migrationBuilder.DropTable(
                name: "machines");

            migrationBuilder.DropTable(
                name: "users");

            migrationBuilder.DropTable(
                name: "companies");
        }
    }
}
