using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HammamAPI.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPrefixeTicketToHammam : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PrefixeTicket",
                table: "hammam",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.UpdateData(
                table: "employe",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000001"),
                columns: new[] { "created_at", "password_hash" },
                values: new object[] { new DateTime(2026, 2, 4, 10, 57, 35, 981, DateTimeKind.Utc).AddTicks(4181), "$2a$11$sf3Bv.Uwbp/HAfcxEDlu6eJXF0ts6mWlcn9fY61hW35KYADY0akwi" });

            migrationBuilder.UpdateData(
                table: "hammam",
                keyColumn: "id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111111"),
                columns: new[] { "created_at", "PrefixeTicket" },
                values: new object[] { new DateTime(2026, 2, 4, 10, 57, 35, 871, DateTimeKind.Utc).AddTicks(8764), 100000 });

            migrationBuilder.UpdateData(
                table: "hammam",
                keyColumn: "id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222222"),
                columns: new[] { "created_at", "PrefixeTicket" },
                values: new object[] { new DateTime(2026, 2, 4, 10, 57, 35, 871, DateTimeKind.Utc).AddTicks(8769), 100000 });

            migrationBuilder.UpdateData(
                table: "hammam",
                keyColumn: "id",
                keyValue: new Guid("33333333-3333-3333-3333-333333333333"),
                columns: new[] { "created_at", "PrefixeTicket" },
                values: new object[] { new DateTime(2026, 2, 4, 10, 57, 35, 871, DateTimeKind.Utc).AddTicks(8772), 100000 });

            migrationBuilder.UpdateData(
                table: "hammam",
                keyColumn: "id",
                keyValue: new Guid("44444444-4444-4444-4444-444444444444"),
                columns: new[] { "created_at", "PrefixeTicket" },
                values: new object[] { new DateTime(2026, 2, 4, 10, 57, 35, 871, DateTimeKind.Utc).AddTicks(8774), 100000 });

            migrationBuilder.UpdateData(
                table: "hammam",
                keyColumn: "id",
                keyValue: new Guid("55555555-5555-5555-5555-555555555555"),
                columns: new[] { "created_at", "PrefixeTicket" },
                values: new object[] { new DateTime(2026, 2, 4, 10, 57, 35, 871, DateTimeKind.Utc).AddTicks(8776), 100000 });

            migrationBuilder.UpdateData(
                table: "hammam",
                keyColumn: "id",
                keyValue: new Guid("66666666-6666-6666-6666-666666666666"),
                columns: new[] { "created_at", "PrefixeTicket" },
                values: new object[] { new DateTime(2026, 2, 4, 10, 57, 35, 871, DateTimeKind.Utc).AddTicks(8777), 100000 });

            migrationBuilder.UpdateData(
                table: "type_ticket",
                keyColumn: "id",
                keyValue: new Guid("aaaa1111-1111-1111-1111-111111111111"),
                column: "created_at",
                value: new DateTime(2026, 2, 4, 10, 57, 35, 871, DateTimeKind.Utc).AddTicks(8912));

            migrationBuilder.UpdateData(
                table: "type_ticket",
                keyColumn: "id",
                keyValue: new Guid("aaaa2222-2222-2222-2222-222222222222"),
                column: "created_at",
                value: new DateTime(2026, 2, 4, 10, 57, 35, 871, DateTimeKind.Utc).AddTicks(8915));

            migrationBuilder.UpdateData(
                table: "type_ticket",
                keyColumn: "id",
                keyValue: new Guid("aaaa3333-3333-3333-3333-333333333333"),
                column: "created_at",
                value: new DateTime(2026, 2, 4, 10, 57, 35, 871, DateTimeKind.Utc).AddTicks(8921));

            migrationBuilder.UpdateData(
                table: "type_ticket",
                keyColumn: "id",
                keyValue: new Guid("aaaa4444-4444-4444-4444-444444444444"),
                column: "created_at",
                value: new DateTime(2026, 2, 4, 10, 57, 35, 871, DateTimeKind.Utc).AddTicks(8923));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PrefixeTicket",
                table: "hammam");

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
                column: "created_at",
                value: new DateTime(2026, 2, 4, 10, 8, 47, 393, DateTimeKind.Utc).AddTicks(8497));

            migrationBuilder.UpdateData(
                table: "hammam",
                keyColumn: "id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222222"),
                column: "created_at",
                value: new DateTime(2026, 2, 4, 10, 8, 47, 393, DateTimeKind.Utc).AddTicks(8508));

            migrationBuilder.UpdateData(
                table: "hammam",
                keyColumn: "id",
                keyValue: new Guid("33333333-3333-3333-3333-333333333333"),
                column: "created_at",
                value: new DateTime(2026, 2, 4, 10, 8, 47, 393, DateTimeKind.Utc).AddTicks(8511));

            migrationBuilder.UpdateData(
                table: "hammam",
                keyColumn: "id",
                keyValue: new Guid("44444444-4444-4444-4444-444444444444"),
                column: "created_at",
                value: new DateTime(2026, 2, 4, 10, 8, 47, 393, DateTimeKind.Utc).AddTicks(8520));

            migrationBuilder.UpdateData(
                table: "hammam",
                keyColumn: "id",
                keyValue: new Guid("55555555-5555-5555-5555-555555555555"),
                column: "created_at",
                value: new DateTime(2026, 2, 4, 10, 8, 47, 393, DateTimeKind.Utc).AddTicks(8524));

            migrationBuilder.UpdateData(
                table: "hammam",
                keyColumn: "id",
                keyValue: new Guid("66666666-6666-6666-6666-666666666666"),
                column: "created_at",
                value: new DateTime(2026, 2, 4, 10, 8, 47, 393, DateTimeKind.Utc).AddTicks(8526));

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
    }
}
