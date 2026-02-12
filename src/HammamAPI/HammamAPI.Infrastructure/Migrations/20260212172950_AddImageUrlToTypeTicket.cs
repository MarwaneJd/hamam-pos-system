using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HammamAPI.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddImageUrlToTypeTicket : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                DO $$ BEGIN
                    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='type_ticket' AND column_name='image_url') THEN
                        ALTER TABLE type_ticket ADD COLUMN image_url character varying(500);
                    END IF;
                END $$;
            ");

            migrationBuilder.UpdateData(
                table: "employe",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000001"),
                columns: new[] { "created_at", "password_hash" },
                values: new object[] { new DateTime(2026, 2, 12, 17, 29, 50, 566, DateTimeKind.Utc).AddTicks(5430), "$2a$11$vvAB/wgZJCbcM1q7AommdumSxE6NVsGFsgfNGYWSg21RH3zlDkRgq" });

            migrationBuilder.UpdateData(
                table: "hammam",
                keyColumn: "id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111111"),
                column: "created_at",
                value: new DateTime(2026, 2, 12, 17, 29, 50, 452, DateTimeKind.Utc).AddTicks(9109));

            migrationBuilder.UpdateData(
                table: "hammam",
                keyColumn: "id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222222"),
                column: "created_at",
                value: new DateTime(2026, 2, 12, 17, 29, 50, 452, DateTimeKind.Utc).AddTicks(9113));

            migrationBuilder.UpdateData(
                table: "hammam",
                keyColumn: "id",
                keyValue: new Guid("33333333-3333-3333-3333-333333333333"),
                column: "created_at",
                value: new DateTime(2026, 2, 12, 17, 29, 50, 452, DateTimeKind.Utc).AddTicks(9116));

            migrationBuilder.UpdateData(
                table: "hammam",
                keyColumn: "id",
                keyValue: new Guid("44444444-4444-4444-4444-444444444444"),
                column: "created_at",
                value: new DateTime(2026, 2, 12, 17, 29, 50, 452, DateTimeKind.Utc).AddTicks(9120));

            migrationBuilder.UpdateData(
                table: "hammam",
                keyColumn: "id",
                keyValue: new Guid("55555555-5555-5555-5555-555555555555"),
                column: "created_at",
                value: new DateTime(2026, 2, 12, 17, 29, 50, 452, DateTimeKind.Utc).AddTicks(9122));

            migrationBuilder.UpdateData(
                table: "hammam",
                keyColumn: "id",
                keyValue: new Guid("66666666-6666-6666-6666-666666666666"),
                column: "created_at",
                value: new DateTime(2026, 2, 12, 17, 29, 50, 452, DateTimeKind.Utc).AddTicks(9124));

            migrationBuilder.UpdateData(
                table: "type_ticket",
                keyColumn: "id",
                keyValue: new Guid("aaaa1111-1111-1111-1111-111111111111"),
                columns: new[] { "created_at", "image_url" },
                values: new object[] { new DateTime(2026, 2, 12, 17, 29, 50, 452, DateTimeKind.Utc).AddTicks(9263), null });

            migrationBuilder.UpdateData(
                table: "type_ticket",
                keyColumn: "id",
                keyValue: new Guid("aaaa2222-2222-2222-2222-222222222222"),
                columns: new[] { "created_at", "image_url" },
                values: new object[] { new DateTime(2026, 2, 12, 17, 29, 50, 452, DateTimeKind.Utc).AddTicks(9267), null });

            migrationBuilder.UpdateData(
                table: "type_ticket",
                keyColumn: "id",
                keyValue: new Guid("aaaa3333-3333-3333-3333-333333333333"),
                columns: new[] { "created_at", "image_url" },
                values: new object[] { new DateTime(2026, 2, 12, 17, 29, 50, 452, DateTimeKind.Utc).AddTicks(9271), null });

            migrationBuilder.UpdateData(
                table: "type_ticket",
                keyColumn: "id",
                keyValue: new Guid("aaaa4444-4444-4444-4444-444444444444"),
                columns: new[] { "created_at", "image_url" },
                values: new object[] { new DateTime(2026, 2, 12, 17, 29, 50, 452, DateTimeKind.Utc).AddTicks(9273), null });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "image_url",
                table: "type_ticket");

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
        }
    }
}
