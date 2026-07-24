using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using DeskGuardBackend.Data;

#nullable disable

namespace DeskGuardBackend.Migrations
{
    [DbContext(typeof(DeskGuardDbContext))]
    [Migration("20260722183000_AddCustomerIdToCompany")]
    public partial class AddCustomerIdToCompany : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("ALTER TABLE companies ADD COLUMN IF NOT EXISTS customer_id character varying(100);");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "customer_id",
                table: "companies");
        }
    }
}
