using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GestionEspaces.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Actifs",
                columns: table => new
                {
                    IdActif = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nom = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Type = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Marque = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Modele = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    NumeroSerie = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    DateAchat = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Etat = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Image = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Actifs", x => x.IdActif);
                });

            migrationBuilder.CreateTable(
                name: "Agents",
                columns: table => new
                {
                    IdAgent = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nom = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Prenom = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Matricule = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    Telephone = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    Fonction = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    Departement = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    DateEmbauche = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Image = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Agents", x => x.IdAgent);
                });

            migrationBuilder.CreateTable(
                name: "Sites",
                columns: table => new
                {
                    IdSite = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nom = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    AdresseRue = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    AdresseVille = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    AdresseCodePostal = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    AdressePays = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Image = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Sites", x => x.IdSite);
                });

            migrationBuilder.CreateTable(
                name: "AffectationsActif",
                columns: table => new
                {
                    IdAffectationActif = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IdAgent = table.Column<int>(type: "int", nullable: false),
                    IdActif = table.Column<int>(type: "int", nullable: false),
                    DateAffectation = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DateFin = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AffectationsActif", x => x.IdAffectationActif);
                    table.ForeignKey(
                        name: "FK_AffectationsActif_Actifs_IdActif",
                        column: x => x.IdActif,
                        principalTable: "Actifs",
                        principalColumn: "IdActif");
                    table.ForeignKey(
                        name: "FK_AffectationsActif_Agents_IdAgent",
                        column: x => x.IdAgent,
                        principalTable: "Agents",
                        principalColumn: "IdAgent");
                });

            migrationBuilder.CreateTable(
                name: "Batiments",
                columns: table => new
                {
                    IdBatiment = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nom = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    NombreEtages = table.Column<int>(type: "int", nullable: false),
                    Superficie = table.Column<float>(type: "real", nullable: false),
                    Image = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IdSite = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Batiments", x => x.IdBatiment);
                    table.ForeignKey(
                        name: "FK_Batiments_Sites_IdSite",
                        column: x => x.IdSite,
                        principalTable: "Sites",
                        principalColumn: "IdSite",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Bureaux",
                columns: table => new
                {
                    IdBureau = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Numero = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Type = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Capacite = table.Column<int>(type: "int", nullable: false),
                    Superficie = table.Column<float>(type: "real", nullable: false),
                    Etage = table.Column<int>(type: "int", nullable: false),
                    Statut = table.Column<int>(type: "int", nullable: false),
                    Image = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IdBatiment = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Bureaux", x => x.IdBureau);
                    table.ForeignKey(
                        name: "FK_Bureaux_Batiments_IdBatiment",
                        column: x => x.IdBatiment,
                        principalTable: "Batiments",
                        principalColumn: "IdBatiment",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AffectationsPoste",
                columns: table => new
                {
                    IdAffectationPoste = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IdAgent = table.Column<int>(type: "int", nullable: false),
                    IdBureau = table.Column<int>(type: "int", nullable: false),
                    DateAffectation = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DateFin = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AffectationsPoste", x => x.IdAffectationPoste);
                    table.ForeignKey(
                        name: "FK_AffectationsPoste_Agents_IdAgent",
                        column: x => x.IdAgent,
                        principalTable: "Agents",
                        principalColumn: "IdAgent");
                    table.ForeignKey(
                        name: "FK_AffectationsPoste_Bureaux_IdBureau",
                        column: x => x.IdBureau,
                        principalTable: "Bureaux",
                        principalColumn: "IdBureau");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Actifs_NumeroSerie",
                table: "Actifs",
                column: "NumeroSerie",
                unique: true,
                filter: "[NumeroSerie] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_AffectationsActif_IdActif",
                table: "AffectationsActif",
                column: "IdActif",
                unique: true,
                filter: "[DateFin] IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_AffectationsActif_IdAgent",
                table: "AffectationsActif",
                column: "IdAgent");

            migrationBuilder.CreateIndex(
                name: "IX_AffectationsPoste_IdAgent",
                table: "AffectationsPoste",
                column: "IdAgent",
                unique: true,
                filter: "[DateFin] IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_AffectationsPoste_IdBureau",
                table: "AffectationsPoste",
                column: "IdBureau",
                unique: true,
                filter: "[DateFin] IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Agents_Matricule",
                table: "Agents",
                column: "Matricule",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Batiments_IdSite",
                table: "Batiments",
                column: "IdSite");

            migrationBuilder.CreateIndex(
                name: "IX_Bureaux_IdBatiment_Numero",
                table: "Bureaux",
                columns: new[] { "IdBatiment", "Numero" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Sites_Code",
                table: "Sites",
                column: "Code",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AffectationsActif");

            migrationBuilder.DropTable(
                name: "AffectationsPoste");

            migrationBuilder.DropTable(
                name: "Actifs");

            migrationBuilder.DropTable(
                name: "Agents");

            migrationBuilder.DropTable(
                name: "Bureaux");

            migrationBuilder.DropTable(
                name: "Batiments");

            migrationBuilder.DropTable(
                name: "Sites");
        }
    }
}
