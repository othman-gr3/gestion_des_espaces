using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GestionEspaces.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddImageToAppUsers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Image",
                table: "AppUsers",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Image",
                table: "AppUsers");
        }
    }
}
