using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HammamAPI.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RemoveUsernameUniqueConstraint : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_employe_username",
                table: "employe");

            migrationBuilder.UpdateData(
                table: "employe",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000001"),
                columns: new[] { "created_at", "password_hash" },
                values: new object[] { new DateTime(2026, 2, 4, 12, 26, 44, 841, DateTimeKind.Utc).AddTicks(9010), "$2a$11$XFBynzbd/KDsj3ak53drlOz4ImWaAe1NgY.A6JrMWx2r3EU0.cKbi" });

            migrationBuilder.UpdateData(
                table: "hammam",
                keyColumn: "id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111111"),
                column: "created_at",
                value: new DateTime(2026, 2, 4, 12, 26, 44, 719, DateTimeKind.Utc).AddTicks(166));

            migrationBuilder.UpdateData(
                table: "hammam",
                keyColumn: "id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222222"),
                column: "created_at",
                value: new DateTime(2026, 2, 4, 12, 26, 44, 719, DateTimeKind.Utc).AddTicks(171));

            migrationBuilder.UpdateData(
                table: "hammam",
                keyColumn: "id",
                keyValue: new Guid("33333333-3333-3333-3333-333333333333"),
                column: "created_at",
                value: new DateTime(2026, 2, 4, 12, 26, 44, 719, DateTimeKind.Utc).AddTicks(173));

            migrationBuilder.UpdateData(
                table: "hammam",
                keyColumn: "id",
                keyValue: new Guid("44444444-4444-4444-4444-444444444444"),
                column: "created_at",
                value: new DateTime(2026, 2, 4, 12, 26, 44, 719, DateTimeKind.Utc).AddTicks(175));

            migrationBuilder.UpdateData(
                table: "hammam",
                keyColumn: "id",
                keyValue: new Guid("55555555-5555-5555-5555-555555555555"),
                column: "created_at",
                value: new DateTime(2026, 2, 4, 12, 26, 44, 719, DateTimeKind.Utc).AddTicks(177));

            migrationBuilder.UpdateData(
                table: "hammam",
                keyColumn: "id",
                keyValue: new Guid("66666666-6666-6666-6666-666666666666"),
                column: "created_at",
                value: new DateTime(2026, 2, 4, 12, 26, 44, 719, DateTimeKind.Utc).AddTicks(177));

            migrationBuilder.UpdateData(
                table: "type_ticket",
                keyColumn: "id",
                keyValue: new Guid("aaaa1111-1111-1111-1111-111111111111"),
                column: "created_at",
                value: new DateTime(2026, 2, 4, 12, 26, 44, 719, DateTimeKind.Utc).AddTicks(320));

            migrationBuilder.UpdateData(
                table: "type_ticket",
                keyColumn: "id",
                keyValue: new Guid("aaaa2222-2222-2222-2222-222222222222"),
                column: "created_at",
                value: new DateTime(2026, 2, 4, 12, 26, 44, 719, DateTimeKind.Utc).AddTicks(323));

            migrationBuilder.UpdateData(
                table: "type_ticket",
                keyColumn: "id",
                keyValue: new Guid("aaaa3333-3333-3333-3333-333333333333"),
                column: "created_at",
                value: new DateTime(2026, 2, 4, 12, 26, 44, 719, DateTimeKind.Utc).AddTicks(326));

            migrationBuilder.UpdateData(
                table: "type_ticket",
                keyColumn: "id",
                keyValue: new Guid("aaaa4444-4444-4444-4444-444444444444"),
                column: "created_at",
                value: new DateTime(2026, 2, 4, 12, 26, 44, 719, DateTimeKind.Utc).AddTicks(329));

            migrationBuilder.CreateIndex(
                name: "IX_employe_username",
                table: "employe",
                column: "username");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_employe_username",
                table: "employe");

            migrationBuilder.UpdateData(
                table: "employe",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000001"),
                columns: new[] { "created_at", "password_hash" },
                values: new object[] { new DateTime(2026, 2, 4, 11, 30, 33, 358, DateTimeKind.Utc).AddTicks(9714), "$2a$11$rM0YsIXyLvgH35KRzH7QLe6iDU/xIm7EPmhOkpVd1cwfN5UAsfhRu" });

            migrationBuilder.UpdateData(
                table: "hammam",
                keyColumn: "id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111111"),
                column: "created_at",
                value: new DateTime(2026, 2, 4, 11, 30, 33, 252, DateTimeKind.Utc).AddTicks(2534));

            migrationBuilder.UpdateData(
                table: "hammam",
                keyColumn: "id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222222"),
                column: "created_at",
                value: new DateTime(2026, 2, 4, 11, 30, 33, 252, DateTimeKind.Utc).AddTicks(2538));

            migrationBuilder.UpdateData(
                table: "hammam",
                keyColumn: "id",
                keyValue: new Guid("33333333-3333-3333-3333-333333333333"),
                column: "created_at",
                value: new DateTime(2026, 2, 4, 11, 30, 33, 252, DateTimeKind.Utc).AddTicks(2540));

            migrationBuilder.UpdateData(
                table: "hammam",
                keyColumn: "id",
                keyValue: new Guid("44444444-4444-4444-4444-444444444444"),
                column: "created_at",
                value: new DateTime(2026, 2, 4, 11, 30, 33, 252, DateTimeKind.Utc).AddTicks(2542));

            migrationBuilder.UpdateData(
                table: "hammam",
                keyColumn: "id",
                keyValue: new Guid("55555555-5555-5555-5555-555555555555"),
                column: "created_at",
                value: new DateTime(2026, 2, 4, 11, 30, 33, 252, DateTimeKind.Utc).AddTicks(2544));

            migrationBuilder.UpdateData(
                table: "hammam",
                keyColumn: "id",
                keyValue: new Guid("66666666-6666-6666-6666-666666666666"),
                column: "created_at",
                value: new DateTime(2026, 2, 4, 11, 30, 33, 252, DateTimeKind.Utc).AddTicks(2546));

            migrationBuilder.UpdateData(
                table: "type_ticket",
                keyColumn: "id",
                keyValue: new Guid("aaaa1111-1111-1111-1111-111111111111"),
                column: "created_at",
                value: new DateTime(2026, 2, 4, 11, 30, 33, 252, DateTimeKind.Utc).AddTicks(2761));

            migrationBuilder.UpdateData(
                table: "type_ticket",
                keyColumn: "id",
                keyValue: new Guid("aaaa2222-2222-2222-2222-222222222222"),
                column: "created_at",
                value: new DateTime(2026, 2, 4, 11, 30, 33, 252, DateTimeKind.Utc).AddTicks(2765));

            migrationBuilder.UpdateData(
                table: "type_ticket",
                keyColumn: "id",
                keyValue: new Guid("aaaa3333-3333-3333-3333-333333333333"),
                column: "created_at",
                value: new DateTime(2026, 2, 4, 11, 30, 33, 252, DateTimeKind.Utc).AddTicks(2768));

            migrationBuilder.UpdateData(
                table: "type_ticket",
                keyColumn: "id",
                keyValue: new Guid("aaaa4444-4444-4444-4444-444444444444"),
                column: "created_at",
                value: new DateTime(2026, 2, 4, 11, 30, 33, 252, DateTimeKind.Utc).AddTicks(2770));

            migrationBuilder.CreateIndex(
                name: "IX_employe_username",
                table: "employe",
                column: "username",
                unique: true);
        }
    }
}
