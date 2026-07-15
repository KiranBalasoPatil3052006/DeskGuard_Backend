using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DeskGuardBackend.Migrations
{
    /// <inheritdoc />
    public partial class AddEmployeeIdToUsers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "created_by_user_id",
                table: "users",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "employee_id",
                table: "users",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

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

            migrationBuilder.InsertData(
                table: "roles",
                columns: new[] { "id", "created_at", "guard_name", "name", "updated_at" },
                values: new object[] { 4L, new DateTime(2026, 7, 14, 16, 9, 12, 496, DateTimeKind.Utc).AddTicks(9185), "web", "Admin", new DateTime(2026, 7, 14, 16, 9, 12, 496, DateTimeKind.Utc).AddTicks(9186) });

            migrationBuilder.CreateIndex(
                name: "ix_users_created_by_user_id",
                table: "users",
                column: "created_by_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_users_employee_id",
                table: "users",
                column: "employee_id",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "fk_users_users_created_by_user_id",
                table: "users",
                column: "created_by_user_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_users_users_created_by_user_id",
                table: "users");

            migrationBuilder.DropIndex(
                name: "ix_users_created_by_user_id",
                table: "users");

            migrationBuilder.DropIndex(
                name: "ix_users_employee_id",
                table: "users");

            migrationBuilder.DeleteData(
                table: "roles",
                keyColumn: "id",
                keyValue: 4L);

            migrationBuilder.DropColumn(
                name: "created_by_user_id",
                table: "users");

            migrationBuilder.DropColumn(
                name: "employee_id",
                table: "users");

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
    }
}
