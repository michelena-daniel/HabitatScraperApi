using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HabitatScraperApi.Migrations
{
    /// <inheritdoc />
    public partial class Update_Anuncio : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "PreviousPrice",
                table: "PriceHistory",
                type: "numeric(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "Location",
                table: "Anuncios",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Source",
                table: "Anuncios",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Street",
                table: "Anuncios",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "StreetNumber",
                table: "Anuncios",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PreviousPrice",
                table: "PriceHistory");

            migrationBuilder.DropColumn(
                name: "Location",
                table: "Anuncios");

            migrationBuilder.DropColumn(
                name: "Source",
                table: "Anuncios");

            migrationBuilder.DropColumn(
                name: "Street",
                table: "Anuncios");

            migrationBuilder.DropColumn(
                name: "StreetNumber",
                table: "Anuncios");
        }
    }
}
