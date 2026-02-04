using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HammamAPI.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddNomArabeToHammam : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "NomArabe",
                table: "hammam",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.UpdateData(
                table: "employe",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000001"),
                columns: new[] { "created_at", "password_hash" },
                values: new object[] { new DateTime(2026, 2, 4, 10, 8, 47, 512, DateTimeKind.Utc).AddTicks(9511), "$2a$11$4ZO0nW.k/ug3wRaRe2rzEeR5OX6zbsF/mchDAnCeAs7BooPq8C0x6" });

            migrationBuilder.UpdateData(
                table: "hammam",
                keyColumn: "id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111111"),
                columns: new[] { "created_at", "NomArabe" },
                values: new object[] { new DateTime(2026, 2, 4, 10, 8, 47, 393, DateTimeKind.Utc).AddTicks(8497), "" });

            migrationBuilder.UpdateData(
                table: "hammam",
                keyColumn: "id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222222"),
                columns: new[] { "created_at", "NomArabe" },
                values: new object[] { new DateTime(2026, 2, 4, 10, 8, 47, 393, DateTimeKind.Utc).AddTicks(8508), "" });

            migrationBuilder.UpdateData(
                table: "hammam",
                keyColumn: "id",
                keyValue: new Guid("33333333-3333-3333-3333-333333333333"),
                columns: new[] { "created_at", "NomArabe" },
                values: new object[] { new DateTime(2026, 2, 4, 10, 8, 47, 393, DateTimeKind.Utc).AddTicks(8511), "" });

            migrationBuilder.UpdateData(
                table: "hammam",
                keyColumn: "id",
                keyValue: new Guid("44444444-4444-4444-4444-444444444444"),
                columns: new[] { "created_at", "NomArabe" },
                values: new object[] { new DateTime(2026, 2, 4, 10, 8, 47, 393, DateTimeKind.Utc).AddTicks(8520), "" });

            migrationBuilder.UpdateData(
                table: "hammam",
                keyColumn: "id",
                keyValue: new Guid("55555555-5555-5555-5555-555555555555"),
                columns: new[] { "created_at", "NomArabe" },
                values: new object[] { new DateTime(2026, 2, 4, 10, 8, 47, 393, DateTimeKind.Utc).AddTicks(8524), "" });

            migrationBuilder.UpdateData(
                table: "hammam",
                keyColumn: "id",
                keyValue: new Guid("66666666-6666-6666-6666-666666666666"),
                columns: new[] { "created_at", "NomArabe" },
                values: new object[] { new DateTime(2026, 2, 4, 10, 8, 47, 393, DateTimeKind.Utc).AddTicks(8526), "" });

            migrationBuilder.UpdateData(
                table: "type_ticket",
                keyColumn: "id",
                keyValue: new Guid("aaaa1111-1111-1111-1111-111111111111"),
                column: "created_at",
                value: new DateTime(2026, 2, 4, 10, 8, 47, 393, DateTimeKind.Utc).AddTicks(8815));

            migrationBuilder.UpdateData(
                table: "type_ticket",
                keyColumn: "id",
                keyValue: new Guid("aaaa2222-2222-2222-2222-222222222222"),
                column: "created_at",
                value: new DateTime(2026, 2, 4, 10, 8, 47, 393, DateTimeKind.Utc).AddTicks(8824));

            migrationBuilder.UpdateData(
                table: "type_ticket",
                keyColumn: "id",
                keyValue: new Guid("aaaa3333-3333-3333-3333-333333333333"),
                column: "created_at",
                value: new DateTime(2026, 2, 4, 10, 8, 47, 393, DateTimeKind.Utc).AddTicks(8828));

            migrationBuilder.UpdateData(
                table: "type_ticket",
                keyColumn: "id",
                keyValue: new Guid("aaaa4444-4444-4444-4444-444444444444"),
                column: "created_at",
                value: new DateTime(2026, 2, 4, 10, 8, 47, 393, DateTimeKind.Utc).AddTicks(8836));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "NomArabe",
                table: "hammam");

            migrationBuilder.UpdateData(
                table: "employe",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000001"),
                columns: new[] { "created_at", "password_hash" },
                values: new object[] { new DateTime(2026, 2, 2, 14, 3, 22, 753, DateTimeKind.Utc).AddTicks(7481), "$2a$11$jTd8hZ9SL2AeRMDLV3vw6OV3AGC3xfAWI4ajPWepsYRND8CJsw.WS" });

            migrationBuilder.UpdateData(
                table: "hammam",
                keyColumn: "id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111111"),
                column: "created_at",
                value: new DateTime(2026, 2, 2, 14, 3, 22, 640, DateTimeKind.Utc).AddTicks(3800));

            migrationBuilder.UpdateData(
                table: "hammam",
                keyColumn: "id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222222"),
                column: "created_at",
                value: new DateTime(2026, 2, 2, 14, 3, 22, 640, DateTimeKind.Utc).AddTicks(3805));

            migrationBuilder.UpdateData(
                table: "hammam",
                keyColumn: "id",
                keyValue: new Guid("33333333-3333-3333-3333-333333333333"),
                column: "created_at",
                value: new DateTime(2026, 2, 2, 14, 3, 22, 640, DateTimeKind.Utc).AddTicks(3807));

            migrationBuilder.UpdateData(
                table: "hammam",
                keyColumn: "id",
                keyValue: new Guid("44444444-4444-4444-4444-444444444444"),
                column: "created_at",
                value: new DateTime(2026, 2, 2, 14, 3, 22, 640, DateTimeKind.Utc).AddTicks(3809));

            migrationBuilder.UpdateData(
                table: "hammam",
                keyColumn: "id",
                keyValue: new Guid("55555555-5555-5555-5555-555555555555"),
                column: "created_at",
                value: new DateTime(2026, 2, 2, 14, 3, 22, 640, DateTimeKind.Utc).AddTicks(3811));

            migrationBuilder.UpdateData(
                table: "hammam",
                keyColumn: "id",
                keyValue: new Guid("66666666-6666-6666-6666-666666666666"),
                column: "created_at",
                value: new DateTime(2026, 2, 2, 14, 3, 22, 640, DateTimeKind.Utc).AddTicks(3812));

            migrationBuilder.UpdateData(
                table: "type_ticket",
                keyColumn: "id",
                keyValue: new Guid("aaaa1111-1111-1111-1111-111111111111"),
                column: "created_at",
                value: new DateTime(2026, 2, 2, 14, 3, 22, 640, DateTimeKind.Utc).AddTicks(3944));

            migrationBuilder.UpdateData(
                table: "type_ticket",
                keyColumn: "id",
                keyValue: new Guid("aaaa2222-2222-2222-2222-222222222222"),
                column: "created_at",
                value: new DateTime(2026, 2, 2, 14, 3, 22, 640, DateTimeKind.Utc).AddTicks(3948));

            migrationBuilder.UpdateData(
                table: "type_ticket",
                keyColumn: "id",
                keyValue: new Guid("aaaa3333-3333-3333-3333-333333333333"),
                column: "created_at",
                value: new DateTime(2026, 2, 2, 14, 3, 22, 640, DateTimeKind.Utc).AddTicks(3951));

            migrationBuilder.UpdateData(
                table: "type_ticket",
                keyColumn: "id",
                keyValue: new Guid("aaaa4444-4444-4444-4444-444444444444"),
                column: "created_at",
                value: new DateTime(2026, 2, 2, 14, 3, 22, 640, DateTimeKind.Utc).AddTicks(3953));
        }
    }
}
