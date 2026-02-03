using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace HammamAPI.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "hammam",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    nom = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    adresse = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    actif = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_hammam", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "employe",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    username = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    password_hash = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    nom = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    prenom = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    hammam_id = table.Column<Guid>(type: "uuid", nullable: false),
                    langue = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: false, defaultValue: "FR"),
                    role = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    actif = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    last_login_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_employe", x => x.id);
                    table.ForeignKey(
                        name: "FK_employe_hammam_hammam_id",
                        column: x => x.hammam_id,
                        principalTable: "hammam",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "type_ticket",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    nom = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    prix = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    couleur = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    icone = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValue: "User"),
                    ordre = table.Column<int>(type: "integer", nullable: false),
                    actif = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    hammam_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_type_ticket", x => x.id);
                    table.ForeignKey(
                        name: "FK_type_ticket_hammam_hammam_id",
                        column: x => x.hammam_id,
                        principalTable: "hammam",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ticket",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    type_ticket_id = table.Column<Guid>(type: "uuid", nullable: false),
                    employe_id = table.Column<Guid>(type: "uuid", nullable: false),
                    hammam_id = table.Column<Guid>(type: "uuid", nullable: false),
                    prix = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    synced_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    sync_status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    device_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ticket", x => x.id);
                    table.ForeignKey(
                        name: "FK_ticket_employe_employe_id",
                        column: x => x.employe_id,
                        principalTable: "employe",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ticket_hammam_hammam_id",
                        column: x => x.hammam_id,
                        principalTable: "hammam",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ticket_type_ticket_type_ticket_id",
                        column: x => x.type_ticket_id,
                        principalTable: "type_ticket",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "hammam",
                columns: new[] { "id", "actif", "adresse", "code", "created_at", "nom", "updated_at" },
                values: new object[,]
                {
                    { new Guid("11111111-1111-1111-1111-111111111111"), true, "123 Rue Principale, Casablanca", "HAM001", new DateTime(2026, 2, 2, 11, 52, 11, 728, DateTimeKind.Utc).AddTicks(8853), "Hammam Centre", null },
                    { new Guid("22222222-2222-2222-2222-222222222222"), true, "45 Boulevard Anfa, Casablanca", "HAM002", new DateTime(2026, 2, 2, 11, 52, 11, 728, DateTimeKind.Utc).AddTicks(8860), "Hammam Anfa", null },
                    { new Guid("33333333-3333-3333-3333-333333333333"), true, "78 Rue Maarif, Casablanca", "HAM003", new DateTime(2026, 2, 2, 11, 52, 11, 728, DateTimeKind.Utc).AddTicks(8863), "Hammam Maarif", null },
                    { new Guid("44444444-4444-4444-4444-444444444444"), true, "12 Avenue Hassan II, Casablanca", "HAM004", new DateTime(2026, 2, 2, 11, 52, 11, 728, DateTimeKind.Utc).AddTicks(8865), "Hammam Hay Mohammadi", null },
                    { new Guid("55555555-5555-5555-5555-555555555555"), true, "90 Derb Sultan, Casablanca", "HAM005", new DateTime(2026, 2, 2, 11, 52, 11, 728, DateTimeKind.Utc).AddTicks(8867), "Hammam Derb Sultan", null }
                });

            migrationBuilder.InsertData(
                table: "hammam",
                columns: new[] { "id", "adresse", "code", "created_at", "nom", "updated_at" },
                values: new object[] { new Guid("66666666-6666-6666-6666-666666666666"), "34 Quartier Sidi Moumen, Casablanca", "HAM006", new DateTime(2026, 2, 2, 11, 52, 11, 728, DateTimeKind.Utc).AddTicks(8869), "Hammam Sidi Moumen", null });

            migrationBuilder.InsertData(
                table: "type_ticket",
                columns: new[] { "id", "actif", "couleur", "created_at", "hammam_id", "icone", "nom", "ordre", "prix" },
                values: new object[,]
                {
                    { new Guid("aaaa1111-1111-1111-1111-111111111111"), true, "#3B82F6", new DateTime(2026, 2, 2, 11, 52, 11, 728, DateTimeKind.Utc).AddTicks(9143), null, "User", "HOMME", 1, 15.00m },
                    { new Guid("aaaa2222-2222-2222-2222-222222222222"), true, "#EC4899", new DateTime(2026, 2, 2, 11, 52, 11, 728, DateTimeKind.Utc).AddTicks(9147), null, "UserCheck", "FEMME", 2, 15.00m },
                    { new Guid("aaaa3333-3333-3333-3333-333333333333"), true, "#10B981", new DateTime(2026, 2, 2, 11, 52, 11, 728, DateTimeKind.Utc).AddTicks(9154), null, "Baby", "ENFANT", 3, 10.00m },
                    { new Guid("aaaa4444-4444-4444-4444-444444444444"), true, "#06B6D4", new DateTime(2026, 2, 2, 11, 52, 11, 728, DateTimeKind.Utc).AddTicks(9157), null, "Droplets", "DOUCHE", 4, 8.00m }
                });

            migrationBuilder.InsertData(
                table: "employe",
                columns: new[] { "id", "actif", "created_at", "hammam_id", "langue", "last_login_at", "nom", "password_hash", "prenom", "role", "updated_at", "username" },
                values: new object[] { new Guid("00000000-0000-0000-0000-000000000001"), true, new DateTime(2026, 2, 2, 11, 52, 11, 866, DateTimeKind.Utc).AddTicks(4160), new Guid("11111111-1111-1111-1111-111111111111"), "FR", null, "Administrateur", "$2a$11$0xy3iH6SLgD9mRUfgilm/e4fI/IhNZogdajs1eBlAM3eT089MIVjG", "System", "Admin", null, "admin" });

            migrationBuilder.CreateIndex(
                name: "IX_employe_hammam_id",
                table: "employe",
                column: "hammam_id");

            migrationBuilder.CreateIndex(
                name: "IX_employe_username",
                table: "employe",
                column: "username",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_hammam_code",
                table: "hammam",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ticket_created_at",
                table: "ticket",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "IX_ticket_employe_id",
                table: "ticket",
                column: "employe_id");

            migrationBuilder.CreateIndex(
                name: "IX_ticket_employe_id_created_at",
                table: "ticket",
                columns: new[] { "employe_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "IX_ticket_hammam_id",
                table: "ticket",
                column: "hammam_id");

            migrationBuilder.CreateIndex(
                name: "IX_ticket_hammam_id_created_at",
                table: "ticket",
                columns: new[] { "hammam_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "IX_ticket_type_ticket_id",
                table: "ticket",
                column: "type_ticket_id");

            migrationBuilder.CreateIndex(
                name: "IX_type_ticket_hammam_id",
                table: "type_ticket",
                column: "hammam_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ticket");

            migrationBuilder.DropTable(
                name: "employe");

            migrationBuilder.DropTable(
                name: "type_ticket");

            migrationBuilder.DropTable(
                name: "hammam");
        }
    }
}
