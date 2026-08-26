using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GestionEspaces.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddStatutAndEtatRetour : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Statut",
                table: "AffectationsPoste",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "EtatRetour",
                table: "AffectationsActif",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Statut",
                table: "AffectationsActif",
                type: "int",
                nullable: false,
                defaultValue: 0);

            // The new Statut column defaults every existing row to 0 (Active) — including
            // rows that are already closed (DateFin IS NOT NULL). Backfill those to
            // 1 (Terminee) so the persisted column agrees with the DateFin-derived state
            // it's replacing, for both tables.
            migrationBuilder.Sql(@"
                UPDATE [AffectationsPoste] SET [Statut] = 1 WHERE [DateFin] IS NOT NULL;
                UPDATE [AffectationsActif] SET [Statut] = 1 WHERE [DateFin] IS NOT NULL;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Statut",
                table: "AffectationsPoste");

            migrationBuilder.DropColumn(
                name: "EtatRetour",
                table: "AffectationsActif");

            migrationBuilder.DropColumn(
                name: "Statut",
                table: "AffectationsActif");
        }
    }
}
