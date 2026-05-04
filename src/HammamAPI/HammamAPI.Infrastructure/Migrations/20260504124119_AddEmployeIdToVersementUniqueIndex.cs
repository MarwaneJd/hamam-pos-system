using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HammamAPI.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddEmployeIdToVersementUniqueIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"DROP INDEX IF EXISTS ""IX_versement_hammam_id_date_versement"";");
            migrationBuilder.Sql(@"DROP INDEX IF EXISTS ""IX_versement_employe_id_date_versement"";");

            migrationBuilder.Sql(@"
                CREATE UNIQUE INDEX IF NOT EXISTS ""IX_versement_hammam_id_employe_id_date_versement""
                ON versement (hammam_id, employe_id, date_versement);
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"DROP INDEX IF EXISTS ""IX_versement_hammam_id_employe_id_date_versement"";");

            migrationBuilder.Sql(@"
                CREATE UNIQUE INDEX IF NOT EXISTS ""IX_versement_hammam_id_date_versement""
                ON versement (hammam_id, date_versement);
            ");
        }
    }
}
