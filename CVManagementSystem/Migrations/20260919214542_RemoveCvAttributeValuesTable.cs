using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace CVManagementSystem.Migrations
{
    /// <inheritdoc />
    public partial class RemoveCvAttributeValuesTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CvAttributeValues");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CvAttributeValues",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    AttributeId = table.Column<int>(type: "integer", nullable: false),
                    CvId = table.Column<int>(type: "integer", nullable: false),
                    BooleanValue = table.Column<bool>(type: "boolean", nullable: true),
                    DateRangeEnd = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DateRangeStart = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DateValue = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    NumericValue = table.Column<double>(type: "double precision", nullable: true),
                    TextValue = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CvAttributeValues", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CvAttributeValues_Attributes_AttributeId",
                        column: x => x.AttributeId,
                        principalTable: "Attributes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CvAttributeValues_Cvs_CvId",
                        column: x => x.CvId,
                        principalTable: "Cvs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CvAttributeValues_AttributeId",
                table: "CvAttributeValues",
                column: "AttributeId");

            migrationBuilder.CreateIndex(
                name: "IX_CvAttributeValues_CvId",
                table: "CvAttributeValues",
                column: "CvId");
        }
    }
}
