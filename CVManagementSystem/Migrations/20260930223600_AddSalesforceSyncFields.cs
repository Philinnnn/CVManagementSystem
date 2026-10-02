using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CVManagementSystem.Migrations
{
    /// <inheritdoc />
    public partial class AddSalesforceSyncFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SalesforceContactId",
                table: "Users",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "SalesforceSyncedAt",
                table: "Users",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SalesforceContactId",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "SalesforceSyncedAt",
                table: "Users");
        }
    }
}
