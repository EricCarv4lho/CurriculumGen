using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CurriculumGenerator.Migrations
{
    /// <inheritdoc />
    public partial class AddPdfData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<byte[]>(
                name: "PdfData",
                table: "Resumes",
                type: "bytea",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "ResumeTemplates",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 26, 0, 31, 2, 953, DateTimeKind.Utc).AddTicks(3845));

            migrationBuilder.UpdateData(
                table: "ResumeTemplates",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 26, 0, 31, 2, 953, DateTimeKind.Utc).AddTicks(3848));

            migrationBuilder.UpdateData(
                table: "ResumeTemplates",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 26, 0, 31, 2, 953, DateTimeKind.Utc).AddTicks(3851));

            migrationBuilder.UpdateData(
                table: "ResumeTemplates",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 26, 0, 31, 2, 953, DateTimeKind.Utc).AddTicks(3853));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PdfData",
                table: "Resumes");

            migrationBuilder.UpdateData(
                table: "ResumeTemplates",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 24, 1, 17, 6, 211, DateTimeKind.Utc).AddTicks(6408));

            migrationBuilder.UpdateData(
                table: "ResumeTemplates",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 24, 1, 17, 6, 211, DateTimeKind.Utc).AddTicks(6417));

            migrationBuilder.UpdateData(
                table: "ResumeTemplates",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 24, 1, 17, 6, 211, DateTimeKind.Utc).AddTicks(6424));

            migrationBuilder.UpdateData(
                table: "ResumeTemplates",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 24, 1, 17, 6, 211, DateTimeKind.Utc).AddTicks(6430));
        }
    }
}
