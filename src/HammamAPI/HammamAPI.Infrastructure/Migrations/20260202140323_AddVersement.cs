using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HammamAPI.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddVersement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "versement",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    employe_id = table.Column<Guid>(type: "uuid", nullable: false),
                    hammam_id = table.Column<Guid>(type: "uuid", nullable: false),
                    date_versement = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    montant_theorique = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    montant_remis = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    ecart = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    nombre_tickets = table.Column<int>(type: "integer", nullable: false),
                    commentaire = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    valide_par = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_versement", x => x.id);
                    table.ForeignKey(
                        name: "FK_versement_employe_employe_id",
                        column: x => x.employe_id,
                        principalTable: "employe",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_versement_hammam_hammam_id",
                        column: x => x.hammam_id,
                        principalTable: "hammam",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

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

            migrationBuilder.CreateIndex(
                name: "IX_versement_date_versement",
                table: "versement",
                column: "date_versement");

            migrationBuilder.CreateIndex(
                name: "IX_versement_employe_id",
                table: "versement",
                column: "employe_id");

            migrationBuilder.CreateIndex(
                name: "IX_versement_employe_id_date_versement",
                table: "versement",
                columns: new[] { "employe_id", "date_versement" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_versement_hammam_id",
                table: "versement",
                column: "hammam_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "versement");

            migrationBuilder.UpdateData(
                table: "employe",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000001"),
                columns: new[] { "created_at", "password_hash" },
                values: new object[] { new DateTime(2026, 2, 2, 12, 39, 19, 393, DateTimeKind.Utc).AddTicks(1182), "$2a$11$H2Vd5ReuNWLeRDPkjRyEJuLEDTF02/Nb/aaTm4he.En.xn.3KHPda" });

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
    }
}
