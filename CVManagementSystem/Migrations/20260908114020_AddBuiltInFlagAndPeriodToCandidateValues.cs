using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CVManagementSystem.Migrations
{
    /// <inheritdoc />
    public partial class AddBuiltInFlagAndPeriodToCandidateValues : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "DateRangeEnd",
                table: "CandidateAttributeValues",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DateRangeStart",
                table: "CandidateAttributeValues",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsBuiltIn",
                table: "Attributes",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DateRangeEnd",
                table: "CandidateAttributeValues");

            migrationBuilder.DropColumn(
                name: "DateRangeStart",
                table: "CandidateAttributeValues");

            migrationBuilder.DropColumn(
                name: "IsBuiltIn",
                table: "Attributes");
        }
    }
}
