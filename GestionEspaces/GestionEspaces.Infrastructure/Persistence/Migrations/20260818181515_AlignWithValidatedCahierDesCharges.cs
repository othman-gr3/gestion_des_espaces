using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GestionEspaces.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AlignWithValidatedCahierDesCharges : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Email",
                table: "Sites",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Telephone",
                table: "Sites",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            // --- Data fixups, required before the type/semantic changes below ---

            // Bureau.Type used to be free text (e.g. "Bureau individuel", "Open space",
            // "Salle de réunion", plus a few looser labels from the seed data like
            // "Bureau partagé" / "Salle de formation"). Map every known value to the new
            // TypeBureau enum's numeric value (0=Individuel, 1=OpenSpace, 2=SalleReunion)
            // *while the column is still nvarchar*, so the AlterColumn below (nvarchar -> int)
            // has only numeric strings to cast. Anything unrecognized becomes NULL rather
            // than failing the migration.
            migrationBuilder.Sql(@"
                UPDATE [Bureaux] SET [Type] = CASE
                    WHEN [Type] IN (N'Individuel', N'Bureau individuel') THEN N'0'
                    WHEN [Type] IN (N'OpenSpace', N'Open space', N'Bureau partagé') THEN N'1'
                    WHEN [Type] IN (N'SalleReunion', N'Salle de réunion', N'Salle de formation') THEN N'2'
                    ELSE NULL
                END;
            ");

            // Bureau.Statut is reordered from (0=Disponible, 1=EnMaintenance, 2=HorsService)
            // to (0=Disponible, 1=Occupe, 2=EnMaintenance) — HorsService is retired, and
            // Occupe now takes the old EnMaintenance slot. Remap existing rows via a temporary
            // offset to avoid the two numbering schemes colliding mid-update, then derive
            // Occupe for any bureau that already has an active (DateFin IS NULL) office
            // assignment, mirroring the new auto-managed behavior in Agent.AffecterAuBureau.
            migrationBuilder.Sql(@"
                UPDATE [Bureaux] SET [Statut] = 102 WHERE [Statut] = 2;  -- old HorsService -> temp
                UPDATE [Bureaux] SET [Statut] = 101 WHERE [Statut] = 1;  -- old EnMaintenance -> temp
                UPDATE [Bureaux] SET [Statut] = 2 WHERE [Statut] IN (101, 102);  -- both -> new EnMaintenance
                UPDATE [Bureaux] SET [Statut] = 1
                    WHERE [Statut] = 0
                      AND [IdBureau] IN (SELECT [IdBureau] FROM [AffectationsPoste] WHERE [DateFin] IS NULL);
            ");

            migrationBuilder.AlterColumn<int>(
                name: "Type",
                table: "Bureaux",
                type: "int",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Motif",
                table: "AffectationsPoste",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Agents_Email",
                table: "Agents",
                column: "Email",
                unique: true,
                filter: "[Email] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Agents_Email",
                table: "Agents");

            migrationBuilder.DropColumn(
                name: "Email",
                table: "Sites");

            migrationBuilder.DropColumn(
                name: "Telephone",
                table: "Sites");

            migrationBuilder.DropColumn(
                name: "Motif",
                table: "AffectationsPoste");

            migrationBuilder.AlterColumn<string>(
                name: "Type",
                table: "Bureaux",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);
        }
    }
}
