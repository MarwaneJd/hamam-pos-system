using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HammamAPI.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPasswordClairToEmploye : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PasswordClair",
                table: "employe",
                type: "text",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "employe",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000001"),
                columns: new[] { "created_at", "PasswordClair", "password_hash" },
                values: new object[] { new DateTime(2026, 2, 2, 12, 39, 19, 393, DateTimeKind.Utc).AddTicks(1182), null, "$2a$11$H2Vd5ReuNWLeRDPkjRyEJuLEDTF02/Nb/aaTm4he.En.xn.3KHPda" });

            migrationBuilder.UpdateData(
                table: "hammam",
                keyColumn: "id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111111"),
                column: "created_at",
                value: new DateTime(2026, 2, 2, 12, 39, 19, 269, DateTimeKind.Utc).AddTicks(4952));

            migrationBuilder.UpdateData(
                table: "hammam",
                keyColumn: "id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222222"),
                column: "created_at",
                value: new DateTime(2026, 2, 2, 12, 39, 19, 269, DateTimeKind.Utc).AddTicks(4969));

            migrationBuilder.UpdateData(
                table: "hammam",
                keyColumn: "id",
                keyValue: new Guid("33333333-3333-3333-3333-333333333333"),
                column: "created_at",
                value: new DateTime(2026, 2, 2, 12, 39, 19, 269, DateTimeKind.Utc).AddTicks(4971));

            migrationBuilder.UpdateData(
                table: "hammam",
                keyColumn: "id",
                keyValue: new Guid("44444444-4444-4444-4444-444444444444"),
                column: "created_at",
                value: new DateTime(2026, 2, 2, 12, 39, 19, 269, DateTimeKind.Utc).AddTicks(4973));

            migrationBuilder.UpdateData(
                table: "hammam",
                keyColumn: "id",
                keyValue: new Guid("55555555-5555-5555-5555-555555555555"),
                column: "created_at",
                value: new DateTime(2026, 2, 2, 12, 39, 19, 269, DateTimeKind.Utc).AddTicks(4978));

            migrationBuilder.UpdateData(
                table: "hammam",
                keyColumn: "id",
                keyValue: new Guid("66666666-6666-6666-6666-666666666666"),
                column: "created_at",
                value: new DateTime(2026, 2, 2, 12, 39, 19, 269, DateTimeKind.Utc).AddTicks(4993));

            migrationBuilder.UpdateData(
                table: "type_ticket",
                keyColumn: "id",
                keyValue: new Guid("aaaa1111-1111-1111-1111-111111111111"),
                column: "created_at",
                value: new DateTime(2026, 2, 2, 12, 39, 19, 269, DateTimeKind.Utc).AddTicks(5358));

            migrationBuilder.UpdateData(
                table: "type_ticket",
                keyColumn: "id",
                keyValue: new Guid("aaaa2222-2222-2222-2222-222222222222"),
                column: "created_at",
                value: new DateTime(2026, 2, 2, 12, 39, 19, 269, DateTimeKind.Utc).AddTicks(5364));

            migrationBuilder.UpdateData(
                table: "type_ticket",
                keyColumn: "id",
                keyValue: new Guid("aaaa3333-3333-3333-3333-333333333333"),
                column: "created_at",
                value: new DateTime(2026, 2, 2, 12, 39, 19, 269, DateTimeKind.Utc).AddTicks(5368));

            migrationBuilder.UpdateData(
                table: "type_ticket",
                keyColumn: "id",
                keyValue: new Guid("aaaa4444-4444-4444-4444-444444444444"),
                column: "created_at",
                value: new DateTime(2026, 2, 2, 12, 39, 19, 269, DateTimeKind.Utc).AddTicks(5377));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PasswordClair",
                table: "employe");

            migrationBuilder.UpdateData(
                table: "employe",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000001"),
                columns: new[] { "created_at", "password_hash" },
                values: new object[] { new DateTime(2026, 2, 2, 11, 52, 11, 866, DateTimeKind.Utc).AddTicks(4160), "$2a$11$0xy3iH6SLgD9mRUfgilm/e4fI/IhNZogdajs1eBlAM3eT089MIVjG" });

            migrationBuilder.UpdateData(
                table: "hammam",
                keyColumn: "id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111111"),
                column: "created_at",
                value: new DateTime(2026, 2, 2, 11, 52, 11, 728, DateTimeKind.Utc).AddTicks(8853));

            migrationBuilder.UpdateData(
                table: "hammam",
                keyColumn: "id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222222"),
                column: "created_at",
                value: new DateTime(2026, 2, 2, 11, 52, 11, 728, DateTimeKind.Utc).AddTicks(8860));

            migrationBuilder.UpdateData(
                table: "hammam",
                keyColumn: "id",
                keyValue: new Guid("33333333-3333-3333-3333-333333333333"),
                column: "created_at",
                value: new DateTime(2026, 2, 2, 11, 52, 11, 728, DateTimeKind.Utc).AddTicks(8863));

            migrationBuilder.UpdateData(
                table: "hammam",
                keyColumn: "id",
                keyValue: new Guid("44444444-4444-4444-4444-444444444444"),
                column: "created_at",
                value: new DateTime(2026, 2, 2, 11, 52, 11, 728, DateTimeKind.Utc).AddTicks(8865));

            migrationBuilder.UpdateData(
                table: "hammam",
                keyColumn: "id",
                keyValue: new Guid("55555555-5555-5555-5555-555555555555"),
                column: "created_at",
                value: new DateTime(2026, 2, 2, 11, 52, 11, 728, DateTimeKind.Utc).AddTicks(8867));

            migrationBuilder.UpdateData(
                table: "hammam",
                keyColumn: "id",
                keyValue: new Guid("66666666-6666-6666-6666-666666666666"),
                column: "created_at",
                value: new DateTime(2026, 2, 2, 11, 52, 11, 728, DateTimeKind.Utc).AddTicks(8869));

            migrationBuilder.UpdateData(
                table: "type_ticket",
                keyColumn: "id",
                keyValue: new Guid("aaaa1111-1111-1111-1111-111111111111"),
                column: "created_at",
                value: new DateTime(2026, 2, 2, 11, 52, 11, 728, DateTimeKind.Utc).AddTicks(9143));

            migrationBuilder.UpdateData(
                table: "type_ticket",
                keyColumn: "id",
                keyValue: new Guid("aaaa2222-2222-2222-2222-222222222222"),
                column: "created_at",
                value: new DateTime(2026, 2, 2, 11, 52, 11, 728, DateTimeKind.Utc).AddTicks(9147));

            migrationBuilder.UpdateData(
                table: "type_ticket",
                keyColumn: "id",
                keyValue: new Guid("aaaa3333-3333-3333-3333-333333333333"),
                column: "created_at",
                value: new DateTime(2026, 2, 2, 11, 52, 11, 728, DateTimeKind.Utc).AddTicks(9154));

            migrationBuilder.UpdateData(
                table: "type_ticket",
                keyColumn: "id",
                keyValue: new Guid("aaaa4444-4444-4444-4444-444444444444"),
                column: "created_at",
                value: new DateTime(2026, 2, 2, 11, 52, 11, 728, DateTimeKind.Utc).AddTicks(9157));
        }
    }
}
