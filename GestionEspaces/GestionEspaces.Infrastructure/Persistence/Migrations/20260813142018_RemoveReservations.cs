using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GestionEspaces.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RemoveReservations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Reservations");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Reservations",
                columns: table => new
                {
                    IdReservation = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IdAgent = table.Column<int>(type: "int", nullable: false),
                    IdBureau = table.Column<int>(type: "int", nullable: false),
                    DateDebut = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DateFin = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Motif = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Statut = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Version = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Reservations", x => x.IdReservation);
                    table.ForeignKey(
                        name: "FK_Reservations_Agents_IdAgent",
                        column: x => x.IdAgent,
                        principalTable: "Agents",
                        principalColumn: "IdAgent");
                    table.ForeignKey(
                        name: "FK_Reservations_Bureaux_IdBureau",
                        column: x => x.IdBureau,
                        principalTable: "Bureaux",
                        principalColumn: "IdBureau");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Reservations_IdAgent",
                table: "Reservations",
                column: "IdAgent");

            migrationBuilder.CreateIndex(
                name: "IX_Reservations_IdBureau_DateDebut_DateFin",
                table: "Reservations",
                columns: new[] { "IdBureau", "DateDebut", "DateFin" });
        }
    }
}
