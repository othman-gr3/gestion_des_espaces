using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GestionEspaces.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddDemandes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Demandes",
                columns: table => new
                {
                    IdDemande = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Version = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    IdAgent = table.Column<int>(type: "int", nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    Statut = table.Column<int>(type: "int", nullable: false),
                    DateCreation = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DateTraitement = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Reponse = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Demandes", x => x.IdDemande);
                    table.ForeignKey(
                        name: "FK_Demandes_Agents_IdAgent",
                        column: x => x.IdAgent,
                        principalTable: "Agents",
                        principalColumn: "IdAgent");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Demandes_IdAgent",
                table: "Demandes",
                column: "IdAgent");

            migrationBuilder.CreateIndex(
                name: "IX_Demandes_Statut",
                table: "Demandes",
                column: "Statut");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Demandes");
        }
    }
}
