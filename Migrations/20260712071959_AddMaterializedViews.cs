using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DeskGuardBackend.Migrations
{
    /// <inheritdoc />
    public partial class AddMaterializedViews : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Hourly health aggregates per machine (used by dashboard chart queries)
            // Pre-computes hourly CPU/RAM averages with a rolling 48-hour window
            migrationBuilder.Sql("""
                CREATE MATERIALIZED VIEW IF NOT EXISTS mv_hourly_health AS
                SELECT
                    machine_id,
                    company_id,
                    date_trunc('hour', collected_at) AS hour_bucket,
                    ROUND(AVG(cpu_percentage)::numeric, 1) AS avg_cpu,
                    ROUND(AVG(ram_percentage)::numeric, 1) AS avg_ram,
                    COUNT(*) AS data_points
                FROM health_logs
                WHERE collected_at >= NOW() - INTERVAL '48 hours'
                    AND (cpu_percentage IS NOT NULL OR ram_percentage IS NOT NULL)
                GROUP BY machine_id, company_id, date_trunc('hour', collected_at)
                WITH NO DATA;
            """);

            migrationBuilder.Sql("CREATE INDEX IF NOT EXISTS idx_mv_hourly_health_company ON mv_hourly_health (company_id, hour_bucket);");
            migrationBuilder.Sql("CREATE INDEX IF NOT EXISTS idx_mv_hourly_health_machine ON mv_hourly_health (machine_id, hour_bucket);");

            // Daily alert counts per company by severity (used by alert chart queries)
            migrationBuilder.Sql("""
                CREATE MATERIALIZED VIEW IF NOT EXISTS mv_daily_alerts AS
                SELECT
                    company_id,
                    severity,
                    DATE(created_at) AS alert_date,
                    COUNT(*) AS alert_count
                FROM alerts
                WHERE created_at >= NOW() - INTERVAL '30 days'
                GROUP BY company_id, severity, DATE(created_at)
                WITH NO DATA;
            """);

            migrationBuilder.Sql("CREATE UNIQUE INDEX IF NOT EXISTS idx_mv_daily_alerts_pk ON mv_daily_alerts (company_id, severity, alert_date);");

            // Company dashboard summary (machine counts + alert counts)
            migrationBuilder.Sql("""
                CREATE MATERIALIZED VIEW IF NOT EXISTS mv_company_summary AS
                SELECT
                    m.company_id,
                    COUNT(*) AS total_machines,
                    COUNT(*) FILTER (WHERE m.is_online) AS online_count,
                    COUNT(*) FILTER (WHERE NOT m.is_online) AS offline_count,
                    COUNT(a.id) FILTER (WHERE a.severity = 'critical' AND a.status IN ('open', 'acknowledged')) AS critical_alerts,
                    COUNT(a.id) AS total_alerts
                FROM machines m
                LEFT JOIN alerts a ON a.machine_id = m.id
                GROUP BY m.company_id
                WITH NO DATA;
            """);

            migrationBuilder.Sql("CREATE UNIQUE INDEX IF NOT EXISTS idx_mv_company_summary_pk ON mv_company_summary (company_id);");

            // Refresh the views immediately after creation
            migrationBuilder.Sql("REFRESH MATERIALIZED VIEW mv_hourly_health;");
            migrationBuilder.Sql("REFRESH MATERIALIZED VIEW mv_daily_alerts;");
            migrationBuilder.Sql("REFRESH MATERIALIZED VIEW mv_company_summary;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP MATERIALIZED VIEW IF EXISTS mv_hourly_health CASCADE;");
            migrationBuilder.Sql("DROP MATERIALIZED VIEW IF EXISTS mv_daily_alerts CASCADE;");
            migrationBuilder.Sql("DROP MATERIALIZED VIEW IF EXISTS mv_company_summary CASCADE;");
        }
    }
}
